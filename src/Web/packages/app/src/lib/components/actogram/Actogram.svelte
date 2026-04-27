<script lang="ts">
  import type { Snippet } from 'svelte';
  import type {
    ActogramPoint,
    ActogramRowContext,
    GlucosePoint,
    GlucoseThresholds,
  } from './actogram';
  import { sliceIntoRows, sliceBgIntoRows, HOURS_PER_ROW } from './actogram';
  import ActogramRow from './ActogramRow.svelte';

  interface Props {
    data: ActogramPoint[];
    bgData?: GlucosePoint[];
    days: Date[];
    thresholds?: GlucoseThresholds;
    rowHeight?: number;
    row: Snippet<[ActogramRowContext]>;
  }

  let {
    data,
    bgData,
    days,
    thresholds,
    rowHeight = 48,
    row,
  }: Props = $props();

  const dataRows = $derived(sliceIntoRows(data, days));
  const bgRows = $derived(bgData ? sliceBgIntoRows(bgData, days) : []);

  // X-axis hour labels at 6-hour intervals across 48h double-plot.
  // Labels show hours mod 24, so both 0h and 24h display as "0h" (midnight).
  const hourLabels = [0, 6, 12, 18, 24, 30, 36, 42, 48];

  function formatDate(date: Date): string {
    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }
</script>

<div class="flex flex-col w-full">
  <!-- X-axis labels (top) -->
  <div class="flex">
    <div class="w-16 shrink-0"></div>
    <div class="flex-1 relative h-6">
      {#each hourLabels as hour}
        {@const pct = (hour / HOURS_PER_ROW) * 100}
        <span
          class="absolute text-xs text-muted-foreground -translate-x-1/2"
          style:left="{pct}%"
        >
          {hour % 24 === 0 && hour < 48 ? '0' : hour % 24}h
        </span>
      {/each}
    </div>
  </div>

  <!-- Rows -->
  {#each dataRows as dataRow, i}
    <div class="flex items-center" style:height="{rowHeight}px">
      <!-- Date label -->
      <div class="w-16 shrink-0 text-xs text-muted-foreground text-right pr-2">
        {formatDate(dataRow.day)}
      </div>
      <!-- Chart row -->
      <div class="flex-1 h-full border-b border-border/30">
        <ActogramRow
          day={dataRow.day}
          data={dataRow.data}
          bgData={bgRows[i]?.bgData ?? []}
          {thresholds}
          height={rowHeight}
          {row}
        />
      </div>
    </div>
  {/each}
</div>
