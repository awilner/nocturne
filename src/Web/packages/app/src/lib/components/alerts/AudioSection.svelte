<script lang="ts">
  import type { AlertCustomSoundResponse } from "$api-clients";
  import {
    getSounds,
    deleteSound,
  } from "$api/generated/alertCustomSounds.generated.remote";
  import * as Select from "$lib/components/ui/select";
  import { Input } from "$lib/components/ui/input";
  import { Switch } from "$lib/components/ui/switch";
  import { Label } from "$lib/components/ui/label";
  import { Separator } from "$lib/components/ui/separator";
  import { Slider } from "$lib/components/ui/slider";
  import { Button } from "$lib/components/ui/button";
  import {
    Loader2,
    Play,
    Upload,
    Trash2,
  } from "lucide-svelte";
  import type { AudioConfig } from "./types";

  interface Props {
    audio: AudioConfig;
    customSounds: AlertCustomSoundResponse[];
    onSoundsChanged: (sounds: AlertCustomSoundResponse[]) => void;
  }

  let { audio = $bindable(), customSounds, onSoundsChanged }: Props = $props();

  // --- State ---
  let uploading = $state(false);
  let uploadError = $state<string | null>(null);
  let previewAudio: HTMLAudioElement | null = null;

  // --- Built-in sounds ---
  const builtInSounds = [
    { value: "alarm-default", label: "Default Alarm" },
    { value: "alarm-urgent", label: "Urgent Alarm" },
    { value: "alarm-high", label: "High Alarm" },
    { value: "alarm-low", label: "Low Alarm" },
    { value: "alert", label: "Alert" },
    { value: "chime", label: "Chime" },
    { value: "bell", label: "Bell" },
    { value: "siren", label: "Siren" },
    { value: "beep", label: "Beep" },
    { value: "soft", label: "Soft" },
  ];

  // --- Derived ---
  let startVolumeArr = $derived([audio.startVolume]);
  let maxVolumeArr = $derived([audio.maxVolume]);

  // --- Audio preview ---
  function playPreview() {
    if (previewAudio) {
      previewAudio.pause();
      previewAudio = null;
    }
    try {
      const sound = audio.sound;
      const isCustom =
        audio.customSoundId &&
        customSounds.some((s) => s.id === audio.customSoundId);
      const url = isCustom
        ? `/api/v4/alert-sounds/${audio.customSoundId}/stream`
        : `/sounds/${sound}.mp3`;
      previewAudio = new Audio(url);
      previewAudio.volume = (audio.maxVolume ?? 80) / 100;
      previewAudio.play().catch(() => {
        // Audio file may not exist yet
      });
    } catch {
      // Gracefully handle missing files
    }
  }

  // --- File upload ---
  async function handleFileUpload(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    uploadError = null;

    if (file.size > 512000) {
      uploadError = "File size must be less than 500KB";
      input.value = "";
      return;
    }

    uploading = true;
    try {
      // Upload via fetch since the generated remote function doesn't support FormData params
      const formData = new FormData();
      formData.append("file", file);
      const response = await fetch("/api/v4/alert-sounds", {
        method: "POST",
        body: formData,
      });
      if (!response.ok) {
        uploadError = "Failed to upload sound";
      } else {
        const result = await getSounds();
        onSoundsChanged(Array.isArray(result) ? result : []);
      }
    } catch {
      uploadError = "Failed to upload sound";
    } finally {
      uploading = false;
      input.value = "";
    }
  }

  // --- Custom sound deletion ---
  async function handleDeleteSound(id: string) {
    try {
      await deleteSound(id);
      const result = await getSounds();
      onSoundsChanged(Array.isArray(result) ? result : []);
      // If the deleted sound was selected, reset to default
      if (audio.customSoundId === id) {
        audio.customSoundId = null;
        audio.sound = "alarm-default";
      }
    } catch {
      // Error handled by remote function
    }
  }

  // --- Sound selection helpers ---
  function getSelectedSoundLabel(): string {
    if (audio.customSoundId) {
      const custom = customSounds.find(
        (s) => s.id === audio.customSoundId,
      );
      if (custom) return custom.name ?? "Custom Sound";
    }
    const built = builtInSounds.find(
      (s) => s.value === audio.sound,
    );
    return built?.label ?? audio.sound;
  }

  function handleSoundSelect(value: string) {
    const custom = customSounds.find((s) => s.id === value);
    if (custom) {
      audio.customSoundId = custom.id ?? null;
      audio.sound = custom.name ?? "custom";
    } else {
      audio.customSoundId = null;
      audio.sound = value;
    }
  }
