namespace Nocturne.API.Models.Requests.V4;

public class UpsertHeartRateRequest
{
    public DateTimeOffset Timestamp { get; set; }
    public int? UtcOffset { get; set; }
    public int Bpm { get; set; }
    public int Accuracy { get; set; }
    public string? Device { get; set; }
    public string? App { get; set; }
    public string? DataSource { get; set; }
}
