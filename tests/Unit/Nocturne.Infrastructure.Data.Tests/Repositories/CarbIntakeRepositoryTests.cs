using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nocturne.Core.Contracts;
using Nocturne.Core.Models.V4;
using Nocturne.Infrastructure.Data.Repositories.V4;
using Nocturne.Tests.Shared.Infrastructure;
using Xunit;

namespace Nocturne.Infrastructure.Data.Tests.Repositories;

[Trait("Category", "Unit")]
[Trait("Category", "Repository")]
[Trait("Category", "CarbIntake")]
public class CarbIntakeRepositoryTests : IDisposable
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly NocturneDbContext _context;
    private readonly Mock<IDeduplicationService> _mockDeduplicationService;
    private readonly CarbIntakeRepository _repo;

    public CarbIntakeRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _context.TenantId = TestTenantId;

        _mockDeduplicationService = new Mock<IDeduplicationService>();
        _mockDeduplicationService
            .Setup(d => d.GetOrCreateCanonicalIdAsync(
                It.IsAny<RecordType>(),
                It.IsAny<long>(),
                It.IsAny<MatchCriteria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _mockDeduplicationService
            .Setup(d => d.LinkRecordAsync(
                It.IsAny<Guid>(),
                It.IsAny<RecordType>(),
                It.IsAny<Guid>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repo = new CarbIntakeRepository(
            _context,
            _mockDeduplicationService.Object,
            NullLogger<CarbIntakeRepository>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateAsync_WithExistingSyncIdentifier_UpdatesInPlace()
    {
        var timestamp = DateTime.UtcNow;
        var first = await _repo.CreateAsync(new CarbIntake
        {
            Timestamp = timestamp,
            DataSource = "aaps",
            SyncIdentifier = "sync-1",
            Carbs = 30.0,
        });

        var second = await _repo.CreateAsync(new CarbIntake
        {
            Timestamp = timestamp,
            DataSource = "aaps",
            SyncIdentifier = "sync-1",
            Carbs = 42.0,
        });

        second.Id.Should().Be(first.Id);
        second.Carbs.Should().Be(42.0);
        var count = await _context.CarbIntakes.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_WithoutSyncIdentifier_DoesNotDedupe()
    {
        var timestamp = DateTime.UtcNow;
        await _repo.CreateAsync(new CarbIntake { Timestamp = timestamp, Carbs = 30.0 });
        await _repo.CreateAsync(new CarbIntake { Timestamp = timestamp, Carbs = 30.0 });

        var count = await _context.CarbIntakes.CountAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_WithoutDataSource_DoesNotDedupe()
    {
        var timestamp = DateTime.UtcNow;
        await _repo.CreateAsync(new CarbIntake { Timestamp = timestamp, SyncIdentifier = "sync-1", Carbs = 30.0 });
        await _repo.CreateAsync(new CarbIntake { Timestamp = timestamp, SyncIdentifier = "sync-1", Carbs = 30.0 });

        var count = await _context.CarbIntakes.CountAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_WithSameSyncIdentifierDifferentDataSource_InsertsBoth()
    {
        var timestamp = DateTime.UtcNow;
        await _repo.CreateAsync(new CarbIntake
        {
            Timestamp = timestamp,
            DataSource = "aaps",
            SyncIdentifier = "sync-1",
            Carbs = 30.0,
        });
        await _repo.CreateAsync(new CarbIntake
        {
            Timestamp = timestamp,
            DataSource = "loop",
            SyncIdentifier = "sync-1",
            Carbs = 30.0,
        });

        var count = await _context.CarbIntakes.CountAsync();
        count.Should().Be(2);
    }
}
