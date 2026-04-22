using Nocturne.API.Controllers.V4;
using Nocturne.API.Controllers.V4.Analytics;

namespace Nocturne.API.Services.Glucose;

/// <summary>
/// Produces forward glucose predictions from current CGM and treatment data.
/// The active prediction source is configured via <see cref="PredictionOptions"/>:
/// <see cref="PredictionSource.DeviceStatus"/> reads AID-computed predictions from the latest
/// device status upload, while <see cref="PredictionSource.OrefWasm"/> runs the oref algorithm
/// server-side via <see cref="OrefWasmService"/>. <see cref="PredictionSource.None"/> causes
/// the endpoint to return a 404.
/// </summary>
/// <seealso cref="PredictionService"/>
public interface IPredictionService
{
    /// <summary>
    /// Returns glucose predictions based on the configured prediction source.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no glucose readings or device status data are available.</exception>
    Task<GlucosePredictionResponse> GetPredictionsAsync(
        string? profileId = null,
        CancellationToken cancellationToken = default);
}
