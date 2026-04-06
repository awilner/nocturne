using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nocturne.Connectors.HomeAssistant.Models;

namespace Nocturne.Connectors.HomeAssistant.Services;

public class HomeAssistantApiClient(HttpClient httpClient, ILogger<HomeAssistantApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public virtual async Task<HomeAssistantStateResponse?> GetStateAsync(
        string entityId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        var response = await httpClient.GetAsync($"/api/states/{entityId}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogDebug("HA entity {EntityId} not found", entityId);
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HomeAssistantStateResponse>(JsonOptions, ct);
    }

    public virtual async Task<bool> SetStateAsync(
        string entityId, string state, Dictionary<string, object> attributes,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        var payload = new { state, attributes };
        var response = await httpClient.PostAsJsonAsync(
            $"/api/states/{entityId}", payload, JsonOptions, ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to set HA state for {EntityId}: {StatusCode}",
                entityId, response.StatusCode);
            return false;
        }

        return true;
    }

    public virtual async Task<bool> ValidateEntityExistsAsync(
        string entityId, CancellationToken ct = default)
    {
        var state = await GetStateAsync(entityId, ct);
        return state != null;
    }
}
