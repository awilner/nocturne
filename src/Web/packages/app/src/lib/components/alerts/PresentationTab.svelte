<script lang="ts">
  import type { AlertCustomSoundResponse } from "$api-clients";
  import { Separator } from "$lib/components/ui/separator";
  import AudioSection from "./AudioSection.svelte";
  import VisualSection from "./VisualSection.svelte";

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

  interface ClientConfiguration {
    audio: AudioConfig;
    visual: VisualConfig;
  }

  interface Props {
    clientConfig: ClientConfiguration;
    customSounds: AlertCustomSoundResponse[];
    onSoundsChanged: (sounds: AlertCustomSoundResponse[]) => void;
  }

  let { clientConfig = $bindable(), customSounds, onSoundsChanged }: Props = $props();
</script>

<div class="space-y-6">
  <AudioSection bind:audio={clientConfig.audio} {customSounds} {onSoundsChanged} />
  <Separator />
  <VisualSection bind:visual={clientConfig.visual} />
</div>
