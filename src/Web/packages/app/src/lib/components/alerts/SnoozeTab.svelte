<script lang="ts">
  import { Input } from "$lib/components/ui/input";
  import { Button } from "$lib/components/ui/button";
  import { Switch } from "$lib/components/ui/switch";
  import { Label } from "$lib/components/ui/label";
  import { Badge } from "$lib/components/ui/badge";
  import { Separator } from "$lib/components/ui/separator";
  import { X } from "lucide-svelte";
  import type { SnoozeConfig } from "./types";

  interface Props {
    snooze: SnoozeConfig;
  }

  let { snooze = $bindable() }: Props = $props();

  let newSnoozeOption = $state("");

  function addSnoozeOption() {
    const val = parseInt(newSnoozeOption, 10);
    if (!isNaN(val) && val > 0 && !snooze.options.includes(val)) {
      snooze.options = [...snooze.options, val].sort((a, b) => a - b);
      newSnoozeOption = "";
    }
  }

  function removeSnoozeOption(val: number) {
    snooze.options = snooze.options.filter((o) => o !== val);
  }
</script>

<div class="space-y-4">
  <div class="space-y-2">
    <Label for="snooze-default">Default Snooze Duration (minutes)</Label>
    <Input
      id="snooze-default"
      type="number"
      bind:value={snooze.defaultMinutes}
    />
  </div>

  <div class="space-y-2">
    <Label>Snooze Options</Label>
    <div class="flex flex-wrap gap-2">
      {#each snooze.options as opt}
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
      bind:value={snooze.maxCount}
    />
  </div>

  <Separator />

  <div class="flex items-center justify-between">
    <Label>Smart Snooze</Label>
    <Switch bind:checked={snooze.smartSnooze} />
  </div>

  {#if snooze.smartSnooze}
    <div class="space-y-2">
      <Label for="smart-snooze-extend">Smart Snooze Extend (minutes)</Label>
      <Input
        id="smart-snooze-extend"
        type="number"
        bind:value={snooze.smartSnoozeExtendMinutes}
      />
      <p class="text-xs text-muted-foreground">
        Automatically extends snooze when glucose trend is favorable
      </p>
    </div>
  {/if}
</div>
