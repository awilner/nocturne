namespace Nocturne.API.Models.Requests.V4;

public class UpdateCarbIntakeRequest
{
    public DateTimeOffset Timestamp { get; set; }
    public int? UtcOffset { get; set; }
    public string? Device { get; set; }
    public string? App { get; set; }
    public string? DataSource { get; set; }
    public double Carbs { get; set; }
    public string? SyncIdentifier { get; set; }
    public double? CarbTime { get; set; }
    public int? AbsorptionTime { get; set; }
    public Guid? CorrelationId { get; set; }
}
