import { ChannelType } from "$api-clients";

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
