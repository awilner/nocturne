using System.Text.Json;
using Nocturne.Core.Contracts.Alerts;
using Nocturne.Core.Models;
using Nocturne.Core.Models.Alerts;

namespace Nocturne.API.Services.Alerts.Evaluators;

/// <summary>
/// Evaluates a signal-loss condition by comparing the elapsed time since the last
/// CGM reading against a configurable timeout.
/// </summary>
/// <remarks>
/// When <see cref="SensorContext.LastReadingAt"/> is <see langword="null"/> (no data has ever
/// arrived), the condition is considered met immediately regardless of the configured timeout.
/// </remarks>
/// <seealso cref="IConditionEvaluator"/>
public class SignalLossEvaluator : IConditionEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initialises a new <see cref="SignalLossEvaluator"/>.
    /// </summary>
    /// <param name="timeProvider">
    /// Abstraction for the current UTC time, enabling deterministic unit tests.
    /// </param>
    public SignalLossEvaluator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public AlertConditionType ConditionType => AlertConditionType.SignalLoss;

    /// <inheritdoc/>
    /// <param name="conditionParamsJson">JSON representation of a <see cref="SignalLossCondition"/>.</param>
    /// <param name="context">Current sensor context including <see cref="SensorContext.LastReadingAt"/>.</param>
    /// <returns>
    /// <see langword="true"/> when no reading has been received, or the elapsed time since the
    /// last reading exceeds <see cref="SignalLossCondition.TimeoutMinutes"/>.
    /// </returns>
    public bool Evaluate(string conditionParamsJson, SensorContext context)
    {
        // No data at all means signal is lost
        if (context.LastReadingAt is null)
            return true;

        var condition = JsonSerializer.Deserialize<SignalLossCondition>(conditionParamsJson, JsonOptions);
        if (condition is null)
            return false;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var elapsed = now - context.LastReadingAt.Value;

        return elapsed > TimeSpan.FromMinutes(condition.TimeoutMinutes);
    }
}
