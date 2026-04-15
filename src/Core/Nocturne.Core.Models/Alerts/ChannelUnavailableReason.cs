using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Nocturne.Core.Models.Alerts;

[JsonConverter(typeof(JsonStringEnumConverter<ChannelUnavailableReason>))]
public enum ChannelUnavailableReason
{
    [EnumMember(Value = "adapter_not_configured")]
    AdapterNotConfigured,

    [EnumMember(Value = "heartbeat_stale")]
    HeartbeatStale,
}
