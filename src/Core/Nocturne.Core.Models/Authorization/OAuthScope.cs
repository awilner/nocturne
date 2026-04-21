using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Nocturne.Core.Models.Authorization;

/// <summary>
/// Backend-owned OAuth scope taxonomy exposed to generated frontend clients.
/// Values intentionally match the RFC 6749 scope strings used on the wire.
/// </summary>
/// <seealso cref="OAuthScopes"/>
/// <seealso cref="ScopeTranslator"/>
[JsonConverter(typeof(JsonStringEnumConverter<OAuthScope>))]
public enum OAuthScope
{
    [EnumMember(Value = "entries.read"), JsonStringEnumMemberName("entries.read")]
    EntriesRead,

    [EnumMember(Value = "entries.readwrite"), JsonStringEnumMemberName("entries.readwrite")]
    EntriesReadWrite,

    [EnumMember(Value = "treatments.read"), JsonStringEnumMemberName("treatments.read")]
    TreatmentsRead,

    [EnumMember(Value = "treatments.readwrite"), JsonStringEnumMemberName("treatments.readwrite")]
    TreatmentsReadWrite,

    [EnumMember(Value = "devicestatus.read"), JsonStringEnumMemberName("devicestatus.read")]
    DeviceStatusRead,

    [EnumMember(Value = "devicestatus.readwrite"), JsonStringEnumMemberName("devicestatus.readwrite")]
    DeviceStatusReadWrite,

    [EnumMember(Value = "profile.read"), JsonStringEnumMemberName("profile.read")]
    ProfileRead,

    [EnumMember(Value = "profile.readwrite"), JsonStringEnumMemberName("profile.readwrite")]
    ProfileReadWrite,

    [EnumMember(Value = "notifications.read"), JsonStringEnumMemberName("notifications.read")]
    NotificationsRead,

    [EnumMember(Value = "notifications.readwrite"), JsonStringEnumMemberName("notifications.readwrite")]
    NotificationsReadWrite,

    [EnumMember(Value = "reports.read"), JsonStringEnumMemberName("reports.read")]
    ReportsRead,

    [EnumMember(Value = "identity.read"), JsonStringEnumMemberName("identity.read")]
    IdentityRead,

    [EnumMember(Value = "sharing.readwrite"), JsonStringEnumMemberName("sharing.readwrite")]
    SharingReadWrite,

    [EnumMember(Value = "heartrate.read"), JsonStringEnumMemberName("heartrate.read")]
    HeartRateRead,

    [EnumMember(Value = "heartrate.readwrite"), JsonStringEnumMemberName("heartrate.readwrite")]
    HeartRateReadWrite,

    [EnumMember(Value = "stepcount.read"), JsonStringEnumMemberName("stepcount.read")]
    StepCountRead,

    [EnumMember(Value = "stepcount.readwrite"), JsonStringEnumMemberName("stepcount.readwrite")]
    StepCountReadWrite,

    [EnumMember(Value = "health.read"), JsonStringEnumMemberName("health.read")]
    HealthRead,

    [EnumMember(Value = "health.readwrite"), JsonStringEnumMemberName("health.readwrite")]
    HealthReadWrite,

    [EnumMember(Value = "*"), JsonStringEnumMemberName("*")]
    FullAccess
}
