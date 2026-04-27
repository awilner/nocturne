<script lang="ts">
  import type { Snippet } from 'svelte';
  import { Chart, Svg, Spline, Points } from 'layerchart';
  import { scaleTime } from 'd3-scale';
  import type { ScaleTime } from 'd3-scale';
  import { curveMonotoneX } from 'd3';
  import {
    MS_PER_HOUR,
    HOURS_PER_DAY,
    HOURS_PER_ROW,
    type ActogramPoint,
    type ActogramRowContext,
    type GlucosePoint,
    type GlucoseThresholds,
    type RowDataPoint,
  } from './actogram';

  interface Props {
    day: Date;
    data: RowDataPoint<ActogramPoint>[];
    bgData: RowDataPoint<GlucosePoint>[];
    thresholds: GlucoseThresholds | undefined;
    height: number;
    row: Snippet<[ActogramRowContext]>;
  }

  let { day, data, bgData, thresholds, height, row }: Props = $props();

  // X domain: 0–48 hours from day start
  const xDomainEnd = $derived(new Date(day.getTime() + HOURS_PER_ROW * MS_PER_HOUR));
  const midpoint = $derived(new Date(day.getTime() + HOURS_PER_DAY * MS_PER_HOUR));

  // Map RowDataPoints to chart-plottable objects for BG overlay
  const bgChartData = $derived(
    bgData
      .toSorted((a, b) => a.hoursFromStart - b.hoursFromStart)
      .map((d) => ({
        time: new Date(day.getTime() + d.hoursFromStart * MS_PER_HOUR),
        sgv: d.point.sgv,
        color: d.point.color,
      })),
  );
</script>

<div style:height="{height}px">
<Chart
  data={bgChartData}
  x="time"
  y="sgv"
  xScale={scaleTime()}
  xDomain={[day, xDomainEnd]}
  yDomain={[0, thresholds?.glucoseYMax ?? 300]}
  padding={{ left: 0, top: 0, bottom: 0, right: 0 }}
>
  {#snippet children({ context })}
    {@const rowContext: ActogramRowContext = {
      // LayerChart types xScale as AnyScale; we know it's ScaleTime because we pass scaleTime() above
      xScale: context.xScale as unknown as ScaleTime<number, number>,
      width: context.width,
      height: context.height,
      data,
      day,
    }}
    <Svg>
      <!-- Consumer's snippet renders first (bottom layer) -->
      {@render row(rowContext)}

      <!-- BG overlay line (middle layer) -->
      {#if bgChartData.length > 1 && thresholds}
        <Spline
          data={bgChartData}
          x={(d) => d.time}
          y={(d) => d.sgv}
          curve={curveMonotoneX}
          class="stroke-muted-foreground/50 fill-none"
          strokeWidth={1.5}
        />
        {#each bgChartData as point}
          <Points
            data={[point]}
            x={(d) => d.time}
            y={(d) => d.sgv}
            r={2}
            fill={point.color}
            class="opacity-80"
          />
        {/each}
      {/if}

      <!-- Dimming overlay for the extended (24–48h) half (top layer) -->
      <rect
        x={context.xScale(midpoint)}
        y={0}
        width={context.xScale(xDomainEnd) - context.xScale(midpoint)}
        height={context.height}
        fill="var(--background)"
        opacity={0.6}
      />
    </Svg>
  {/snippet}
</Chart>
</div>
