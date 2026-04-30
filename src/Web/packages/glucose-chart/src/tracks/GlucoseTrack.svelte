<script lang="ts">
  import { Spline, Points, Rule, Axis, ChartClipPath, Highlight } from "layerchart";
  import { curveMonotoneX } from "d3";
  import type { ScaleLinear } from "d3-scale";
  import { bg } from "../utils/formatting.js";

  interface GlucoseDataPoint {
    time: Date;
    sgv: number;
    color: string;
  }

  interface Props {
    glucoseData: GlucoseDataPoint[];
    glucoseScale: ScaleLinear<number, number>;
    glucoseAxisScale: ScaleLinear<number, number>;
    glucoseTrackTop: number;
    highThreshold: number;
    lowThreshold: number;
    contextWidth: number;
    onPointClick?: (data: GlucoseDataPoint) => void;
    heartRateSeries?: Array<{ time: Date; bpm: number }>;
    stepSeries?: Array<{ time: Date; steps: number }>;
  }

  let {
    glucoseData,
    glucoseScale,
    glucoseAxisScale,
    glucoseTrackTop,
    highThreshold,
    lowThreshold,
    contextWidth,
    onPointClick,
    heartRateSeries = [],
    stepSeries = [],
  }: Props = $props();

  // Only show points when density is reasonable (less than 0.5 points per pixel)
  const pointDensity = $derived(glucoseData.length / contextWidth);
  const showGlucosePoints = $derived(pointDensity < 0.5);

  // Normalize heartrate BPM to glucose Y scale:
  // 50 BPM -> low threshold, 180 BPM -> high threshold
  const heartRateToGlucose = (bpm: number) => {
    return lowThreshold + ((bpm - 50) / (180 - 50)) * (highThreshold - lowThreshold);
  };

  // Pre-compute step bubble positions: Y = 2-hour trailing glucose average
  const TWO_HOURS_MS = 2 * 60 * 60 * 1000;
  const MAX_STEPS = 500;
  const MIN_RADIUS = 2;
  const MAX_RADIUS = 8;

  const stepBubbles = $derived(
    stepSeries.map((step) => {
      const cutoff = step.time.getTime() - TWO_HOURS_MS;
      const recentGlucose = glucoseData.filter(
        (g) => g.time.getTime() >= cutoff && g.time.getTime() <= step.time.getTime()
      );
      const avgSgv =
        recentGlucose.length > 0
          ? recentGlucose.reduce((sum, g) => sum + g.sgv, 0) / recentGlucose.length
          : (lowThreshold + highThreshold) / 2;
      const radius = MIN_RADIUS + (Math.min(step.steps, MAX_STEPS) / MAX_STEPS) * (MAX_RADIUS - MIN_RADIUS);
      return { time: step.time, sgv: avgSgv, radius };
    })
  );
</script>

<!-- High threshold line -->
<Rule y={glucoseScale(highThreshold)} class="stroke-glucose-high/50" stroke-dasharray="4,4" />

<!-- Low threshold line -->
<Rule y={glucoseScale(lowThreshold)} class="stroke-glucose-very-low/50" stroke-dasharray="4,4" />

<!-- Glucose axis on left -->
<Axis
  placement="left"
  scale={glucoseAxisScale}
  ticks={5}
  format={(v) => String(bg(v))}
  tickLabelProps={{ class: "text-xs fill-muted-foreground" }}
/>

<!-- Heartrate line (behind glucose) -->
{#if heartRateSeries.length > 0}
<ChartClipPath>
  <Spline
    data={heartRateSeries}
    x={(d) => d.time}
    y={(d) => glucoseScale(heartRateToGlucose(d.bpm))}
    class="fill-none"
    style="stroke: var(--heart-rate); opacity: 0.3; stroke-width: 1.5px;"
    curve={curveMonotoneX}
  />
</ChartClipPath>
{/if}

<!-- Step bubbles (behind glucose) -->
{#if stepBubbles.length > 0}
<ChartClipPath>
  {#each stepBubbles as bubble}
    <Points
      data={[bubble]}
      x={(d) => d.time}
      y={(d) => glucoseScale(d.sgv)}
      r={bubble.radius}
      class="stroke-none"
      style="fill: var(--steps); opacity: 0.25;"
    />
  {/each}
</ChartClipPath>
{/if}

<ChartClipPath>
  <!-- Glucose line -->
  <Spline
    data={glucoseData}
    x={(d) => d.time}
    y={(d) => glucoseScale(d.sgv)}
    class="stroke-glucose-in-range stroke-2 fill-none"
    motion="spring"
    curve={curveMonotoneX}
  />

  <!-- Glucose points -->
  {#if showGlucosePoints}
    {#each glucoseData as point}
      <Points
        data={[point]}
        x={(d) => d.time}
        y={(d) => glucoseScale(d.sgv)}
        r={3}
        fill={point.color}
        class="opacity-90"
      />
    {/each}
  {/if}
</ChartClipPath>

<!-- Glucose highlight (main) -->
<ChartClipPath>
  <Highlight
    x={(d) => d.time}
    y={(d) => glucoseScale(d.sgv)}
    points
    lines
    onPointClick={onPointClick
      ? (_e, { data }) => onPointClick(data)
      : undefined}
  />
</ChartClipPath>
