using Nocturne.Core.Contracts.Alerts;
using Nocturne.Core.Models.Alerts;

namespace Nocturne.API.Services.Alerts.Evaluators;

/// <summary>
/// Resolves an <see cref="AlertConditionType"/> to the corresponding <see cref="IConditionEvaluator"/>.
/// Registered as singleton; constructor takes all registered evaluators via DI.
/// </summary>
public class ConditionEvaluatorRegistry
{
    private readonly Dictionary<AlertConditionType, IConditionEvaluator> _evaluators;

    /// <summary>
    /// Initialises a new <see cref="ConditionEvaluatorRegistry"/> with all registered evaluators.
    /// </summary>
    /// <param name="evaluators">All <see cref="IConditionEvaluator"/> implementations registered in DI.</param>
    public ConditionEvaluatorRegistry(IEnumerable<IConditionEvaluator> evaluators)
    {
        _evaluators = evaluators.ToDictionary(e => e.ConditionType, e => e);
    }

    /// <summary>
    /// Returns the <see cref="IConditionEvaluator"/> for the specified <paramref name="conditionType"/>,
    /// or <see langword="null"/> if no evaluator is registered for that type.
    /// </summary>
    /// <param name="conditionType">The <see cref="AlertConditionType"/> to look up.</param>
    /// <returns>The matching evaluator, or <see langword="null"/>.</returns>
    public IConditionEvaluator? GetEvaluator(AlertConditionType conditionType)
    {
        _evaluators.TryGetValue(conditionType, out var evaluator);
        return evaluator;
    }

    /// <summary>
    /// Convenience overload for composite sub-condition routing where the type
    /// arrives as a raw JSON string from <see cref="ConditionNode.Type"/>.
    /// </summary>
    public IConditionEvaluator? GetEvaluator(string conditionTypeString)
    {
        if (Enum.TryParse<AlertConditionType>(conditionTypeString, ignoreCase: true, out var parsed))
            return GetEvaluator(parsed);

        // Try matching by EnumMember value (e.g. "rate_of_change")
        foreach (var (type, evaluator) in _evaluators)
        {
            var memberInfo = typeof(AlertConditionType).GetMember(type.ToString()).FirstOrDefault();
            var attr = memberInfo?.GetCustomAttributes(typeof(System.Runtime.Serialization.EnumMemberAttribute), false)
                .Cast<System.Runtime.Serialization.EnumMemberAttribute>()
                .FirstOrDefault();
            if (attr?.Value?.Equals(conditionTypeString, StringComparison.OrdinalIgnoreCase) == true)
                return evaluator;
        }

        return null;
    }
}
