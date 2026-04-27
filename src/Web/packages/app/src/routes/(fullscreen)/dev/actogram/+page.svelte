<script lang="ts">
  import { Actogram, type ActogramPoint, type ActogramRowContext, type GlucosePoint } from '$lib/components/actogram';
  import { getGlucoseColor } from '$lib/utils/chart-colors';
  import { MS_PER_HOUR } from '$lib/components/actogram/actogram';

  const DAYS = 14;
  const thresholds = { low: 70, high: 180, veryLow: 54, veryHigh: 250, glucoseYMax: 300 };

  // Build an array of day-start dates (midnight UTC) going back DAYS days from today
  const now = new Date();
  const todayMidnight = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const days: Date[] = Array.from({ length: DAYS }, (_, i) => {
    const d = new Date(todayMidnight);
    d.setDate(d.getDate() - (DAYS - 1 - i));
    return d;
  });

  // Generate mock step count data: one point per hour for each day
  function generateStepData(): ActogramPoint[] {
    const points: ActogramPoint[] = [];
    // Use a seeded-ish approach for deterministic-looking randomness
    let seed = 42;
    function pseudoRandom(): number {
      seed = (seed * 16807 + 0) % 2147483647;
      return seed / 2147483647;
    }

    for (const day of days) {
      // Cover 48h window (current day + next day for double-plot)
      for (let hour = 0; hour < 48; hour++) {
        const mills = day.getTime() + hour * MS_PER_HOUR;
        // Simulate circadian pattern: lower at night (0-6, 22-30), higher during day
        const hourOfDay = hour % 24;
        const isNight = hourOfDay < 6 || hourOfDay >= 22;
        const base = isNight ? 500 : 3000;
        const range = isNight ? 1000 : 7000;
        const steps = Math.floor(base + pseudoRandom() * range);
        points.push({ mills, steps });
      }
    }
    return points;
  }

  // Generate mock glucose data: sinusoidal pattern every 5 minutes
  function generateGlucoseData(): GlucosePoint[] {
    const points: GlucosePoint[] = [];
    const fiveMin = 5 * 60_000;

    for (const day of days) {
      for (let offset = 0; offset < 48 * MS_PER_HOUR; offset += fiveMin) {
        const mills = day.getTime() + offset;
        const hours = offset / MS_PER_HOUR;
        // Sinusoidal between 70-250: center at 160, amplitude 90
        const sgv = Math.round(160 + 90 * Math.sin((hours / 4) * Math.PI));
        const color = getGlucoseColor(sgv, thresholds);
        points.push({ mills, sgv, color });
      }
    }
    return points;
  }

  const stepData = generateStepData();
  const bgData = generateGlucoseData();
  const MAX_STEPS = 10000;
</script>

<svelte:head>
  <title>Actogram Dev</title>
</svelte:head>

<div class="p-8 max-w-6xl mx-auto">
  <h1 class="text-2xl font-bold mb-4">Actogram Visual Test</h1>
  <p class="text-muted-foreground mb-6">
    {DAYS} days of mock step count (bars) and glucose data (line overlay).
    The right half of each row is the next day's data (double-plot), dimmed.
  </p>

  <Actogram
    data={stepData}
    {bgData}
    {days}
    {thresholds}
    rowHeight={48}
  >
    {#snippet row(ctx: ActogramRowContext)}
      {#each ctx.data as { point, hoursFromStart, isExtended }}
        {@const steps = (point as ActogramPoint & { steps: number }).steps ?? 0}
        {@const barHeight = (steps / MAX_STEPS) * ctx.height}
        {@const x = ctx.xScale(new Date(ctx.day.getTime() + hoursFromStart * MS_PER_HOUR))}
        <rect
          {x}
          y={ctx.height - barHeight}
          width={3}
          height={barHeight}
          fill={isExtended ? 'var(--primary/40)' : 'var(--primary)'}
          opacity={isExtended ? 0.4 : 0.8}
        />
      {/each}
    {/snippet}
  </Actogram>
</div>
