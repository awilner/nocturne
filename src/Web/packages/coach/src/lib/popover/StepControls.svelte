<script lang="ts">
  let {
    currentStep,
    totalSteps,
    action,
    onback,
    onnext,
    ondismiss,
  }: {
    currentStep: number;
    totalSteps: number;
    action?: { label: string; href: string } | undefined;
    onback: () => void;
    onnext: () => void;
    ondismiss: () => void;
  } = $props();

  const isFirst = $derived(currentStep === 0);
  const isLast = $derived(currentStep === totalSteps - 1);
</script>

<div class="coach-step-controls">
  {#if totalSteps > 1}
    <span class="coach-step-indicator">{currentStep + 1} of {totalSteps}</span>
  {/if}

  <div class="coach-step-buttons">
    {#if !isFirst}
      <button type="button" class="coach-btn coach-btn--ghost" onclick={onback}>Back</button>
    {/if}

    {#if isLast && action}
      <a href={action.href} class="coach-btn coach-btn--primary" onclick={ondismiss}
        >{action.label}</a
      >
    {:else if !isLast}
      <button type="button" class="coach-btn coach-btn--primary" onclick={onnext}>Next</button>
    {:else}
      <button type="button" class="coach-btn coach-btn--primary" onclick={ondismiss}>Got it</button>
    {/if}
  </div>
</div>
