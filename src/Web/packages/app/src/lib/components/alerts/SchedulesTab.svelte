<script lang="ts">
  import { ChannelType, type ChannelStatusEntry } from "$api-clients";
  import * as Select from "$lib/components/ui/select";
  import { Input } from "$lib/components/ui/input";
  import { Button } from "$lib/components/ui/button";
  import { Switch } from "$lib/components/ui/switch";
  import { Label } from "$lib/components/ui/label";
  import { Badge } from "$lib/components/ui/badge";
  import { Separator } from "$lib/components/ui/separator";
  import { Plus, X, Trash2, ChevronDown, ChevronUp } from "lucide-svelte";

  interface EditableChannel {
    channelType: ChannelType | string;
    destination: string;
    destinationLabel: string;
  }

  interface EditableStep {
    stepOrder: number;
    delaySeconds: number;
    channels: EditableChannel[];
  }

  interface EditableSchedule {
    name: string;
    isDefault: boolean;
    daysOfWeek: number[];
    startTime: string;
    endTime: string;
    timezone: string;
    escalationSteps: EditableStep[];
    expanded: boolean;
  }

  interface Props {
    schedules: EditableSchedule[];
    availableChannels: ChannelStatusEntry[];
  }

  let { schedules = $bindable(), availableChannels }: Props = $props();

  const dayLabels = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

  const channelTypeLabels: Partial<Record<ChannelType, string>> = {
    [ChannelType.WebPush]: "Web Push",
    [ChannelType.Webhook]: "Webhook",
    [ChannelType.DiscordDm]: "Discord DM",
    [ChannelType.SlackDm]: "Slack DM",
    [ChannelType.Telegram]: "Telegram",
    [ChannelType.WhatsApp]: "WhatsApp",
  };

  function defaultSchedule(): EditableSchedule {
    return {
      name: "Default Schedule",
      isDefault: true,
      daysOfWeek: [],
      startTime: "00:00",
      endTime: "23:59",
      timezone: "UTC",
      escalationSteps: [
        {
          stepOrder: 0,
          delaySeconds: 0,
          channels: [
            {
              channelType: ChannelType.WebPush,
              destination: "",
              destinationLabel: "",
            },
          ],
        },
      ],
      expanded: true,
    };
  }

  function addSchedule() {
    const newSched = defaultSchedule();
    newSched.isDefault = false;
    newSched.name = `Schedule ${schedules.length + 1}`;
    schedules = [...schedules, newSched];
  }

  function removeSchedule(index: number) {
    if (schedules.length <= 1) return;
    schedules = schedules.filter((_, i) => i !== index);
  }

  function toggleScheduleDefault(index: number) {
    schedules = schedules.map((s, i) => ({
      ...s,
      isDefault: i === index,
      expanded: s.expanded,
    }));
  }

  function toggleScheduleExpand(index: number) {
    schedules = schedules.map((s, i) => ({
      ...s,
      expanded: i === index ? !s.expanded : s.expanded,
    }));
  }

  function toggleDay(schedIndex: number, day: number) {
    const sched = schedules[schedIndex];
    if (sched.daysOfWeek.includes(day)) {
      sched.daysOfWeek = sched.daysOfWeek.filter((d) => d !== day);
    } else {
      sched.daysOfWeek = [...sched.daysOfWeek, day].sort();
    }
    schedules = [...schedules];
  }

  function addStep(schedIndex: number) {
    const sched = schedules[schedIndex];
    const newStep: EditableStep = {
      stepOrder: sched.escalationSteps.length,
      delaySeconds: 60,
      channels: [],
    };
    sched.escalationSteps = [...sched.escalationSteps, newStep];
    schedules = [...schedules];
  }

  function removeStep(schedIndex: number, stepIndex: number) {
    const sched = schedules[schedIndex];
    if (stepIndex === 0) return;
    sched.escalationSteps = sched.escalationSteps
      .filter((_, i) => i !== stepIndex)
      .map((s, i) => ({ ...s, stepOrder: i }));
    schedules = [...schedules];
  }

  function addChannel(schedIndex: number, stepIndex: number) {
    const step = schedules[schedIndex].escalationSteps[stepIndex];
    const defaultType = availableChannels[0]?.channelType ?? ChannelType.WebPush;
    step.channels = [
      ...step.channels,
      { channelType: defaultType, destination: "", destinationLabel: "" },
    ];
    schedules = [...schedules];
  }

  function removeChannel(
    schedIndex: number,
    stepIndex: number,
    channelIndex: number,
  ) {
    const step = schedules[schedIndex].escalationSteps[stepIndex];
    step.channels = step.channels.filter((_, i) => i !== channelIndex);
    schedules = [...schedules];
  }
</script>

