using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Nocturne.Core.Models.Alerts;

/// <summary>
/// The type of condition an alert rule evaluates.
/// Used as a discriminator to route rules to the correct IConditionEvaluator.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AlertConditionType>))]
public enum AlertConditionType
{
    [EnumMember(Value = "threshold"), JsonStringEnumMemberName("threshold")]
    Threshold,

    [EnumMember(Value = "rate_of_change"), JsonStringEnumMemberName("rate_of_change")]
    RateOfChange,

    [EnumMember(Value = "signal_loss"), JsonStringEnumMemberName("signal_loss")]
    SignalLoss,

    [EnumMember(Value = "composite"), JsonStringEnumMemberName("composite")]
    Composite,
}
