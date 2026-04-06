using Microsoft.Extensions.Logging;
using Nocturne.Connectors.Core.Interfaces;
using Nocturne.Connectors.Core.Models;
using Nocturne.Connectors.Core.Services;
using Nocturne.Connectors.HomeAssistant.Configurations;
using Nocturne.Connectors.HomeAssistant.Mappers;
using Nocturne.Connectors.HomeAssistant.Services;
using Nocturne.Core.Constants;

namespace Nocturne.Connectors.HomeAssistant;

/// <summary>
/// Connector service for Home Assistant bidirectional sync.
/// Polls HA entity states and publishes as Nocturne domain models.
/// </summary>
public class HomeAssistantConnectorService : BaseConnectorService<HomeAssistantConnectorConfiguration>
{
    private readonly IHomeAssistantApiClient _apiClient;
    private readonly HomeAssistantEntityMapper _mapper;
    private readonly Dictionary<string, DateTimeOffset> _lastChanged = new();

    public HomeAssistantConnectorService(
        HttpClient httpClient,
        ILogger<HomeAssistantConnectorService> logger,
        IHomeAssistantApiClient apiClient,
        HomeAssistantEntityMapper mapper,
        IConnectorPublisher? publisher = null)
        : base(httpClient, logger, publisher)
    {
        _apiClient = apiClient;
        _mapper = mapper;
    }

    protected override string ConnectorSource => DataSources.HomeAssistantConnector;
    public override string ServiceName => "Home Assistant";

    public override List<SyncDataType> SupportedDataTypes =>
    [
        SyncDataType.Glucose,
        SyncDataType.Boluses,
        SyncDataType.CarbIntake,
        SyncDataType.Activity,
        SyncDataType.ManualBG
    ];

    public override Task<bool> AuthenticateAsync()
    {
        // OAuth2 handled at HTTP client level
        TrackSuccessfulRequest();
        return Task.FromResult(true);
    }

    protected override async Task<SyncResult> PerformSyncInternalAsync(
        SyncRequest request,
        HomeAssistantConnectorConfiguration config,
        CancellationToken cancellationToken,
        ISyncProgressReporter? progressReporter = null)
    {
        var result = new SyncResult { StartTime = DateTimeOffset.UtcNow, Success = true };

        foreach (var (dataType, entityId) in config.EntityMappings)
        {
            try
            {
                var state = await _apiClient.GetStateAsync(entityId, cancellationToken);
                if (state == null)
                {
                    _logger.LogWarning("HA entity {EntityId} not found for {DataType}", entityId, dataType);
                    continue;
                }

                // Dedup: skip if last_changed hasn't moved
                if (_lastChanged.TryGetValue(entityId, out var prev) && prev >= state.LastChanged)
                    continue;

                _lastChanged[entityId] = state.LastChanged;

                var published = await PublishStateAsync(dataType, state, config, cancellationToken);
                if (published)
                {
                    result.ItemsSynced.TryGetValue(dataType, out var count);
                    result.ItemsSynced[dataType] = count + 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing HA entity {EntityId} for {DataType}", entityId, dataType);
                result.Errors.Add($"{dataType}: {ex.Message}");
            }
        }

        result.Success = result.Errors.Count == 0;
        result.EndTime = DateTimeOffset.UtcNow;
        return result;
    }

    private async Task<bool> PublishStateAsync(
        SyncDataType dataType,
        Models.HomeAssistantStateResponse state,
        HomeAssistantConnectorConfiguration config,
        CancellationToken ct)
    {
        switch (dataType)
        {
            case SyncDataType.Glucose:
                var entry = _mapper.MapToEntry(state);
                if (entry == null) return false;
                return await PublishGlucoseDataAsync([entry], config, ct);

            // Additional data types added in Task 11
            default:
                _logger.LogWarning("Unsupported data type {DataType} for HA sync", dataType);
                return false;
        }
    }
}
