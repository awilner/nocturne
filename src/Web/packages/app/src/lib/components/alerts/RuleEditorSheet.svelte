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
  import { Button } from "$lib/components/ui/button";
  import { Loader2 } from "lucide-svelte";
  import GeneralTab from "./GeneralTab.svelte";
  import PresentationTab from "./PresentationTab.svelte";
  import SnoozeTab from "./SnoozeTab.svelte";
  import SchedulesTab from "./SchedulesTab.svelte";
  import { defaultSchedule } from "./types";
  import type { ClientConfiguration, EditableSchedule } from "./types";

  interface Props {
    open: boolean;
    rule: AlertRuleResponse | null;
    onSave: () => void;
  }

  let { open = $bindable(), rule, onSave }: Props = $props();

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

  // Schedules tab
  let schedules = $state<EditableSchedule[]>([defaultSchedule()]);

  // --- Computed ---
  let isEditMode = $derived(rule !== null);
  let title = $derived(isEditMode ? "Edit Rule" : "Create Rule");

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
      } else if (
        ct === AlertConditionType.Threshold ||
        (ct as string) === "threshold_low" ||
        (ct as string) === "threshold_high"
      ) {
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
            smartSnoozeExtendMinutes: cc.snooze?.smartSnoozeExtendMinutes ?? 10,
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

    getChannelStatuses()
      .then((res) => {
        availableChannels = (res?.channels ?? []).filter(
          (c) => c.status !== ChannelStatus.Unavailable
        );
      })
      .catch(() => {});
  });

  // --- Condition type mapping ---
  function getApiConditionType(): string {
    if (conditionType === AlertConditionType.Threshold) {
      return thresholdDirection === "above"
        ? "threshold_high"
        : "threshold_low";
    }
    return conditionType;
  }

  function getConditionParams(): Record<string, unknown> {
    switch (conditionType) {
      case AlertConditionType.Threshold:
        return {
          direction: thresholdDirection,
          value: thresholdValue,
          threshold: thresholdValue,
        };
      case AlertConditionType.RateOfChange:
        return {
          direction: rocDirection,
          rate: rocRate,
          rateThreshold: rocRate,
        };
      case AlertConditionType.SignalLoss:
        return {
          timeout_minutes: signalLossTimeout,
          minutes: signalLossTimeout,
        };
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
                })
              ),
            })
          ),
        })
      );

      const payload = {
        name,
        description: description || undefined,
        conditionType: isComposite
          ? AlertConditionType.Composite
          : getApiConditionType(),
        conditionParams: isComposite
          ? rule?.conditionParams
          : getConditionParams(),
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
          <Tabs.Trigger value="presentation" class="flex-1">
            Presentation
          </Tabs.Trigger>
          <Tabs.Trigger value="snooze" class="flex-1">Snooze</Tabs.Trigger>
          <Tabs.Trigger value="schedules" class="flex-1">
            Schedules
          </Tabs.Trigger>
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
            onSoundsChanged={(sounds) => {
              customSounds = sounds;
            }}
          />
        </Tabs.Content>

        <!-- Snooze Tab -->
        <Tabs.Content value="snooze" class="space-y-4 pt-4">
          <SnoozeTab bind:snooze={clientConfig.snooze} />
        </Tabs.Content>

        <!-- Schedules Tab -->
        <Tabs.Content value="schedules" class="space-y-4 pt-4">
          <SchedulesTab bind:schedules {availableChannels} />
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
