<script lang="ts">
  import { onMount } from "svelte";
  import {
    createRule,
    updateRule,
  } from "$api/generated/alertRules.generated.remote";
  import { getSounds } from "$api/generated/alertCustomSounds.generated.remote";
  import { getChannelStatuses } from "$api/generated/systems.generated.remote";
  import {
    ChannelStatus,
    type ChannelStatusEntry,
    AlertConditionType,
    AlertRuleSeverity,
    ChannelType,
  } from "$api-clients";
  import type {
    AlertRuleResponse,
    AlertCustomSoundResponse,
    CreateAlertScheduleRequest,
    CreateAlertEscalationStepRequest,
    CreateAlertStepChannelRequest,
  } from "$api-clients";
  import * as Sheet from "$lib/components/ui/sheet";
  import * as Tabs from "$lib/components/ui/tabs";
  import * as Select from "$lib/components/ui/select";
  import { Input } from "$lib/components/ui/input";
  import { Button } from "$lib/components/ui/button";
  import { Switch } from "$lib/components/ui/switch";
  import { Label } from "$lib/components/ui/label";
  import { Badge } from "$lib/components/ui/badge";
  import { Separator } from "$lib/components/ui/separator";
  import {
    Loader2,
    Plus,
    X,
    Trash2,
    ChevronDown,
    ChevronUp,
  } from "lucide-svelte";
  import GeneralTab from "./GeneralTab.svelte";
  import PresentationTab from "./PresentationTab.svelte";

  interface Props {
    open: boolean;
    rule: AlertRuleResponse | null;
    onSave: () => void;
  }

  let { open = $bindable(), rule, onSave }: Props = $props();

  // --- Audio config type ---
  interface AudioConfig {
    enabled: boolean;
    sound: string;
    customSoundId: string | null;
    ascending: boolean;
    startVolume: number;
    maxVolume: number;
    ascendDurationSeconds: number;
    repeatCount: number;
  }

  interface VisualConfig {
    flashEnabled: boolean;
    flashColor: string;
    persistentBanner: boolean;
    wakeScreen: boolean;
  }

  interface SnoozeConfig {
    defaultMinutes: number;
    options: number[];
    maxCount: number;
    smartSnooze: boolean;
    smartSnoozeExtendMinutes: number;
  }

  interface ClientConfiguration {
    audio: AudioConfig;
    visual: VisualConfig;
    snooze: SnoozeConfig;
  }

  // --- Schedule editing types ---
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

  // --- Defaults ---
  function defaultClientConfig(): ClientConfiguration {
    return {
      audio: {
        enabled: true,
        sound: "alarm-default",
        customSoundId: null,
        ascending: false,
        startVolume: 50,
        maxVolume: 80,
        ascendDurationSeconds: 30,
        repeatCount: 2,
      },
      visual: {
        flashEnabled: false,
        flashColor: "#ff0000",
        persistentBanner: true,
        wakeScreen: false,
      },
      snooze: {
        defaultMinutes: 15,
        options: [5, 15, 30, 60],
        maxCount: 5,
        smartSnooze: false,
        smartSnoozeExtendMinutes: 10,
      },
    };
  }

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

  // --- State ---
  let activeTab = $state<string>("general");
  let saving = $state(false);
  let customSounds = $state<AlertCustomSoundResponse[]>([]);
  let availableChannels = $state<ChannelStatusEntry[]>([]);

  // General tab
  let name = $state("");
  let description = $state("");
  let severity = $state<AlertRuleSeverity>(AlertRuleSeverity.Normal);
  let conditionType = $state<AlertConditionType>(AlertConditionType.Threshold);
  let isComposite = $state(false);

  // Condition params
  let thresholdDirection = $state("below");
  let thresholdValue = $state(70);
  let rocDirection = $state("falling");
  let rocRate = $state(3.0);
  let signalLossTimeout = $state(15);

  let hysteresisMinutes = $state(5);
  let confirmationReadings = $state(1);
  let sortOrder = $state(0);
  let isEnabled = $state(true);

  // Presentation tab
  let clientConfig = $state<ClientConfiguration>(defaultClientConfig());

  // Snooze tab - new option input
  let newSnoozeOption = $state("");

  // Schedules tab
  let schedules = $state<EditableSchedule[]>([defaultSchedule()]);

  // --- Computed ---
  let isEditMode = $derived(rule !== null);
  let title = $derived(isEditMode ? "Edit Rule" : "Create Rule");

  const dayLabels = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

  // --- Initialization ---
  function initFromRule(r: AlertRuleResponse | null) {
    if (r) {
      name = r.name ?? "";
      description = r.description ?? "";
      severity = (r.severity as AlertRuleSeverity) ?? AlertRuleSeverity.Normal;
      isEnabled = r.isEnabled ?? true;
      hysteresisMinutes = r.hysteresisMinutes ?? 5;
      confirmationReadings = r.confirmationReadings ?? 1;
      sortOrder = r.sortOrder ?? 0;

      // Condition type
      const ct = r.conditionType ?? AlertConditionType.Threshold;
      if (ct === AlertConditionType.Composite) {
        isComposite = true;
        conditionType = AlertConditionType.Composite;
      } else if (ct === AlertConditionType.Threshold || (ct as string) === "threshold_low" || (ct as string) === "threshold_high") {
        isComposite = false;
        conditionType = AlertConditionType.Threshold;
      } else if (ct === AlertConditionType.RateOfChange) {
        isComposite = false;
        conditionType = AlertConditionType.RateOfChange;
      } else if (ct === AlertConditionType.SignalLoss) {
        isComposite = false;
        conditionType = AlertConditionType.SignalLoss;
      } else {
        isComposite = false;
        conditionType = ct as AlertConditionType;
      }

      // Condition params
      const params = r.conditionParams;
      if (params) {
        if (conditionType === AlertConditionType.Threshold) {
          if (params.direction === "above") {
            thresholdDirection = "above";
          } else {
            thresholdDirection = "below";
          }
          thresholdValue = params.threshold ?? params.value ?? 70;
        } else if (conditionType === AlertConditionType.RateOfChange) {
          rocDirection = params.direction ?? "falling";
          rocRate = params.rateThreshold ?? params.rate ?? 3.0;
        } else if (conditionType === AlertConditionType.SignalLoss) {
          signalLossTimeout = params.minutes ?? params.timeout_minutes ?? 15;
        }
      }

      // Client configuration
      const cc = r.clientConfiguration;
      if (cc) {
        clientConfig = {
          audio: {
            enabled: cc.audio?.enabled ?? true,
            sound: cc.audio?.sound ?? "alarm-default",
            customSoundId: cc.audio?.customSoundId ?? null,
            ascending: cc.audio?.ascending ?? false,
            startVolume: cc.audio?.startVolume ?? 50,
            maxVolume: cc.audio?.maxVolume ?? 80,
            ascendDurationSeconds: cc.audio?.ascendDurationSeconds ?? 30,
            repeatCount: cc.audio?.repeatCount ?? 2,
          },
          visual: {
            flashEnabled: cc.visual?.flashEnabled ?? false,
            flashColor: cc.visual?.flashColor ?? "#ff0000",
            persistentBanner: cc.visual?.persistentBanner ?? true,
            wakeScreen: cc.visual?.wakeScreen ?? false,
          },
          snooze: {
            defaultMinutes: cc.snooze?.defaultMinutes ?? 15,
            options: cc.snooze?.options ?? [5, 15, 30, 60],
            maxCount: cc.snooze?.maxCount ?? 5,
            smartSnooze: cc.snooze?.smartSnooze ?? false,
            smartSnoozeExtendMinutes:
              cc.snooze?.smartSnoozeExtendMinutes ?? 10,
          },
        };
      } else {
        clientConfig = defaultClientConfig();
      }

      // Schedules
      if (r.schedules && r.schedules.length > 0) {
        schedules = r.schedules.map((s) => ({
          name: s.name ?? "Default Schedule",
          isDefault: s.isDefault ?? false,
          daysOfWeek: s.daysOfWeek ?? [],
          startTime: s.startTime ?? "00:00",
          endTime: s.endTime ?? "23:59",
          timezone: s.timezone ?? "UTC",
          escalationSteps: (s.escalationSteps ?? [])
            .sort((a, b) => (a.stepOrder ?? 0) - (b.stepOrder ?? 0))
            .map((step) => ({
              stepOrder: step.stepOrder ?? 0,
              delaySeconds: step.delaySeconds ?? 0,
              channels: (step.channels ?? []).map((ch) => ({
                channelType: ch.channelType ?? ChannelType.WebPush,
                destination: ch.destination ?? "",
                destinationLabel: ch.destinationLabel ?? "",
              })),
            })),
          expanded: false,
        }));
      } else {
        schedules = [defaultSchedule()];
      }
    } else {
      // Create mode defaults
      name = "";
      description = "";
      severity = AlertRuleSeverity.Normal;
      conditionType = AlertConditionType.Threshold;
      isComposite = false;
      thresholdDirection = "below";
      thresholdValue = 70;
      rocDirection = "falling";
      rocRate = 3.0;
      signalLossTimeout = 15;
      hysteresisMinutes = 5;
      confirmationReadings = 1;
      sortOrder = 0;
      isEnabled = true;
      clientConfig = defaultClientConfig();
      schedules = [defaultSchedule()];
    }
    activeTab = "general";
    newSnoozeOption = "";
  }

  $effect(() => {
    if (open) {
      initFromRule(rule);
    }
  });

  // Load custom sounds and available channels on mount
  onMount(async () => {
    try {
      const result = await getSounds();
      customSounds = Array.isArray(result) ? result : [];
    } catch {
      // Sounds unavailable
    }

    getChannelStatuses().then(res => {
      availableChannels = (res?.channels ?? []).filter(
        c => c.status !== ChannelStatus.Unavailable
      );
    }).catch(() => {});
  });

  // --- Condition type mapping ---
  function getApiConditionType(): string {
    if (conditionType === AlertConditionType.Threshold) {
      return thresholdDirection === "above" ? "threshold_high" : "threshold_low";
    }
    return conditionType;
  }

  function getConditionParams(): Record<string, unknown> {
    switch (conditionType) {
      case AlertConditionType.Threshold:
        return { direction: thresholdDirection, value: thresholdValue, threshold: thresholdValue };
      case AlertConditionType.RateOfChange:
        return { direction: rocDirection, rate: rocRate, rateThreshold: rocRate };
      case AlertConditionType.SignalLoss:
        return { timeout_minutes: signalLossTimeout, minutes: signalLossTimeout };
      default:
        return {};
    }
  }

  // --- Save ---
  async function handleSave() {
    saving = true;
    try {
      const schedulesPayload: CreateAlertScheduleRequest[] = schedules.map(
        (s) => ({
          name: s.name || undefined,
          isDefault: s.isDefault,
          daysOfWeek:
            s.daysOfWeek.length === 0 || s.daysOfWeek.length === 7
              ? undefined
              : s.daysOfWeek,
          startTime: s.isDefault ? undefined : s.startTime || undefined,
          endTime: s.isDefault ? undefined : s.endTime || undefined,
          timezone: s.timezone || undefined,
          escalationSteps: s.escalationSteps.map(
            (step): CreateAlertEscalationStepRequest => ({
              stepOrder: step.stepOrder,
              delaySeconds: step.delaySeconds,
              channels: step.channels.map(
                (ch): CreateAlertStepChannelRequest => ({
                  channelType: ch.channelType as ChannelType,
                  destination: ch.destination || undefined,
                  destinationLabel: ch.destinationLabel || undefined,
                }),
              ),
            }),
          ),
        }),
      );

      const payload = {
        name,
        description: description || undefined,
        conditionType: isComposite ? AlertConditionType.Composite : getApiConditionType(),
        conditionParams: isComposite ? rule?.conditionParams : getConditionParams(),
        hysteresisMinutes,
        confirmationReadings,
        isEnabled,
        sortOrder,
        severity: severity || undefined,
        clientConfiguration: clientConfig,
        schedules: schedulesPayload,
      };

      if (isEditMode && rule?.id) {
        await updateRule({ id: rule.id, request: payload });
      } else {
        await createRule(payload);
      }

      onSave();
      open = false;
    } catch {
      // Error handled by remote function
    } finally {
      saving = false;
    }
  }

  // --- Snooze options ---
  function addSnoozeOption() {
    const val = parseInt(newSnoozeOption, 10);
    if (!isNaN(val) && val > 0 && !clientConfig.snooze.options.includes(val)) {
      clientConfig.snooze.options = [
        ...clientConfig.snooze.options,
        val,
      ].sort((a, b) => a - b);
      newSnoozeOption = "";
    }
  }

  function removeSnoozeOption(val: number) {
    clientConfig.snooze.options = clientConfig.snooze.options.filter(
      (o) => o !== val,
    );
  }

  // --- Schedule management ---
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

  // --- Escalation step management ---
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

  const channelTypeLabels: Partial<Record<ChannelType, string>> = {
    [ChannelType.WebPush]: "Web Push",
    [ChannelType.Webhook]: "Webhook",
    [ChannelType.DiscordDm]: "Discord DM",
    [ChannelType.SlackDm]: "Slack DM",
    [ChannelType.Telegram]: "Telegram",
    [ChannelType.WhatsApp]: "WhatsApp",
  };
