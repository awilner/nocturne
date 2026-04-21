using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Nocturne.Core.Contracts.Alerts;
using Nocturne.Core.Models;
using Nocturne.Core.Models.Alerts;

namespace Nocturne.API.Services.Alerts.Evaluators;

/// <summary>
/// Evaluates a composite alert condition whose sub-conditions are combined with logical
/// <c>AND</c> or <c>OR</c> operators.
/// </summary>
/// <remarks>
/// Sub-condition routing is delegated to the <see cref="ConditionEvaluatorRegistry"/> resolved
/// lazily from the DI container to avoid circular dependencies. Each sub-condition is
/// re-serialised from the <see cref="ConditionNode"/> and forwarded to the appropriate
/// <see cref="IConditionEvaluator"/> implementation.
/// </remarks>
/// <seealso cref="IConditionEvaluator"/>
/// <seealso cref="ConditionEvaluatorRegistry"/>
public class CompositeEvaluator : IConditionEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceProvider _serviceProvider;
    private ConditionEvaluatorRegistry? _registry;

    /// <summary>
    /// Initialises a new <see cref="CompositeEvaluator"/>.
    /// </summary>
    /// <param name="serviceProvider">
    /// The DI service provider used for lazy resolution of <see cref="ConditionEvaluatorRegistry"/>.
    /// </param>
    public CompositeEvaluator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private ConditionEvaluatorRegistry Registry =>
        _registry ??= _serviceProvider.GetRequiredService<ConditionEvaluatorRegistry>();

    /// <inheritdoc/>
    public AlertConditionType ConditionType => AlertConditionType.Composite;

    /// <inheritdoc/>
    /// <param name="conditionParamsJson">JSON representation of a <see cref="CompositeCondition"/>.</param>
    /// <param name="context">Current sensor reading context passed to each sub-evaluator.</param>
    /// <returns>
    /// <see langword="true"/> when all (AND) or any (OR) sub-conditions evaluate to <see langword="true"/>;
    /// <see langword="false"/> if the condition is null, empty, or has an unrecognised operator.
    /// </returns>
    public bool Evaluate(string conditionParamsJson, SensorContext context)
    {
        var condition = JsonSerializer.Deserialize<CompositeCondition>(conditionParamsJson, JsonOptions);
        if (condition is null || condition.Conditions.Count == 0)
            return false;

        return condition.Operator.ToLowerInvariant() switch
        {
            "and" => condition.Conditions.All(node => EvaluateNode(node, context)),
            "or" => condition.Conditions.Any(node => EvaluateNode(node, context)),
            _ => false
        };
    }

    private bool EvaluateNode(ConditionNode node, SensorContext context)
    {
        var evaluator = Registry.GetEvaluator(node.Type);
        if (evaluator is null)
            return false;

        // Serialize the appropriate sub-condition back to JSON for the evaluator
        var paramsJson = node.Type.ToLowerInvariant() switch
        {
            "threshold" => JsonSerializer.Serialize(node.Threshold, JsonOptions),
            "rate_of_change" => JsonSerializer.Serialize(node.RateOfChange, JsonOptions),
            "signal_loss" => JsonSerializer.Serialize(node.SignalLoss, JsonOptions),
            "composite" => JsonSerializer.Serialize(node.Composite, JsonOptions),
            _ => "{}"
        };

        return evaluator.Evaluate(paramsJson, context);
    }
}
