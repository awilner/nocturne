using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Nocturne.Core.Models.Alerts;

[JsonConverter(typeof(JsonStringEnumConverter<ChannelStatus>))]
public enum ChannelStatus
{
    [EnumMember(Value = "available")]
    Available,

    [EnumMember(Value = "degraded")]
    Degraded,

    [EnumMember(Value = "unavailable")]
    Unavailable,
}
