// @nocturne/coach — Progressive disclosure system
export type {
  CoachMarkAdapter,
  CoachMarkOptions,
  CoachMarkProviderOptions,
  CoachMarkStep,
  MarkRegistration,
  MarkState,
  MarkStatus,
  SequenceConfig,
  SequenceDefinition,
} from "./types.js";

export { selectActiveMark, isSequenceDone, sequenceProgress } from "./sequencing.js";
export type { SelectionResult } from "./sequencing.js";
