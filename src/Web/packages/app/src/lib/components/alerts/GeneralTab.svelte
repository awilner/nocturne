<script lang="ts">
  import {
    AlertConditionType,
    AlertRuleSeverity,
  } from "$api-clients";
  import * as Select from "$lib/components/ui/select";
  import { Input } from "$lib/components/ui/input";
  import { Switch } from "$lib/components/ui/switch";
  import { Label } from "$lib/components/ui/label";
  import { Badge } from "$lib/components/ui/badge";
  import { Separator } from "$lib/components/ui/separator";
  import { glucoseUnits } from "$lib/stores/appearance-store.svelte";
  import { bgValue, bgLabel, convertFromDisplayUnits } from "$lib/utils/formatting";

  interface Props {
    name: string;
    description: string;
    severity: AlertRuleSeverity;
    conditionType: AlertConditionType;
    isComposite: boolean;
    thresholdDirection: string;
    thresholdValue: number;
    rocDirection: string;
    rocRate: number;
    signalLossTimeout: number;
    hysteresisMinutes: number;
    confirmationReadings: number;
    sortOrder: number;
    isEnabled: boolean;
  }

  let {
    name = $bindable(),
    description = $bindable(),
    severity = $bindable(),
    conditionType = $bindable(),
    isComposite,
    thresholdDirection = $bindable(),
    thresholdValue = $bindable(),
    rocDirection = $bindable(),
    rocRate = $bindable(),
    signalLossTimeout = $bindable(),
    hysteresisMinutes = $bindable(),
    confirmationReadings = $bindable(),
    sortOrder = $bindable(),
    isEnabled = $bindable(),
  }: Props = $props();

  const conditionTypeLabels: Partial<Record<AlertConditionType, string>> = {
    [AlertConditionType.Threshold]: "Threshold",
    [AlertConditionType.RateOfChange]: "Rate of Change",
    [AlertConditionType.SignalLoss]: "Signal Loss",
  };

  const severityLabels: Partial<Record<AlertRuleSeverity, string>> = {
    [AlertRuleSeverity.Normal]: "Normal",
    [AlertRuleSeverity.Critical]: "Critical",
  };

  const thresholdDirLabels: Record<string, string> = {
    below: "Below",
    above: "Above",
  };

  const rocDirLabels: Record<string, string> = {
    falling: "Falling",
    rising: "Rising",
  };

  let unitLabel = $derived(bgLabel());
  let displayThreshold = $derived(bgValue(thresholdValue));
  let displayRocRate = $derived(bgValue(rocRate));

  function onThresholdInput(e: Event) {
    const input = e.currentTarget as HTMLInputElement;
    const val = parseFloat(input.value);
    if (!Number.isNaN(val)) {
      thresholdValue = convertFromDisplayUnits(val, glucoseUnits.current);
    }
  }

  function onRocRateInput(e: Event) {
    const input = e.currentTarget as HTMLInputElement;
    const val = parseFloat(input.value);
    if (!Number.isNaN(val)) {
      rocRate = convertFromDisplayUnits(val, glucoseUnits.current);
    }
  }
</script>

