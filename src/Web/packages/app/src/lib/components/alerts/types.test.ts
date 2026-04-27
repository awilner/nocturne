import { describe, it, expect } from "vitest";
import {
	defaultSchedule,
	defaultClientConfig,
	parseRule,
	type EditableSchedule,
	type ClientConfiguration,
} from "./types";

describe("defaultSchedule", () => {
	it("returns a valid default schedule", () => {
		const schedule = defaultSchedule();

		expect(schedule.name).toBe("Default Schedule");
		expect(schedule.isDefault).toBe(true);
		expect(schedule.daysOfWeek).toEqual([]);
		expect(schedule.startTime).toBe("00:00");
		expect(schedule.endTime).toBe("23:59");
		expect(schedule.timezone).toBe("UTC");
		expect(schedule.expanded).toBe(true);
		expect(schedule.escalationSteps).toHaveLength(1);
		expect(schedule.escalationSteps[0].stepOrder).toBe(0);
		expect(schedule.escalationSteps[0].delaySeconds).toBe(0);
		expect(schedule.escalationSteps[0].channels).toHaveLength(1);
	});
});

describe("defaultClientConfig", () => {
	it("returns valid audio defaults", () => {
		const config = defaultClientConfig();

		expect(config.audio.enabled).toBe(true);
		expect(config.audio.sound).toBe("alarm-default");
		expect(config.audio.customSoundId).toBeNull();
		expect(config.audio.ascending).toBe(false);
		expect(config.audio.startVolume).toBe(50);
		expect(config.audio.maxVolume).toBe(80);
		expect(config.audio.repeatCount).toBe(2);
	});

	it("returns valid visual defaults", () => {
		const config = defaultClientConfig();

		expect(config.visual.flashEnabled).toBe(false);
		expect(config.visual.flashColor).toBe("#ff0000");
		expect(config.visual.persistentBanner).toBe(true);
		expect(config.visual.wakeScreen).toBe(false);
	});

	it("returns valid snooze defaults", () => {
		const config = defaultClientConfig();

		expect(config.snooze.defaultMinutes).toBe(15);
		expect(config.snooze.options).toEqual([5, 15, 30, 60]);
		expect(config.snooze.maxCount).toBe(5);
		expect(config.snooze.smartSnooze).toBe(false);
		expect(config.snooze.smartSnoozeExtendMinutes).toBe(10);
	});
});

