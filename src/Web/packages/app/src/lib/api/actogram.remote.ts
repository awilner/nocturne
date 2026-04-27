/**
 * Remote function for actogram report data.
 * Fetches step counts, heart rates, glucose data, and sleep state spans in parallel.
 */
import { getRequestEvent, query } from '$app/server';
import { z } from 'zod';
import { error } from '@sveltejs/kit';
import { getGlucoseColor } from '$lib/utils/chart-colors';
import { StateSpanCategory } from '$api/generated/nocturne-api-client';

const actogramSchema = z.object({
	from: z.number(),
	to: z.number(),
});

export const getActogramData = query(actogramSchema, async ({ from, to }) => {
	const { locals } = getRequestEvent();
	const { apiClient } = locals;

	const fromDate = new Date(from);
	const toDate = new Date(to);

	try {
		const [stepCountsRaw, heartRatesRaw, chartData, sleepResponse] = await Promise.all([
			apiClient.stepCount.getStepCounts(50000, 0),
			apiClient.heartRate.getHeartRates(50000, 0),
			apiClient.chartData.getDashboardChartData(from, to, 5),
			apiClient.stateSpans.getStateSpans(
				StateSpanCategory.Sleep,
				undefined,
				fromDate,
				toDate,
				undefined,
				undefined,
				10000,
			),
		]);

		// Filter step counts and heart rates by date range (client-side until NSwag regen)
		const stepCounts = (stepCountsRaw ?? [])
			.filter((s) => {
				const mills = s.mills ?? 0;
				return mills >= from && mills <= to;
			})
			.map((s) => ({
				mills: s.mills ?? 0,
				metric: s.metric ?? 0,
			}));

		const heartRates = (heartRatesRaw ?? [])
			.filter((h) => {
				const mills = h.mills ?? 0;
				return mills >= from && mills <= to;
			})
			.map((h) => ({
				mills: h.mills ?? 0,
				bpm: h.bpm ?? 0,
			}));

		// Extract thresholds from chart data
		const thresholds = {
			low: chartData.thresholds?.low ?? 55,
			high: chartData.thresholds?.high ?? 180,
			veryLow: chartData.thresholds?.veryLow ?? 54,
			veryHigh: chartData.thresholds?.veryHigh ?? 250,
			glucoseYMax: chartData.thresholds?.glucoseYMax ?? 300,
		};

		// Map glucose data with colors
		const glucoseData = (chartData.glucoseData ?? []).map((p) => ({
			mills: new Date(p.time ?? 0).getTime(),
			sgv: p.sgv ?? 0,
			color: getGlucoseColor(p.sgv ?? 0, thresholds),
		}));

		// Map sleep spans
		const sleepSpans = (sleepResponse.data ?? []).map((s) => ({
			startMills: s.startMills ?? 0,
			endMills: s.endMills ?? s.startMills ?? 0,
			state: s.state ?? 'Unknown',
		}));

		return {
			stepCounts,
			heartRates,
			glucoseData,
			sleepSpans,
			thresholds,
		};
	} catch (err) {
		console.error('Error loading actogram data:', err);
		throw error(500, 'Failed to load actogram data');
	}
});