<div class="space-y-4">
  <div class="space-y-2">
    <Label for="rule-name">Name</Label>
    <Input id="rule-name" bind:value={name} placeholder="Rule name" />
  </div>

  <div class="space-y-2">
    <Label for="rule-description">Description (optional)</Label>
    <Input
      id="rule-description"
      bind:value={description}
      placeholder="Brief description"
    />
  </div>

  <div class="space-y-2">
    <Label for="rule-severity">Severity</Label>
    <Select.Root type="single" bind:value={severity}>
      <Select.Trigger id="rule-severity">
        {severityLabels[severity] ?? severity}
      </Select.Trigger>
      <Select.Content>
        <Select.Item value={AlertRuleSeverity.Normal} label="Normal" />
        <Select.Item value={AlertRuleSeverity.Critical} label="Critical" />
      </Select.Content>
    </Select.Root>
    {#if severity === AlertRuleSeverity.Critical}
      <p class="text-xs text-muted-foreground">
        Critical alerts bypass quiet hours
      </p>
    {/if}
  </div>

  <Separator />

  <div class="space-y-2">
    <Label for="rule-condition-type">Condition Type</Label>
    {#if isComposite}
      <div>
        <Badge variant="secondary">
          Composite -- editing not available
        </Badge>
      </div>
    {:else}
      <Select.Root type="single" bind:value={conditionType}>
        <Select.Trigger id="rule-condition-type">
          {conditionTypeLabels[conditionType] ?? conditionType}
        </Select.Trigger>
        <Select.Content>
          <Select.Item value={AlertConditionType.Threshold} label="Threshold" />
          <Select.Item
            value={AlertConditionType.RateOfChange}
            label="Rate of Change"
          />
          <Select.Item value={AlertConditionType.SignalLoss} label="Signal Loss" />
        </Select.Content>
      </Select.Root>
    {/if}
  </div>

  {#if !isComposite}
    <div class="space-y-3 rounded-md border p-3 bg-muted/30">
      {#if conditionType === AlertConditionType.Threshold}
        <div class="space-y-2">
          <Label for="threshold-direction">Direction</Label>
          <Select.Root
            type="single"
            bind:value={thresholdDirection}
          >
            <Select.Trigger id="threshold-direction">
              {thresholdDirLabels[thresholdDirection] ??
                thresholdDirection}
            </Select.Trigger>
            <Select.Content>
              <Select.Item value="below" label="Below" />
              <Select.Item value="above" label="Above" />
            </Select.Content>
          </Select.Root>
        </div>
        <div class="space-y-2">
          <Label for="threshold-value">Value ({unitLabel})</Label>
          <Input
            id="threshold-value"
            type="number"
            value={displayThreshold}
            oninput={onThresholdInput}
            step={glucoseUnits.current === "mmol" ? "0.1" : "1"}
          />
        </div>
      {:else if conditionType === AlertConditionType.RateOfChange}
        <div class="space-y-2">
          <Label for="roc-direction">Direction</Label>
          <Select.Root type="single" bind:value={rocDirection}>
            <Select.Trigger id="roc-direction">
              {rocDirLabels[rocDirection] ?? rocDirection}
            </Select.Trigger>
            <Select.Content>
              <Select.Item value="falling" label="Falling" />
              <Select.Item value="rising" label="Rising" />
            </Select.Content>
          </Select.Root>
        </div>
        <div class="space-y-2">
          <Label for="roc-rate">Rate ({unitLabel}/min)</Label>
          <Input
            id="roc-rate"
            type="number"
            step="0.1"
            value={displayRocRate}
            oninput={onRocRateInput}
          />
        </div>
      {:else if conditionType === AlertConditionType.SignalLoss}
        <div class="space-y-2">
          <Label for="signal-loss-timeout">Timeout (minutes)</Label>
          <Input
            id="signal-loss-timeout"
            type="number"
            bind:value={signalLossTimeout}
          />
        </div>
      {/if}
    </div>
  {/if}

  <Separator />

  <div class="grid grid-cols-2 gap-4">
    <div class="space-y-2">
      <Label for="hysteresis">Hysteresis (minutes)</Label>
      <Input
        id="hysteresis"
        type="number"
        bind:value={hysteresisMinutes}
      />
    </div>
    <div class="space-y-2">
      <Label for="confirmation">Confirmation Readings</Label>
      <Input
        id="confirmation"
        type="number"
        bind:value={confirmationReadings}
      />
    </div>
  </div>

  <div class="grid grid-cols-2 gap-4">
    <div class="space-y-2">
      <Label for="sort-order">Sort Order</Label>
      <Input
        id="sort-order"
        type="number"
        bind:value={sortOrder}
      />
    </div>
    <div class="flex items-end gap-3 pb-1">
      <div class="space-y-2">
        <Label>Enabled</Label>
        <Switch bind:checked={isEnabled} />
      </div>
    </div>
  </div>
</div>
