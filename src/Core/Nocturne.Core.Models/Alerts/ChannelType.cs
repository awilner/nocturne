using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Nocturne.Core.Models.Alerts;

[JsonConverter(typeof(JsonStringEnumConverter<ChannelType>))]
public enum ChannelType
{
    [EnumMember(Value = "web_push"), JsonStringEnumMemberName("web_push")]
    WebPush,

    [EnumMember(Value = "webhook"), JsonStringEnumMemberName("webhook")]
    Webhook,

    [EnumMember(Value = "discord_dm"), JsonStringEnumMemberName("discord_dm")]
    DiscordDm,

    [EnumMember(Value = "discord_channel"), JsonStringEnumMemberName("discord_channel")]
    DiscordChannel,

    [EnumMember(Value = "slack_dm"), JsonStringEnumMemberName("slack_dm")]
    SlackDm,

    [EnumMember(Value = "slack_channel"), JsonStringEnumMemberName("slack_channel")]
    SlackChannel,

    [EnumMember(Value = "telegram"), JsonStringEnumMemberName("telegram")]
    Telegram,

    [EnumMember(Value = "telegram_dm"), JsonStringEnumMemberName("telegram_dm")]
    TelegramDm,

    [EnumMember(Value = "telegram_group"), JsonStringEnumMemberName("telegram_group")]
    TelegramGroup,

    [EnumMember(Value = "whatsapp"), JsonStringEnumMemberName("whatsapp")]
    WhatsApp,

    [EnumMember(Value = "whatsapp_dm"), JsonStringEnumMemberName("whatsapp_dm")]
    WhatsAppDm,
}
