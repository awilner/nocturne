using System.Text.Json.Serialization;

namespace Nocturne.Connectors.HomeAssistant.Models;

public class HomeAssistantStateResponse
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; init; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("attributes")]
    public Dictionary<string, object?> Attributes { get; init; } = new();

    [JsonPropertyName("last_changed")]
    public DateTimeOffset LastChanged { get; init; }

    [JsonPropertyName("last_updated")]
    public DateTimeOffset LastUpdated { get; init; }
}
