using System.Net.Http.Json;
using Nocturne.Core.Models;
using Nocturne.Core.Models.Alerts;

namespace Nocturne.API.Services.Alerts.Providers;

internal sealed class ChatBotProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ChatBotProvider> logger)
{
    public static readonly HashSet<ChannelType> SupportedChannelTypes =
    [
        ChannelType.DiscordDm,
        ChannelType.DiscordChannel,
        ChannelType.SlackDm,
        ChannelType.SlackChannel,
        ChannelType.TelegramDm,
        ChannelType.TelegramGroup,
        ChannelType.WhatsAppDm,
    ];

    public async Task SendAsync(Guid deliveryId, ChannelType channelType, string destination, AlertPayload payload, CancellationToken ct)
    {
        var webUrl = configuration["WEB_URL"];
        if (string.IsNullOrEmpty(webUrl))
        {
            logger.LogWarning("WEB_URL not configured, cannot dispatch to chat bot");
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient("ChatBot");
            var dispatchUrl = $"{webUrl.TrimEnd('/')}/api/v4/bot/dispatch";

            var response = await client.PostAsJsonAsync(dispatchUrl, new
            {
                DeliveryId = deliveryId,
                ChannelType = channelType,
                Destination = destination,
                Payload = payload,
            }, ct);

            response.EnsureSuccessStatusCode();

            logger.LogDebug(
                "Chat bot alert dispatched for delivery {DeliveryId} via {ChannelType}",
                deliveryId, channelType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to dispatch chat bot alert for delivery {DeliveryId} via {ChannelType}",
                deliveryId, channelType);
            throw;
        }
    }
}
