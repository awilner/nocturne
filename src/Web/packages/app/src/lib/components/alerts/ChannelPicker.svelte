<script lang="ts">
  import { onMount } from "svelte";
  import { getChannelStatuses } from "$api/generated/systems.generated.remote";
  import { getLinkedPlatforms } from "$api/generated/linkedPlatforms.generated.remote";
  import {
    ChannelType,
    ChannelStatus,
    type ChannelStatusEntry,
  } from "$api-clients";
  import { Switch } from "$lib/components/ui/switch";
  import { Label } from "$lib/components/ui/label";
  import { Input } from "$lib/components/ui/input";
  import {
    Bell,
    Webhook as WebhookIcon,
    MessageSquare,
    Send,
    AlertTriangle,
    Loader2,
  } from "lucide-svelte";

  type SelectedChannel = {
    channelType: ChannelType;
    destination: string;
    destinationLabel: string;
  };

  let { channels = $bindable([]) }: { channels: SelectedChannel[] } = $props();

  let loading = $state(true);
  let visibleChannels = $state<ChannelStatusEntry[]>([]);
  let linkedPlatforms = $state<string[]>([]);

  const channelMeta: Record<
    string,
    { icon: typeof Bell; label: string; description: string }
  > = {
    [ChannelType.WebPush]: {
      icon: Bell,
      label: "Browser Push",
      description: "Receive alerts directly in your browser",
    },
    [ChannelType.Webhook]: {
      icon: WebhookIcon,
      label: "Webhook",
      description: "Send alert data to a custom URL",
    },
    [ChannelType.DiscordDm]: {
      icon: MessageSquare,
      label: "Discord DM",
      description: "Send alerts as a Discord direct message",
    },
    [ChannelType.SlackDm]: {
      icon: MessageSquare,
      label: "Slack DM",
      description: "Send alerts as a Slack direct message",
    },
    [ChannelType.Telegram]: {
      icon: Send,
      label: "Telegram",
      description: "Send alerts to your Telegram chat",
    },
    [ChannelType.WhatsApp]: {
      icon: MessageSquare,
      label: "WhatsApp",
      description: "Send alerts to your WhatsApp",
    },
  };

  const platformMap: Partial<Record<ChannelType, string>> = {
    [ChannelType.DiscordDm]: "discord",
    [ChannelType.SlackDm]: "slack",
    [ChannelType.Telegram]: "telegram",
    [ChannelType.WhatsApp]: "whatsapp",
  };

  function isEnabled(channelType: ChannelType): boolean {
    return channels.some((c) => c.channelType === channelType);
  }

  function getWebhookUrl(channelType: ChannelType): string {
    return channels.find((c) => c.channelType === channelType)?.destination ?? "";
  }

  function toggleChannel(channelType: ChannelType, checked: boolean) {
    if (checked) {
      const meta = channelMeta[channelType];
      channels = [
        ...channels,
        {
          channelType,
          destination: "",
          destinationLabel: meta?.label ?? channelType,
        },
      ];
    } else {
      channels = channels.filter((c) => c.channelType !== channelType);
    }
  }

  function updateWebhookUrl(channelType: ChannelType, url: string) {
    channels = channels.map((c) =>
      c.channelType === channelType ? { ...c, destination: url } : c
    );
  }

  function isLinked(channelType: ChannelType): boolean {
    const platform = platformMap[channelType];
    if (!platform) return true;
    return linkedPlatforms.includes(platform);
  }

  function getPlatformName(channelType: ChannelType): string {
    const platform = platformMap[channelType];
    if (!platform) return "";
    return platform.charAt(0).toUpperCase() + platform.slice(1);
  }

  onMount(async () => {
    try {
      const [statusResult, platformResult] = await Promise.all([
        getChannelStatuses(),
        getLinkedPlatforms(),
      ]);

      linkedPlatforms = platformResult?.platforms ?? [];
      visibleChannels = (statusResult?.channels ?? []).filter(
        (c) => c.status !== ChannelStatus.Unavailable
      );
    } catch {
      visibleChannels = [];
    } finally {
      loading = false;
    }
  });
</script>

{#if loading}
  <div class="flex items-center justify-center py-8">
    <Loader2 class="h-6 w-6 animate-spin text-muted-foreground" />
  </div>
{:else if visibleChannels.length === 0}
  <p class="text-sm text-muted-foreground text-center py-4">
    No notification channels are currently available.
  </p>
{:else}
  <div class="space-y-3">
    {#each visibleChannels as channel (channel.channelType)}
      {@const ct = channel.channelType}
      {#if ct !== undefined}
      {@const meta = channelMeta[ct]}
      {@const enabled = isEnabled(ct)}
      {@const degraded = channel.status === ChannelStatus.Degraded}
      {@const needsLink = channel.requiresLink === true && !isLinked(ct)}
      {#if meta}
        <div>
          <div class="flex items-center justify-between p-3 rounded-lg border">
            <div class="flex items-center gap-3">
              <div
                class="flex items-center justify-center h-10 w-10 rounded-lg bg-primary/10"
              >
                <meta.icon class="h-5 w-5 text-primary" />
              </div>
              <div>
                <div class="flex items-center gap-2">
                  <Label>{meta.label}</Label>
                  {#if degraded}
                    <span title="Service hasn't reported recently — alerts may be delayed">
                      <AlertTriangle class="h-4 w-4 text-amber-500" />
                    </span>
                  {/if}
                </div>
                <p class="text-sm text-muted-foreground">{meta.description}</p>
              </div>
            </div>
            <Switch
              checked={enabled}
              onCheckedChange={(checked) => toggleChannel(ct, !!checked)}
            />
          </div>

          {#if enabled && needsLink}
            <p class="text-sm text-amber-600 mt-1 pl-13">
              Account not linked. Use /connect in {getPlatformName(ct)} to
              enable delivery.
            </p>
          {/if}

          {#if enabled && ct === ChannelType.Webhook}
            <div class="mt-2 pl-13">
              <Input
                type="url"
                placeholder="https://example.com/webhook"
                value={getWebhookUrl(ct)}
                oninput={(e) =>
                  updateWebhookUrl(ct, e.currentTarget.value)}
              />
            </div>
          {/if}
        </div>
      {/if}
      {/if}
    {/each}
  </div>
{/if}