describe("parseRule", () => {
	it("returns default state when passed null", () => {
		const state = parseRule(null);

		expect(state.name).toBe("");
		expect(state.description).toBe("");
		expect(state.isEnabled).toBe(true);
		expect(state.hysteresisMinutes).toBe(5);
		expect(state.confirmationReadings).toBe(1);
		expect(state.schedules).toHaveLength(1);
		expect(state.schedules[0].name).toBe("Default Schedule");
	});

	it("parses a threshold rule correctly", () => {
		const state = parseRule({
			name: "Low Alert",
			description: "Alert when glucose is low",
			severity: "Normal" as any,
			conditionType: "Threshold" as any,
			conditionParams: {
				direction: "below",
				threshold: 70,
			},
			isEnabled: true,
			hysteresisMinutes: 10,
			confirmationReadings: 2,
			sortOrder: 1,
			schedules: [],
		} as any);

		expect(state.name).toBe("Low Alert");
		expect(state.description).toBe("Alert when glucose is low");
		expect(state.thresholdDirection).toBe("below");
		expect(state.thresholdValue).toBe(70);
		expect(state.hysteresisMinutes).toBe(10);
		expect(state.confirmationReadings).toBe(2);
	});

	it("parses a rate of change rule correctly", () => {
		const state = parseRule({
			name: "Rapid Drop",
			conditionType: "rate_of_change" as any,
			conditionParams: {
				direction: "falling",
				rateThreshold: 5.0,
			},
			schedules: [],
		} as any);

		expect(state.rocDirection).toBe("falling");
		expect(state.rocRate).toBe(5.0);
	});

	it("parses a signal loss rule correctly", () => {
		const state = parseRule({
			name: "Signal Lost",
			conditionType: "signal_loss" as any,
			conditionParams: {
				minutes: 30,
			},
			schedules: [],
		} as any);

		expect(state.signalLossTimeout).toBe(30);
	});

	it("parses threshold with above direction", () => {
		const state = parseRule({
			name: "High Alert",
			conditionType: "threshold" as any,
			conditionParams: {
				direction: "above",
				threshold: 250,
			},
			schedules: [],
		} as any);

		expect(state.thresholdDirection).toBe("above");
		expect(state.thresholdValue).toBe(250);
	});

	it("parses schedules with escalation steps", () => {
		const state = parseRule({
			name: "Test",
			conditionType: "Threshold" as any,
			conditionParams: {},
			schedules: [
				{
					name: "Work Hours",
					isDefault: false,
					daysOfWeek: [1, 2, 3, 4, 5],
					startTime: "09:00",
					endTime: "17:00",
					timezone: "America/New_York",
					escalationSteps: [
						{
							stepOrder: 0,
							delaySeconds: 0,
							channels: [
								{
									channelType: "WebPush",
									destination: "",
									destinationLabel: "",
								},
							],
						},
						{
							stepOrder: 1,
							delaySeconds: 300,
							channels: [],
						},
					],
				},
			],
		} as any);

		expect(state.schedules).toHaveLength(1);
		expect(state.schedules[0].name).toBe("Work Hours");
		expect(state.schedules[0].daysOfWeek).toEqual([1, 2, 3, 4, 5]);
		expect(state.schedules[0].startTime).toBe("09:00");
		expect(state.schedules[0].escalationSteps).toHaveLength(2);
		expect(state.schedules[0].escalationSteps[1].delaySeconds).toBe(300);
	});

	it("uses defaults for missing client configuration", () => {
		const state = parseRule({
			name: "Test",
			conditionType: "Threshold" as any,
			conditionParams: {},
			clientConfiguration: undefined,
			schedules: [],
		} as any);

		expect(state.clientConfig.audio.enabled).toBe(true);
		expect(state.clientConfig.audio.sound).toBe("alarm-default");
		expect(state.clientConfig.visual.flashEnabled).toBe(false);
		expect(state.clientConfig.snooze.defaultMinutes).toBe(15);
	});

	it("parses partial client configuration with defaults for missing fields", () => {
		const state = parseRule({
			name: "Test",
			conditionType: "Threshold" as any,
			conditionParams: {},
			clientConfiguration: {
				audio: { enabled: false, sound: "custom-sound" },
				visual: { flashEnabled: true },
				snooze: { defaultMinutes: 30 },
			},
			schedules: [],
		} as any);

		expect(state.clientConfig.audio.enabled).toBe(false);
		expect(state.clientConfig.audio.sound).toBe("custom-sound");
		// Defaults for missing fields
		expect(state.clientConfig.audio.ascending).toBe(false);
		expect(state.clientConfig.visual.flashEnabled).toBe(true);
		expect(state.clientConfig.visual.persistentBanner).toBe(true);
		expect(state.clientConfig.snooze.defaultMinutes).toBe(30);
		expect(state.clientConfig.snooze.maxCount).toBe(5);
	});

	it("sorts escalation steps by stepOrder", () => {
		const state = parseRule({
			name: "Test",
			conditionType: "Threshold" as any,
			conditionParams: {},
			schedules: [
				{
					name: "Default",
					escalationSteps: [
						{ stepOrder: 2, delaySeconds: 600, channels: [] },
						{ stepOrder: 0, delaySeconds: 0, channels: [] },
						{ stepOrder: 1, delaySeconds: 300, channels: [] },
					],
				},
			],
		} as any);

		const steps = state.schedules[0].escalationSteps;
		expect(steps[0].stepOrder).toBe(0);
		expect(steps[1].stepOrder).toBe(1);
		expect(steps[2].stepOrder).toBe(2);
	});
});
