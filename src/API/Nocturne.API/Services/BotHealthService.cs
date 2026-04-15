using Nocturne.Core.Models.Alerts;

namespace Nocturne.API.Services;

public sealed class BotHealthService
{
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromMinutes(2);

    private static readonly Dictionary<string, ChannelType> PlatformToChannel = new()
    {
        ["discord"] = ChannelType.DiscordDm,
        ["slack"] = ChannelType.SlackDm,
        ["telegram"] = ChannelType.Telegram,
        ["whatsapp"] = ChannelType.WhatsApp,
    };

    private static readonly HashSet<ChannelType> AlwaysAvailable =
        [ChannelType.WebPush, ChannelType.Webhook];

    private static readonly HashSet<ChannelType> RequiresLinkTypes =
        [ChannelType.DiscordDm, ChannelType.SlackDm, ChannelType.Telegram, ChannelType.WhatsApp];

    private string[] _lastPlatforms = [];
    private DateTime _lastHeartbeat = DateTime.MinValue;
    private readonly object _lock = new();

    public void Record(string[] platforms, DateTime? timestamp = null)
    {
        lock (_lock)
        {
            _lastPlatforms = platforms;
            _lastHeartbeat = timestamp ?? DateTime.UtcNow;
        }
    }

    public IReadOnlyList<ChannelStatusEntry> GetChannelStatuses()
    {
        string[] platforms;
        DateTime heartbeat;

        lock (_lock)
        {
            platforms = _lastPlatforms;
            heartbeat = _lastHeartbeat;
        }

        var reportedChannels = platforms
            .Where(PlatformToChannel.ContainsKey)
            .Select(p => PlatformToChannel[p])
            .ToHashSet();

        var isStale = heartbeat != DateTime.MinValue
            && DateTime.UtcNow - heartbeat > StalenessThreshold;

        return Enum.GetValues<ChannelType>()
            .Select(ct =>
            {
                if (AlwaysAvailable.Contains(ct))
                {
                    return new ChannelStatusEntry
                    {
                        ChannelType = ct,
                        Status = ChannelStatus.Available,
                        RequiresLink = false,
                    };
                }

                if (!reportedChannels.Contains(ct))
                {
                    return new ChannelStatusEntry
                    {
                        ChannelType = ct,
                        Status = ChannelStatus.Unavailable,
                        Reason = ChannelUnavailableReason.AdapterNotConfigured,
                        RequiresLink = RequiresLinkTypes.Contains(ct),
                    };
                }

                if (isStale)
                {
                    return new ChannelStatusEntry
                    {
                        ChannelType = ct,
                        Status = ChannelStatus.Degraded,
                        Reason = ChannelUnavailableReason.HeartbeatStale,
                        RequiresLink = RequiresLinkTypes.Contains(ct),
                    };
                }

                return new ChannelStatusEntry
                {
                    ChannelType = ct,
                    Status = ChannelStatus.Available,
                    RequiresLink = RequiresLinkTypes.Contains(ct),
                };
            })
            .ToList();
    }
}

public class ChannelStatusEntry
{
    public ChannelType ChannelType { get; set; }
    public ChannelStatus Status { get; set; }
    public ChannelUnavailableReason? Reason { get; set; }
    public bool RequiresLink { get; set; }
}
