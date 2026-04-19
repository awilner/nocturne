using Nocturne.Core.Models.V4;

namespace Nocturne.API.Models.Requests.V4;

/// <summary>
/// Request body for creating a correlated meal event (a single bolus + single carb
/// intake sharing a CorrelationId, created atomically).
/// </summary>
public class CreateMealRequest
{
    public DateTimeOffset Timestamp { get; set; }
    public int? UtcOffset { get; set; }
    public double Insulin { get; set; }
    public double Carbs { get; set; }
    public BolusType? BolusType { get; set; }
    public double? Duration { get; set; }
    public int? AbsorptionTime { get; set; }
    public double? CarbTime { get; set; }
    public string? InsulinType { get; set; }
    public string? Device { get; set; }
    public string? App { get; set; }
    public string? DataSource { get; set; }
    public string? SyncIdentifier { get; set; }
    public Guid? BolusCalculationId { get; set; }
    public Guid? CorrelationId { get; set; }
}
