using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nocturne.Connectors.Core.Interfaces;
using Nocturne.Connectors.Core.Models;
using Nocturne.Core.Contracts.Audit;
using Nocturne.Core.Contracts.Connectors;
using Nocturne.Core.Contracts.Multitenancy;
using Nocturne.Infrastructure.Data;

namespace Nocturne.API.Services.BackgroundServices;

/// <summary>
/// Abstract base class for connector background services that poll external data sources
/// on a per-tenant basis within the API process.
/// </summary>
/// <typeparam name="TConfig">
/// The connector configuration type, which must implement <see cref="IConnectorConfiguration"/>.
/// </typeparam>
/// <remarks>
/// The service polls every minute and only syncs a given tenant when its configured
/// <c>SyncIntervalMinutes</c> has elapsed since the last sync. Database configuration
/// and secrets are loaded fresh for each tenant sync cycle via <see cref="LoadDatabaseConfigurationAsync"/>.
/// </remarks>
public abstract class ConnectorBackgroundService<TConfig> : BackgroundService
    where TConfig : class, IConnectorConfiguration
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ILogger Logger;
    protected readonly TConfig Config;

    /// <summary>
    /// Tracks the last sync time per tenant so each tenant's configured
    /// SyncIntervalMinutes is respected independently.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, DateTime> _lastSyncByTenant = new();

    /// <summary>
    /// Initialises a new <see cref="ConnectorBackgroundService{TConfig}"/>.
    /// </summary>
    /// <param name="serviceProvider">Root DI service provider; a new scope is created per tenant sync.</param>
    /// <param name="config">Connector configuration singleton; updated at runtime from DB values.</param>
    /// <param name="logger">Logger instance.</param>
    protected ConnectorBackgroundService(
        IServiceProvider serviceProvider,
        TConfig config,
        ILogger logger
    )
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Config = config ?? throw new ArgumentNullException(nameof(config));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the connector name for logging
    /// </summary>
    protected abstract string ConnectorName { get; }

    /// <summary>
    /// Performs a single sync operation using the connector service.
    /// Services should be resolved from the provided <paramref name="scopeProvider"/>
    /// which has the tenant context already set.
    /// </summary>
    /// <param name="scopeProvider">Tenant-scoped service provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="progressReporter">Optional progress reporter for sync status updates</param>
    /// <returns>A SyncResult indicating success/failure and any error details</returns>
    protected abstract Task<SyncResult> PerformSyncAsync(IServiceProvider scopeProvider, CancellationToken cancellationToken, ISyncProgressReporter? progressReporter = null);

    /// <summary>
    /// Loads runtime configuration and secrets from the database and applies them
    /// to the <see cref="Config"/> singleton. Ensures DB-stored values (including encrypted
    /// passwords) are available to the connector at runtime.
    /// </summary>
    /// <param name="scopeProvider">Tenant-scoped service provider for resolving <see cref="IConnectorConfigurationService"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> when a database configuration row exists for this connector;
    /// <see langword="false"/> when no configuration is found and the sync should be skipped.
    /// </returns>
    protected async Task<bool> LoadDatabaseConfigurationAsync(IServiceProvider scopeProvider, CancellationToken ct)
    {
        try
        {
            var configService = scopeProvider.GetRequiredService<IConnectorConfigurationService>();

            // Load runtime configuration from DB
            var dbConfig = await configService.GetConfigurationAsync(ConnectorName, ct);
            if (dbConfig == null)
            {
                Logger.LogDebug("No configuration found for {ConnectorName}, skipping sync", ConnectorName);
                return false;
            }

            if (dbConfig.Configuration != null)
            {
                ApplyJsonToConfig(dbConfig.Configuration);
                Logger.LogDebug("Applied database configuration for {ConnectorName}", ConnectorName);
            }

            // Load and decrypt secrets from DB
            var secrets = await configService.GetSecretsAsync(ConnectorName, ct);
            if (secrets.Count > 0)
            {
                ApplySecretsToConfig(secrets);
                Logger.LogDebug("Applied {Count} secrets for {ConnectorName}", secrets.Count, ConnectorName);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex,
                "Failed to load database configuration for {ConnectorName}, using environment/startup values",
                ConnectorName);
            return false;
        }
    }

    /// <summary>
    /// Applies JSON configuration values to the <see cref="Config"/> object using reflection.
    /// Matches camelCase JSON property names to PascalCase C# property names.
    /// </summary>
    /// <param name="configuration">The parsed JSON document containing connector configuration values.</param>
    private void ApplyJsonToConfig(JsonDocument configuration)
    {
        var properties = Config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var root = configuration.RootElement;

        foreach (var property in properties)
        {
            if (!property.CanWrite) continue;

            var camelName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            if (!root.TryGetProperty(camelName, out var element)) continue;

            try
            {
                if (property.PropertyType == typeof(string) && element.ValueKind == JsonValueKind.String)
                    property.SetValue(Config, element.GetString());
                else if (property.PropertyType == typeof(int) && element.ValueKind == JsonValueKind.Number)
                    property.SetValue(Config, element.GetInt32());
                else if (property.PropertyType == typeof(double) && element.ValueKind == JsonValueKind.Number)
                    property.SetValue(Config, element.GetDouble());
                else if (property.PropertyType == typeof(bool) &&
                         (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False))
                    property.SetValue(Config, element.GetBoolean());
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not apply config property {Property} for {ConnectorName}",
                    property.Name, ConnectorName);
            }
        }
    }

    /// <summary>
    /// Applies decrypted secret values to the <see cref="Config"/> object using reflection.
    /// Matches camelCase secret keys to PascalCase C# properties of type <see cref="string"/>.
    /// </summary>
    /// <param name="secrets">Dictionary of camelCase secret names to decrypted string values.</param>
    private void ApplySecretsToConfig(Dictionary<string, string> secrets)
    {
        var properties = Config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!property.CanWrite || property.PropertyType != typeof(string)) continue;

            var camelName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            if (secrets.TryGetValue(camelName, out var value))
            {
                property.SetValue(Config, value);
            }
        }
    }

    /// <summary>
    /// Persists the health state for this connector to the database via <see cref="IConnectorConfigurationService"/>.
    /// Errors are swallowed and logged as warnings so that health-state failures do not abort sync.
    /// </summary>
    private async Task UpdateHealthStateAsync(
        IServiceProvider scopeProvider,
        DateTime? lastSyncAttempt = null,
        DateTime? lastSuccessfulSync = null,
        string? lastErrorMessage = null,
        DateTime? lastErrorAt = null,
        bool? isHealthy = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var configService = scopeProvider.GetRequiredService<IConnectorConfigurationService>();

            await configService.UpdateHealthStateAsync(
                ConnectorName,
                lastSyncAttempt,
                lastSuccessfulSync,
                lastErrorMessage,
                lastErrorAt,
                isHealthy,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "Failed to update health state for {ConnectorName}",
                ConnectorName
            );
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait briefly to let the application fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        Logger.LogInformation(
            "{ConnectorName} connector background service started",
            ConnectorName);

        try
        {
            // Poll every minute; each tenant is only synced when its own
            // SyncIntervalMinutes has elapsed since its last sync.
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

            do
            {
                try
                {
                    await SyncAllTenantsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Logger.LogError(ex, "Error during {ConnectorName} tenant sync cycle", ConnectorName);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("{ConnectorName} connector background service stopping", ConnectorName);
        }
        finally
        {
            Logger.LogInformation(
                "{ConnectorName} connector background service stopped",
                ConnectorName);
        }
    }

    private async Task SyncAllTenantsAsync(CancellationToken stoppingToken)
    {
        using var lookupScope = ServiceProvider.CreateScope();
        var factory = lookupScope.ServiceProvider.GetRequiredService<IDbContextFactory<NocturneDbContext>>();
        await using var lookupContext = await factory.CreateDbContextAsync(stoppingToken);
        var tenants = await lookupContext.Tenants.AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.Slug, t.DisplayName })
            .ToListAsync(stoppingToken);

        foreach (var tenant in tenants)
        {
            try
            {
                await SyncForTenantAsync(tenant.Id, tenant.Slug, tenant.DisplayName, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogError(ex,
                    "Error syncing {ConnectorName} for tenant {TenantSlug}",
                    ConnectorName, tenant.Slug);
            }
        }
    }

    private async Task SyncForTenantAsync(Guid tenantId, string tenantSlug, string displayName, CancellationToken stoppingToken)
    {
        using var scope = ServiceProvider.CreateScope();

        // Set tenant context for this scope
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantAccessor>();
        tenantAccessor.SetTenant(new TenantContext(tenantId, tenantSlug, displayName, true));

        // Populate audit context so mutations are attributed to this connector
        var dbContext = scope.ServiceProvider.GetRequiredService<NocturneDbContext>();
        dbContext.AuditContext = SystemAuditContext.ForService($"connector:{ConnectorName}");

        // Load tenant-specific connector configuration; skip if no config exists in DB
        var hasConfig = await LoadDatabaseConfigurationAsync(scope.ServiceProvider, stoppingToken);
        if (!hasConfig)
            return;

        if (!Config.Enabled || Config.SyncIntervalMinutes <= 0)
            return;

        // Only sync when the tenant's configured interval has elapsed
        var now = DateTime.UtcNow;
        var interval = TimeSpan.FromMinutes(Config.SyncIntervalMinutes);
        if (_lastSyncByTenant.TryGetValue(tenantId, out var lastSync) && now - lastSync < interval)
            return;

        Logger.LogDebug("Syncing {ConnectorName} for tenant {TenantSlug}", ConnectorName, tenantSlug);

        _lastSyncByTenant[tenantId] = now;

        await UpdateHealthStateAsync(
            scope.ServiceProvider,
            lastSyncAttempt: now,
            cancellationToken: stoppingToken);

        var progressReporter = scope.ServiceProvider.GetService<ISyncProgressReporter>();
        var result = await PerformSyncAsync(scope.ServiceProvider, stoppingToken, progressReporter);

        if (result.Success)
        {
            Logger.LogInformation(
                "{ConnectorName} sync completed for tenant {TenantSlug}",
                ConnectorName, tenantSlug);

            await UpdateHealthStateAsync(
                scope.ServiceProvider,
                lastSuccessfulSync: DateTime.UtcNow,
                isHealthy: true,
                lastErrorMessage: string.Empty,
                lastErrorAt: DateTime.MinValue,
                cancellationToken: stoppingToken);
        }
        else
        {
            var errorMessage = result.Errors.Count > 0
                ? string.Join("; ", result.Errors)
                : !string.IsNullOrWhiteSpace(result.Message)
                    ? result.Message
                    : "Sync failed";

            Logger.LogWarning(
                "{ConnectorName} sync failed for tenant {TenantSlug}: {ErrorMessage}",
                ConnectorName, tenantSlug, errorMessage);

            await UpdateHealthStateAsync(
                scope.ServiceProvider,
                isHealthy: false,
                lastErrorMessage: errorMessage,
                lastErrorAt: DateTime.UtcNow,
                cancellationToken: stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "{ConnectorName} connector background service is stopping...",
            ConnectorName
        );
        await base.StopAsync(cancellationToken);
    }
}
