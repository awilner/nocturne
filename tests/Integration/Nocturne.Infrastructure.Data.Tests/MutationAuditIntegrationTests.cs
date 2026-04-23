using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nocturne.Core.Contracts.Audit;
using Nocturne.Infrastructure.Data.Interceptors;
using Testcontainers.PostgreSql;

namespace Nocturne.Infrastructure.Data.Tests.Integration;

/// <summary>
/// Integration tests verifying the mutation audit log end-to-end with a real
/// PostgreSQL database. Uses EntryEntity as the test subject since it
/// implements both IAuditable and ISoftDeletable.
/// </summary>
[Trait("Category", "Integration")]
public class MutationAuditIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer = null!;
    private ServiceProvider _serviceProvider = null!;
    private Guid _tenantId;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("nocturne_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        services.AddSingleton<MutationAuditInterceptor>();

        services.AddDbContext<NocturneDbContext>((sp, options) =>
            options
                .UseNpgsql(_postgresContainer.GetConnectionString())
                .AddInterceptors(sp.GetRequiredService<MutationAuditInterceptor>())
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
        );

        _serviceProvider = services.BuildServiceProvider();

        var dbContext = _serviceProvider.GetRequiredService<NocturneDbContext>();
        await dbContext.Database.MigrateAsync();

        // Create a test tenant directly so tenant-scoped entities can be saved
        _tenantId = Guid.CreateVersion7();
        await dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO tenants (id, slug, display_name, is_active, is_default, timezone, allow_access_requests, sys_created_at, sys_updated_at) " +
            "VALUES ({0}, {1}, {2}, true, false, 'UTC', true, now(), now())",
            _tenantId, "audit-test", "Audit Test Tenant");
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task Create_ProducesAuditRecord()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NocturneDbContext>();
        context.TenantId = _tenantId;

        var entry = CreateTestEntry();
        context.Entries.Add(entry);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var auditRecords = await context.MutationAuditLog
            .Where(a => a.EntityId == entry.Id)
            .ToListAsync();

        auditRecords.Should().ContainSingle();
        var audit = auditRecords[0];
        audit.Action.Should().Be("create");
        audit.EntityType.Should().Be("Entry");
        audit.EntityId.Should().Be(entry.Id);
        audit.ChangesJson.Should().BeNull();
        audit.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task Update_ProducesAuditRecordWithDiff()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NocturneDbContext>();
        context.TenantId = _tenantId;

        var entry = CreateTestEntry();
        entry.Mgdl = 120.0;
        context.Entries.Add(entry);
        await context.SaveChangesAsync();

        // Act — modify a field
        entry.Mgdl = 150.0;
        await context.SaveChangesAsync();

        // Assert
        var auditRecords = await context.MutationAuditLog
            .Where(a => a.EntityId == entry.Id)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        auditRecords.Should().HaveCount(2);

        var updateAudit = auditRecords[1];
        updateAudit.Action.Should().Be("update");
        updateAudit.ChangesJson.Should().NotBeNull();

        // Verify the changes JSON contains the old/new values
        var changesDoc = JsonDocument.Parse(updateAudit.ChangesJson!);
        var root = changesDoc.RootElement;
        root.TryGetProperty("Mgdl", out var mgdlChange).Should().BeTrue();
        mgdlChange.GetProperty("old").GetDouble().Should().Be(120.0);
        mgdlChange.GetProperty("new").GetDouble().Should().Be(150.0);
    }

    [Fact]
    public async Task SoftDelete_ProducesAuditRecordWithSnapshot()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NocturneDbContext>();
        context.TenantId = _tenantId;

        var entry = CreateTestEntry();
        entry.Mgdl = 130.0;
        entry.Device = "test-device-soft-delete";
        context.Entries.Add(entry);
        await context.SaveChangesAsync();

        // Act — soft delete by setting DeletedAt
        entry.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var auditRecords = await context.MutationAuditLog
            .Where(a => a.EntityId == entry.Id)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        auditRecords.Should().HaveCount(2);

        var deleteAudit = auditRecords[1];
        deleteAudit.Action.Should().Be("delete");
        deleteAudit.ChangesJson.Should().NotBeNull();

        // The changes should be a full snapshot of the entity
        var snapshot = JsonDocument.Parse(deleteAudit.ChangesJson!);
        var root = snapshot.RootElement;
        root.TryGetProperty("Mgdl", out var mgdlProp).Should().BeTrue();
        mgdlProp.GetDouble().Should().Be(130.0);
    }

    [Fact]
    public async Task TransactionRollback_RemovesBothEntityAndAuditRecord()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NocturneDbContext>();
        context.TenantId = _tenantId;

        var entry = CreateTestEntry();

        // Act — save inside a transaction, then roll back
        await using var transaction = await context.Database.BeginTransactionAsync();

        context.Entries.Add(entry);
        await context.SaveChangesAsync();

        // Verify both exist within the transaction
        var entryExists = await context.Entries.AnyAsync(e => e.Id == entry.Id);
        var auditExists = await context.MutationAuditLog.AnyAsync(a => a.EntityId == entry.Id);
        entryExists.Should().BeTrue();
        auditExists.Should().BeTrue();

        await transaction.RollbackAsync();

        // Assert — use a fresh context to avoid stale cache
        using var verifyScope = _serviceProvider.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<NocturneDbContext>();
        verifyContext.TenantId = _tenantId;

        var entryAfterRollback = await verifyContext.Entries.AnyAsync(e => e.Id == entry.Id);
        var auditAfterRollback = await verifyContext.MutationAuditLog.AnyAsync(a => a.EntityId == entry.Id);

        entryAfterRollback.Should().BeFalse();
        auditAfterRollback.Should().BeFalse();
    }

    [Fact]
    public async Task NullAuditContext_ProducesAuditRecordWithNullActorFields()
    {
        // Arrange — no AuditContext set, no HttpContext
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NocturneDbContext>();
        context.TenantId = _tenantId;
        // Explicitly ensure no audit context is set
        context.AuditContext = null;

        var entry = CreateTestEntry();
        context.Entries.Add(entry);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var audit = await context.MutationAuditLog
            .SingleAsync(a => a.EntityId == entry.Id);

        audit.Action.Should().Be("create");
        audit.EntityType.Should().Be("Entry");
        audit.SubjectId.Should().BeNull();
        audit.AuthType.Should().BeNull();
        audit.IpAddress.Should().BeNull();
        audit.TokenId.Should().BeNull();
        audit.CorrelationId.Should().BeNull();
        audit.Endpoint.Should().BeNull();
    }

    [Fact]
    public async Task AuditContextFields_PopulatedFromSystemAuditContext()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NocturneDbContext>();
        context.TenantId = _tenantId;
        context.AuditContext = SystemAuditContext.ForService("test:integration");

        var entry = CreateTestEntry();
        context.Entries.Add(entry);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var audit = await context.MutationAuditLog
            .SingleAsync(a => a.EntityId == entry.Id);

        audit.AuthType.Should().Be("system");
        audit.Endpoint.Should().Be("test:integration");
        audit.CorrelationId.Should().NotBeNullOrEmpty();
        // SystemAuditContext always returns null for these
        audit.SubjectId.Should().BeNull();
        audit.IpAddress.Should().BeNull();
        audit.TokenId.Should().BeNull();
    }

    private EntryEntity CreateTestEntry()
    {
        var now = DateTimeOffset.UtcNow;
        return new EntryEntity
        {
            Id = Guid.CreateVersion7(),
            Mills = now.ToUnixTimeMilliseconds(),
            DateString = now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Mgdl = 120.0,
            Type = "sgv",
            Device = "test-device",
            IsCalibration = false
        };
    }
}
