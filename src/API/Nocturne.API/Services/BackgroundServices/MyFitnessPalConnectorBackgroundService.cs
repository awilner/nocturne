using Nocturne.Connectors.Core.Interfaces;
using Nocturne.Connectors.Core.Models;
using Nocturne.Connectors.MyFitnessPal.Configurations;
using Nocturne.Connectors.MyFitnessPal.Services;

namespace Nocturne.API.Services.BackgroundServices;

/// <summary>
/// Background service that periodically syncs food diary and nutrition data from MyFitnessPal via
/// <see cref="MyFitnessPalConnectorService"/>.
/// </summary>
/// <seealso cref="ConnectorBackgroundService{TConfig}"/>
public class MyFitnessPalConnectorBackgroundService : ConnectorBackgroundService<MyFitnessPalConnectorConfiguration>
{
    /// <param name="serviceProvider">Service provider used to create a DI scope per sync cycle.</param>
    /// <param name="config">MyFitnessPal connector configuration (credentials, polling interval, etc.).</param>
    /// <param name="logger">Logger instance for this background service.</param>
    public MyFitnessPalConnectorBackgroundService(
        IServiceProvider serviceProvider,
        MyFitnessPalConnectorConfiguration config,
        ILogger<MyFitnessPalConnectorBackgroundService> logger
    )
        : base(serviceProvider, config, logger) { }

    protected override string ConnectorName => "MyFitnessPal";

    protected override async Task<SyncResult> PerformSyncAsync(IServiceProvider scopeProvider, CancellationToken cancellationToken, ISyncProgressReporter? progressReporter = null)
    {
        var connectorService = scopeProvider.GetRequiredService<MyFitnessPalConnectorService>();
        return await connectorService.SyncDataAsync(Config, cancellationToken, since: null, progressReporter);
    }
}
