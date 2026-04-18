<script lang="ts">
  import { Switch } from "$lib/components/ui/switch";
  import { Label } from "$lib/components/ui/label";
  import type { VisualConfig } from "./types";

  interface Props {
    visual: VisualConfig;
  }

  let { visual = $bindable() }: Props = $props();
</script>

<div class="space-y-4">
  <h3 class="text-sm font-medium">Visual</h3>

  <div class="flex items-center justify-between">
    <Label>Screen Flash</Label>
    <Switch bind:checked={visual.flashEnabled} />
  </div>

  {#if visual.flashEnabled}
    <div class="space-y-2">
      <Label for="flash-color">Flash Color</Label>
      <input
        id="flash-color"
        type="color"
        bind:value={visual.flashColor}
        class="h-9 w-16 rounded-md border cursor-pointer"
      />
    </div>
  {/if}

  <div class="flex items-center justify-between">
    <Label>Persistent Banner</Label>
    <Switch bind:checked={visual.persistentBanner} />
  </div>

  <div class="flex items-center justify-between">
    <Label>Wake Screen</Label>
    <Switch bind:checked={visual.wakeScreen} />
  </div>
</div>