</script>

<div class="space-y-4">
  <h3 class="text-sm font-medium">Audio</h3>

  <div class="flex items-center justify-between">
    <Label>Audio Enabled</Label>
    <Switch bind:checked={audio.enabled} />
  </div>

  {#if audio.enabled}
    <div class="space-y-2">
      <Label for="audio-sound">Sound</Label>
      <div class="flex gap-2">
        <div class="flex-1">
          <Select.Root
            type="single"
            value={audio.customSoundId ?? audio.sound}
            onValueChange={handleSoundSelect}
          >
            <Select.Trigger id="audio-sound">
              {getSelectedSoundLabel()}
            </Select.Trigger>
            <Select.Content>
              <Select.Group>
                <Select.Label>Built-in Sounds</Select.Label>
                {#each builtInSounds as sound}
                  <Select.Item
                    value={sound.value}
                    label={sound.label}
                  />
                {/each}
              </Select.Group>
              {#if customSounds.length > 0}
                <Select.Separator />
                <Select.Group>
                  <Select.Label>Custom Sounds</Select.Label>
                  {#each customSounds as sound}
                    <Select.Item
                      value={sound.id ?? ""}
                      label={sound.name ?? "Custom"}
                    />
                  {/each}
                </Select.Group>
              {/if}
            </Select.Content>
          </Select.Root>
        </div>
        <Button
          variant="outline"
          size="icon"
          onclick={playPreview}
          title="Preview sound"
        >
          <Play class="h-4 w-4" />
        </Button>
      </div>
    </div>

    <!-- Upload custom sound -->
    <div class="space-y-2">
      <Label>Upload Custom Sound</Label>
      <div class="flex items-center gap-2">
        <label
          class="flex items-center gap-2 cursor-pointer rounded-md border px-3 py-2 text-sm hover:bg-muted transition-colors"
        >
          {#if uploading}
            <Loader2 class="h-4 w-4 animate-spin" />
          {:else}
            <Upload class="h-4 w-4" />
          {/if}
          <span>Choose file</span>
          <input
            type="file"
            accept="audio/*"
            class="hidden"
            onchange={handleFileUpload}
            disabled={uploading}
          />
        </label>
        <span class="text-xs text-muted-foreground">Max 500KB</span>
      </div>
      {#if uploadError}
        <p class="text-xs text-destructive">{uploadError}</p>
      {/if}
    </div>

    <!-- Custom sounds list with delete -->
    {#if customSounds.length > 0}
      <div class="space-y-1">
        <Label>Custom Sounds</Label>
        {#each customSounds as sound}
          <div
            class="flex items-center justify-between p-2 rounded-md border text-sm"
          >
            <span>{sound.name ?? "Custom"}</span>
            <Button
              variant="ghost"
              size="icon"
              class="h-7 w-7 text-destructive"
              onclick={() => handleDeleteSound(sound.id ?? "")}
            >
              <Trash2 class="h-3 w-3" />
            </Button>
          </div>
        {/each}
      </div>
    {/if}

    <Separator />

    <div class="flex items-center justify-between">
      <Label>Ascending Volume</Label>
      <Switch bind:checked={audio.ascending} />
    </div>

    {#if audio.ascending}
      <div class="space-y-2">
        <Label>Start Volume: {audio.startVolume}%</Label>
        <Slider
          type="multiple"
          value={startVolumeArr}
          onValueChange={(v: number[]) => {
            audio.startVolume = v[0];
          }}
          min={0}
          max={100}
          step={1}
        />
      </div>
    {/if}

    <div class="space-y-2">
      <Label>Max Volume: {audio.maxVolume}%</Label>
      <Slider
        type="multiple"
        value={maxVolumeArr}
        onValueChange={(v: number[]) => {
          audio.maxVolume = v[0];
        }}
        min={0}
        max={100}
        step={1}
      />
    </div>

    <div class="grid grid-cols-2 gap-4">
      <div class="space-y-2">
        <Label for="ascend-duration">Ascend Duration (s)</Label>
        <Input
          id="ascend-duration"
          type="number"
          bind:value={audio.ascendDurationSeconds}
        />
      </div>
      <div class="space-y-2">
        <Label for="repeat-count">Repeat Count</Label>
        <Input
          id="repeat-count"
          type="number"
          bind:value={audio.repeatCount}
        />
      </div>
    </div>
  {/if}
</div>