</script>

<Sheet.Root bind:open>
  <Sheet.Content side="right" class="w-full sm:max-w-xl overflow-y-auto">
    <Sheet.Header>
      <Sheet.Title>{title}</Sheet.Title>
      <Sheet.Description>
        {isEditMode
          ? "Modify the alert rule configuration"
          : "Configure a new alert rule"}
      </Sheet.Description>
    </Sheet.Header>

    <div class="flex-1 overflow-y-auto px-1">
      <Tabs.Root bind:value={activeTab}>
        <Tabs.List class="w-full">
          <Tabs.Trigger value="general" class="flex-1">General</Tabs.Trigger>
          <Tabs.Trigger value="presentation" class="flex-1"
            >Presentation</Tabs.Trigger
          >
          <Tabs.Trigger value="snooze" class="flex-1">Snooze</Tabs.Trigger>
          <Tabs.Trigger value="schedules" class="flex-1"
            >Schedules</Tabs.Trigger
          >
        </Tabs.List>

        <!-- General Tab -->
        <Tabs.Content value="general" class="space-y-4 pt-4">
          <GeneralTab
            bind:name
            bind:description
            bind:severity
            bind:conditionType
            {isComposite}
            bind:thresholdDirection
            bind:thresholdValue
            bind:rocDirection
            bind:rocRate
            bind:signalLossTimeout
            bind:hysteresisMinutes
            bind:confirmationReadings
            bind:sortOrder
            bind:isEnabled
          />
        </Tabs.Content>

        <!-- Presentation Tab -->
        <Tabs.Content value="presentation" class="space-y-6 pt-4">
          <PresentationTab
            bind:clientConfig
            {customSounds}
            onSoundsChanged={(sounds) => { customSounds = sounds; }}
          />
        </Tabs.Content>

        <!-- Snooze Tab -->
        <Tabs.Content value="snooze" class="space-y-4 pt-4">
          <div class="space-y-2">
            <Label for="snooze-default">Default Snooze Duration (minutes)</Label>
            <Input
              id="snooze-default"
              type="number"
              bind:value={clientConfig.snooze.defaultMinutes}
            />
          </div>

          <div class="space-y-2">
            <Label>Snooze Options</Label>
            <div class="flex flex-wrap gap-2">
              {#each clientConfig.snooze.options as opt}
                <Badge variant="secondary" class="gap-1 pr-1">
                  {opt}m
                  <button
                    class="ml-1 rounded-full hover:bg-muted-foreground/20 p-0.5"
                    onclick={() => removeSnoozeOption(opt)}
                  >
                    <X class="h-3 w-3" />
                  </button>
                </Badge>
              {/each}
            </div>
            <div class="flex gap-2">
              <Input
                placeholder="Minutes"
                type="number"
                bind:value={newSnoozeOption}
                class="w-24"
                onkeydown={(e: KeyboardEvent) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    addSnoozeOption();
                  }
                }}
              />
              <Button variant="outline" size="sm" onclick={addSnoozeOption}>
                Add
              </Button>
            </div>
          </div>

          <div class="space-y-2">
            <Label for="snooze-max-count">Max Snooze Count</Label>
            <Input
              id="snooze-max-count"
              type="number"
              bind:value={clientConfig.snooze.maxCount}
            />
          </div>

          <Separator />

          <div class="flex items-center justify-between">
            <Label>Smart Snooze</Label>
            <Switch bind:checked={clientConfig.snooze.smartSnooze} />
          </div>

          {#if clientConfig.snooze.smartSnooze}
            <div class="space-y-2">
              <Label for="smart-snooze-extend"
                >Smart Snooze Extend (minutes)</Label
              >
              <Input
                id="smart-snooze-extend"
                type="number"
                bind:value={clientConfig.snooze.smartSnoozeExtendMinutes}
              />
              <p class="text-xs text-muted-foreground">
                Automatically extends snooze when glucose trend is favorable
              </p>
            </div>
          {/if}
        </Tabs.Content>

        <!-- Schedules Tab -->
        <Tabs.Content value="schedules" class="space-y-4 pt-4">
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
        </Tabs.Content>
      </Tabs.Root>
    </div>

    <Sheet.Footer class="mt-4">
      <Button variant="outline" onclick={() => (open = false)}>Cancel</Button>
      <Button onclick={handleSave} disabled={saving || !name.trim()}>
        {#if saving}
          <Loader2 class="h-4 w-4 mr-2 animate-spin" />
        {/if}
        {isEditMode ? "Update Rule" : "Create Rule"}
      </Button>
    </Sheet.Footer>
  </Sheet.Content>
</Sheet.Root>
