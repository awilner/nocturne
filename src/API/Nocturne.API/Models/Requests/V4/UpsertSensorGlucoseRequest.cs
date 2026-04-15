using Nocturne.Core.Models.V4;

namespace Nocturne.API.Models.Requests.V4;

public class UpsertSensorGlucoseRequest
{
    public DateTimeOffset Timestamp { get; set; }
    public int? UtcOffset { get; set; }
    public string? Device { get; set; }
    public string? App { get; set; }
    public string? DataSource { get; set; }
    public double Mgdl { get; set; }
    public GlucoseDirection? Direction { get; set; }
    public double? TrendRate { get; set; }
    public int? Noise { get; set; }

    /// <summary>
    /// Raw filtered sensor value (scaled ADC)
    /// </summary>
    public double? Filtered { get; set; }

    /// <summary>
    /// Raw unfiltered sensor value (scaled ADC)
    /// </summary>
    public double? Unfiltered { get; set; }

    /// <summary>
    /// Glucose delta in mg/dL over the last 5 minutes
    /// </summary>
    public double? Delta { get; set; }
}
