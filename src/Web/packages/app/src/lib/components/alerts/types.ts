import { AlertConditionType, AlertRuleSeverity, ChannelType } from "$api-clients";
import type { AlertRuleResponse } from "$api-clients";

export interface AudioConfig {
	enabled: boolean;
	sound: string;
	customSoundId: string | null;
	ascending: boolean;
	startVolume: number;
	maxVolume: number;
	ascendDurationSeconds: number;
	repeatCount: number;
}

export interface VisualConfig {
	flashEnabled: boolean;
	flashColor: string;
	persistentBanner: boolean;
	wakeScreen: boolean;
}

export interface SnoozeConfig {
	defaultMinutes: number;
	options: number[];
	maxCount: number;
	smartSnooze: boolean;
	smartSnoozeExtendMinutes: number;
}

export interface ClientConfiguration {
	audio: AudioConfig;
	visual: VisualConfig;
	snooze: SnoozeConfig;
}

export interface EditableChannel {
	channelType: ChannelType | string;
	destination: string;
	destinationLabel: string;
}

export interface EditableStep {
	stepOrder: number;
	delaySeconds: number;
	channels: EditableChannel[];
}

export interface EditableSchedule {
	name: string;
	isDefault: boolean;
	daysOfWeek: number[];
	startTime: string;
	endTime: string;
	timezone: string;
	escalationSteps: EditableStep[];
	expanded: boolean;
}

export interface RuleEditorState {
	name: string;
	description: string;
	severity: AlertRuleSeverity;
	conditionType: AlertConditionType;
	isComposite: boolean;
	thresholdDirection: string;
	thresholdValue: number;
	rocDirection: string;
	rocRate: number;
	signalLossTimeout: number;
	hysteresisMinutes: number;
	confirmationReadings: number;
	sortOrder: number;
	isEnabled: boolean;
	clientConfig: ClientConfiguration;
	schedules: EditableSchedule[];
}

export function defaultSchedule(): EditableSchedule {
	return {
		name: "Default Schedule",
		isDefault: true,
		daysOfWeek: [],
		startTime: "00:00",
		endTime: "23:59",
		timezone: "UTC",
		escalationSteps: [
			{
				stepOrder: 0,
				delaySeconds: 0,
				channels: [
					{
						channelType: ChannelType.WebPush,
						destination: "",
						destinationLabel: "",
					},
				],
			},
		],
		expanded: true,
	};
}

export function defaultClientConfig(): ClientConfiguration {
	return {
		audio: {
			enabled: true,
			sound: "alarm-default",
			customSoundId: null,
			ascending: false,
			startVolume: 50,
			maxVolume: 80,
			ascendDurationSeconds: 30,
			repeatCount: 2,
		},
		visual: {
			flashEnabled: false,
			flashColor: "#ff0000",
			persistentBanner: true,
			wakeScreen: false,
		},
		snooze: {
			defaultMinutes: 15,
			options: [5, 15, 30, 60],
			maxCount: 5,
			smartSnooze: false,
			smartSnoozeExtendMinutes: 10,
		},
	};
}

function defaultState(): RuleEditorState {
	return {
		name: "",
		description: "",
		severity: AlertRuleSeverity.Normal,
		conditionType: AlertConditionType.Threshold,
		isComposite: false,
		thresholdDirection: "below",
		thresholdValue: 70,
		rocDirection: "falling",
		rocRate: 3.0,
		signalLossTimeout: 15,
		hysteresisMinutes: 5,
		confirmationReadings: 1,
		sortOrder: 0,
		isEnabled: true,
		clientConfig: defaultClientConfig(),
		schedules: [defaultSchedule()],
	};
}

