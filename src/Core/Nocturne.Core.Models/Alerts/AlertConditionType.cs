using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Nocturne.Core.Models.Alerts;

/// <summary>
/// The type of condition an alert rule evaluates.
/// Used as a discriminator to route rules to the correct IConditionEvaluator.
/// </summary>
/// <seealso cref="AlertRuleSeverity"/>
[JsonConverter(typeof(JsonStringEnumConverter<AlertConditionType>))]
public enum AlertConditionType
{
    /// <summary>Glucose value crosses above or below a fixed threshold.</summary>
    [EnumMember(Value = "threshold"), JsonStringEnumMemberName("threshold")]
    Threshold,

    /// <summary>Glucose is rising or falling faster than a configured rate (mg/dL per minute).</summary>
    [EnumMember(Value = "rate_of_change"), JsonStringEnumMemberName("rate_of_change")]
    RateOfChange,

    /// <summary>No CGM data received within the configured time window.</summary>
    [EnumMember(Value = "signal_loss"), JsonStringEnumMemberName("signal_loss")]
    SignalLoss,

    /// <summary>Logical combination of multiple child conditions (AND/OR).</summary>
    [EnumMember(Value = "composite"), JsonStringEnumMemberName("composite")]
    Composite,
}
