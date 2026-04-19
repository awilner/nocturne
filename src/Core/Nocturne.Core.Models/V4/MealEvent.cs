using System.Text.Json.Serialization;
using Nocturne.Core.Models;

namespace Nocturne.Core.Models.V4;

/// <summary>
/// Event-centric view of a meal: a group of correlated carb intakes and boluses
/// sharing a <see cref="CorrelationId"/>, plus any food attribution rows.
/// Orphan carb intakes (null CorrelationId) are still represented, one per event,
/// with an empty <see cref="Boluses"/> array.
/// </summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public class MealEvent
{
    /// <summary>
    /// Shared correlation identifier. For orphan events (no correlation), this
    /// is <see cref="Guid.Empty"/>.
    /// </summary>
    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Earliest timestamp across all carb intakes and boluses in the group.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("carbIntakes")]
    public CarbIntake[] CarbIntakes { get; set; } = [];

    [JsonPropertyName("boluses")]
    public Bolus[] Boluses { get; set; } = [];

    [JsonPropertyName("foods")]
    public TreatmentFood[] Foods { get; set; } = [];

    [JsonPropertyName("totalCarbs")]
    public double TotalCarbs { get; set; }

    [JsonPropertyName("attributedCarbs")]
    public double AttributedCarbs { get; set; }

    [JsonPropertyName("unspecifiedCarbs")]
    public double UnspecifiedCarbs { get; set; }

    [JsonPropertyName("totalInsulin")]
    public double TotalInsulin { get; set; }

    [JsonPropertyName("isAttributed")]
    public bool IsAttributed { get; set; }
}
