<script lang="ts">
  import {
    ChevronRight,
    ChevronDown,
    X,
    CheckCircle2,
    ListChecks,
  } from "lucide-svelte";
  import { getCoachMarkContext } from "@nocturne/coach";

  const ctx = getCoachMarkContext();

  const stepInfo = [
    { key: "onboarding.patient-details", title: "Patient details", description: "Clinical info so Nocturne can tailor readings", href: "/settings/patient" },
    { key: "onboarding.devices", title: "Devices", description: "Your CGM, pump, and meter", href: "/settings/patient" },
    { key: "onboarding.insulins", title: "Insulins", description: "Current insulin types and regimen", href: "/settings/patient" },
    { key: "onboarding.alerts", title: "Alerts", description: "Get notified about highs and lows", href: "/settings/alerts" },
    { key: "onboarding.sharing", title: "Sharing & Privacy", description: "Control who can see your data", href: "/settings/members" },
    { key: "onboarding.therapy-profile", title: "Therapy profile", description: "Imported from your data source", href: "/settings/profile" },
  ];

  let expanded = $state(false);
  let dismissed = $state(false);

  const progress = $derived(ctx.getSequenceProgress("onboarding"));
  const allComplete = $derived(progress.total > 0 && progress.completed >= progress.total);
  const remaining = $derived(
    stepInfo.filter((s) => {
      const status = ctx.getStatus(s.key);
      return status !== "completed" && status !== "dismissed";
    }),
  );
  const visible = $derived(!dismissed && progress.total > 0);
</script>

{#if visible}
  {#if allComplete}
    <div
      class="flex items-center gap-2 rounded-lg border bg-card px-3 py-2 text-sm text-muted-foreground"
    >
      <CheckCircle2 class="h-4 w-4 shrink-0 text-green-500" />
      <span class="flex-1">You're all set</span>
      <button
        type="button"
        class="inline-flex h-6 w-6 items-center justify-center rounded-md hover:bg-muted transition-colors"
        onclick={() => (dismissed = true)}
      >
        <X class="h-3.5 w-3.5" />
        <span class="sr-only">Dismiss</span>
      </button>
    </div>
  {:else}
    <div class="rounded-lg border bg-card">
      <!-- Compact bar -->
      <button
        type="button"
        class="flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-muted/50 transition-colors rounded-lg"
        onclick={() => (expanded = !expanded)}
      >
        <ListChecks class="h-4 w-4 shrink-0 text-muted-foreground" />
        <span class="flex-1 text-left">
          {progress.completed} of {progress.total} set up
        </span>
        <button
          type="button"
          class="inline-flex h-6 w-6 items-center justify-center rounded-md hover:bg-muted transition-colors"
          onclick={(e: MouseEvent) => {
            e.stopPropagation();
            dismissed = true;
            ctx.dismiss("onboarding._progress");
          }}
        >
          <X class="h-3.5 w-3.5" />
          <span class="sr-only">Dismiss</span>
        </button>
        {#if expanded}
          <ChevronDown class="h-4 w-4 shrink-0 text-muted-foreground" />
        {:else}
          <ChevronRight class="h-4 w-4 shrink-0 text-muted-foreground" />
        {/if}
      </button>

      <!-- Expanded list of remaining steps -->
      {#if expanded}
        <ul class="border-t px-3 py-2 space-y-1">
          {#each remaining as step}
            <li>
              <a
                href={step.href}
                class="flex items-center gap-3 rounded-md px-2 py-1.5 text-sm hover:bg-muted transition-colors"
              >
                <div class="flex-1 min-w-0">
                  <div class="font-medium">{step.title}</div>
                  <div class="text-xs text-muted-foreground">{step.description}</div>
                </div>
                <ChevronRight class="h-4 w-4 shrink-0 text-muted-foreground" />
              </a>
            </li>
          {/each}
        </ul>
      {/if}
    </div>
  {/if}
{/if}
