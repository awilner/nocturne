using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Nocturne.Core.Models.Alerts;

[JsonConverter(typeof(JsonStringEnumConverter<ChannelType>))]
public enum ChannelType
{
    [EnumMember(Value = "web_push")]
    WebPush,

    [EnumMember(Value = "webhook")]
    Webhook,

    [EnumMember(Value = "discord_dm")]
    DiscordDm,

    [EnumMember(Value = "slack_dm")]
    SlackDm,

    [EnumMember(Value = "telegram")]
    Telegram,

    [EnumMember(Value = "whatsapp")]
    WhatsApp,
}
