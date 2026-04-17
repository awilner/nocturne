using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Nocturne.Core.Models.Alerts;

/// <summary>
/// Severity level for an alert rule. Critical alerts bypass quiet hours.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AlertRuleSeverity>))]
public enum AlertRuleSeverity
{
    [EnumMember(Value = "normal"), JsonStringEnumMemberName("normal")]
    Normal,

    [EnumMember(Value = "critical"), JsonStringEnumMemberName("critical")]
    Critical,
}