<div class="space-y-4">
  {#each schedules as schedule, schedIdx}
    <div class="rounded-md border">
      <!-- Schedule header -->
      <button
        class="flex items-center justify-between w-full p-3 text-left hover:bg-muted/50 transition-colors"
        onclick={() => toggleScheduleExpand(schedIdx)}
      >
        <div class="flex items-center gap-2">
          <span class="text-sm font-medium">
            {schedule.name || "Unnamed Schedule"}
          </span>
          {#if schedule.isDefault}
            <Badge variant="secondary">Default</Badge>
          {/if}
        </div>
        {#if schedule.expanded}
          <ChevronUp class="h-4 w-4 text-muted-foreground" />
        {:else}
          <ChevronDown class="h-4 w-4 text-muted-foreground" />
        {/if}
      </button>

      {#if schedule.expanded}
        <div class="border-t p-3 space-y-4">
          <div class="space-y-2">
            <Label>Name</Label>
            <Input
              bind:value={schedule.name}
              placeholder="Schedule name"
            />
          </div>

          <div class="flex items-center justify-between">
            <Label>Default Schedule</Label>
            <Switch
              checked={schedule.isDefault}
              onCheckedChange={() => toggleScheduleDefault(schedIdx)}
            />
          </div>

          {#if !schedule.isDefault}
            <div class="grid grid-cols-2 gap-4">
              <div class="space-y-2">
                <Label>Start Time</Label>
                <Input type="time" bind:value={schedule.startTime} />
              </div>
              <div class="space-y-2">
                <Label>End Time</Label>
                <Input type="time" bind:value={schedule.endTime} />
              </div>
            </div>
          {/if}

          <div class="space-y-2">
            <Label>Days of Week</Label>
            <div class="flex gap-1">
              {#each dayLabels as dayLabel, dayIdx}
                <button
                  class="h-8 w-10 rounded-md border text-xs font-medium transition-colors {schedule.daysOfWeek.includes(
                    dayIdx,
                  )
                    ? 'bg-primary text-primary-foreground'
                    : 'bg-background hover:bg-muted'}"
                  onclick={() => toggleDay(schedIdx, dayIdx)}
                >
                  {dayLabel}
                </button>
              {/each}
            </div>
            <p class="text-xs text-muted-foreground">
              {schedule.daysOfWeek.length === 0 ||
              schedule.daysOfWeek.length === 7
                ? "Every day"
                : `${schedule.daysOfWeek.map((d) => dayLabels[d]).join(", ")}`}
            </p>
          </div>

          <div class="space-y-2">
            <Label>Timezone</Label>
            <Input
              bind:value={schedule.timezone}
              placeholder="UTC"
            />
          </div>

          <Separator />

          <!-- Escalation Steps -->
          <div class="space-y-3">
            <h4 class="text-sm font-medium">Escalation Steps</h4>

            {#each schedule.escalationSteps as step, stepIdx}
              <div class="relative pl-4 border-l-2 border-muted pb-3">
                <div class="space-y-3">
                  <div class="flex items-center justify-between">
                    <span class="text-sm font-medium"
                      >Step {stepIdx + 1}</span
                    >
                    {#if stepIdx > 0}
                      <Button
                        variant="ghost"
                        size="icon"
                        class="h-7 w-7 text-destructive"
                        onclick={() =>
                          removeStep(schedIdx, stepIdx)}
                      >
                        <Trash2 class="h-3 w-3" />
                      </Button>
                    {/if}
                  </div>

                  <div class="space-y-2">
                    <Label>Delay (seconds)</Label>
                    <Input
                      type="number"
                      bind:value={step.delaySeconds}
                      disabled={stepIdx === 0}
                    />
                    {#if stepIdx === 0}
                      <p class="text-xs text-muted-foreground">
                        First step fires immediately
                      </p>
                    {/if}
                  </div>

                  <!-- Channels -->
                  <div class="space-y-2">
                    {#each step.channels as channel, chIdx}
                      <div
                        class="flex items-start gap-2 p-2 rounded-md border bg-background"
                      >
                        <div class="flex-1 space-y-2">
                          <Select.Root
                            type="single"
                            bind:value={channel.channelType}
                          >
                            <Select.Trigger>
                              {channelTypeLabels[
                                channel.channelType as ChannelType
                              ] ?? channel.channelType}
                            </Select.Trigger>
                            <Select.Content>
                              {#each availableChannels as ch}
                                <Select.Item
                                  value={ch.channelType ?? ""}
                                  label={channelTypeLabels[ch.channelType as ChannelType] ?? ch.channelType ?? ""}
                                />
                              {/each}
                              {#if availableChannels.length === 0}
                                <Select.Item value={ChannelType.WebPush} label="Web Push" />
                                <Select.Item value={ChannelType.Webhook} label="Webhook" />
                              {/if}
                            </Select.Content>
                          </Select.Root>
                          <Input
                            bind:value={channel.destination}
                            placeholder="Destination"
                          />
                          <Input
                            bind:value={channel.destinationLabel}
                            placeholder="Label (optional)"
                          />
                        </div>
                        <Button
                          variant="ghost"
                          size="icon"
                          class="h-7 w-7 text-destructive shrink-0"
                          onclick={() =>
                            removeChannel(
                              schedIdx,
                              stepIdx,
                              chIdx,
                            )}
                        >
                          <X class="h-3 w-3" />
                        </Button>
                      </div>
                    {/each}

                    <Button
                      variant="outline"
                      size="sm"
                      onclick={() =>
                        addChannel(schedIdx, stepIdx)}
                    >
                      <Plus class="h-3 w-3 mr-1" />
                      Add Channel
                    </Button>
                  </div>
                </div>
              </div>
            {/each}

            <Button
              variant="outline"
              size="sm"
              onclick={() => addStep(schedIdx)}
            >
              <Plus class="h-3 w-3 mr-1" />
              Add Step
            </Button>
          </div>

          <Separator />

          <Button
            variant="outline"
            size="sm"
            class="text-destructive"
            disabled={schedules.length <= 1}
            onclick={() => removeSchedule(schedIdx)}
          >
            <Trash2 class="h-3 w-3 mr-1" />
            Remove Schedule
          </Button>
        </div>
      {/if}
    </div>
  {/each}

  <Button variant="outline" onclick={addSchedule}>
    <Plus class="h-4 w-4 mr-2" />
    Add Schedule
  </Button>
</div>
