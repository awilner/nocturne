namespace Nocturne.API.Models.Requests.V4;

public class UpsertStepCountRequest
{
    public DateTimeOffset Timestamp { get; set; }
    public int? UtcOffset { get; set; }
    public int Metric { get; set; }
    public int Source { get; set; }
    public string? Device { get; set; }
    public string? App { get; set; }
    public string? DataSource { get; set; }
}