export function parseRule(r: AlertRuleResponse | null): RuleEditorState {
	if (!r) return defaultState();

	// Condition type
	const ct = r.conditionType ?? AlertConditionType.Threshold;
	let conditionType: AlertConditionType;
	let isComposite = false;

	if (ct === AlertConditionType.Composite) {
		isComposite = true;
		conditionType = AlertConditionType.Composite;
	} else if (
		ct === AlertConditionType.Threshold ||
		(ct as string) === "threshold_low" ||
		(ct as string) === "threshold_high"
	) {
		conditionType = AlertConditionType.Threshold;
	} else if (ct === AlertConditionType.RateOfChange) {
		conditionType = AlertConditionType.RateOfChange;
	} else if (ct === AlertConditionType.SignalLoss) {
		conditionType = AlertConditionType.SignalLoss;
	} else {
		conditionType = ct as AlertConditionType;
	}

	// Condition params
	const params = r.conditionParams;
	let thresholdDirection = "below";
	let thresholdValue = 70;
	let rocDirection = "falling";
	let rocRate = 3.0;
	let signalLossTimeout = 15;

	if (params) {
		if (conditionType === AlertConditionType.Threshold) {
			thresholdDirection = params.direction === "above" ? "above" : "below";
			thresholdValue = params.threshold ?? params.value ?? 70;
		} else if (conditionType === AlertConditionType.RateOfChange) {
			rocDirection = params.direction ?? "falling";
			rocRate = params.rateThreshold ?? params.rate ?? 3.0;
		} else if (conditionType === AlertConditionType.SignalLoss) {
			signalLossTimeout = params.minutes ?? params.timeout_minutes ?? 15;
		}
	}

	// Client configuration
	const cc = r.clientConfiguration;
	const clientConfig: ClientConfiguration = cc
		? {
				audio: {
					enabled: cc.audio?.enabled ?? true,
					sound: cc.audio?.sound ?? "alarm-default",
					customSoundId: cc.audio?.customSoundId ?? null,
					ascending: cc.audio?.ascending ?? false,
					startVolume: cc.audio?.startVolume ?? 50,
					maxVolume: cc.audio?.maxVolume ?? 80,
					ascendDurationSeconds: cc.audio?.ascendDurationSeconds ?? 30,
					repeatCount: cc.audio?.repeatCount ?? 2,
				},
				visual: {
					flashEnabled: cc.visual?.flashEnabled ?? false,
					flashColor: cc.visual?.flashColor ?? "#ff0000",
					persistentBanner: cc.visual?.persistentBanner ?? true,
					wakeScreen: cc.visual?.wakeScreen ?? false,
				},
				snooze: {
					defaultMinutes: cc.snooze?.defaultMinutes ?? 15,
					options: cc.snooze?.options ?? [5, 15, 30, 60],
					maxCount: cc.snooze?.maxCount ?? 5,
					smartSnooze: cc.snooze?.smartSnooze ?? false,
					smartSnoozeExtendMinutes: cc.snooze?.smartSnoozeExtendMinutes ?? 10,
				},
			}
		: defaultClientConfig();

	// Schedules
	const schedules: EditableSchedule[] =
		r.schedules && r.schedules.length > 0
			? r.schedules.map((s) => ({
					name: s.name ?? "Default Schedule",
					isDefault: s.isDefault ?? false,
					daysOfWeek: s.daysOfWeek ?? [],
					startTime: s.startTime ?? "00:00",
					endTime: s.endTime ?? "23:59",
					timezone: s.timezone ?? "UTC",
					escalationSteps: (s.escalationSteps ?? [])
						.sort((a, b) => (a.stepOrder ?? 0) - (b.stepOrder ?? 0))
						.map((step) => ({
							stepOrder: step.stepOrder ?? 0,
							delaySeconds: step.delaySeconds ?? 0,
							channels: (step.channels ?? []).map((ch) => ({
								channelType: ch.channelType ?? ChannelType.WebPush,
								destination: ch.destination ?? "",
								destinationLabel: ch.destinationLabel ?? "",
							})),
						})),
					expanded: false,
				}))
			: [defaultSchedule()];

	return {
		name: r.name ?? "",
		description: r.description ?? "",
		severity: (r.severity as AlertRuleSeverity) ?? AlertRuleSeverity.Normal,
		isEnabled: r.isEnabled ?? true,
		hysteresisMinutes: r.hysteresisMinutes ?? 5,
		confirmationReadings: r.confirmationReadings ?? 1,
		sortOrder: r.sortOrder ?? 0,
		conditionType,
		isComposite,
		thresholdDirection,
		thresholdValue,
		rocDirection,
		rocRate,
		signalLossTimeout,
		clientConfig,
		schedules,
	};
}
