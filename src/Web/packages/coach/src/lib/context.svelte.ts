import { getContext, setContext } from "svelte";
import type {
  CoachMarkAdapter,
  MarkRegistration,
  MarkState,
  MarkStatus,
  SequenceConfig,
} from "./types.js";
import { selectActiveMark, isSequenceDone, sequenceProgress, type SelectionResult } from "./sequencing.js";

const COACH_CONTEXT_KEY = Symbol("coach-mark-context");

export class CoachMarkContext {
  private adapter: CoachMarkAdapter;
  private sequences: SequenceConfig;
  private settleDelay: number;
  private seenDwellMs: number;

  private _states = $state<Map<string, MarkState>>(new Map());
  private _registrations = $state<MarkRegistration[]>([]);
  private _activeSelection = $state<SelectionResult | null>(null);
  private _settleTimer: ReturnType<typeof setTimeout> | null = null;
  private _initialized = $state(false);

  activeKey = $derived(this._activeSelection?.key ?? null);
  activeStep = $derived(this._activeSelection?.step ?? null);

  constructor(
    adapter: CoachMarkAdapter,
    sequences: SequenceConfig = {},
    settleDelay = 500,
    seenDwellMs = 2000,
  ) {
    this.adapter = adapter;
    this.sequences = sequences;
    this.settleDelay = settleDelay;
    this.seenDwellMs = seenDwellMs;
  }

  async initialize(): Promise<void> {
    const states = await this.adapter.fetchAll();
    const map = new Map<string, MarkState>();
    for (const s of states) {
      map.set(s.markKey, s);
    }
    this._states = map;
    this._initialized = true;
    this.scheduleSelection();
  }

  register(registration: MarkRegistration): () => void {
    this._registrations = [...this._registrations, registration];
    this.scheduleSelection();
    return () => {
      this._registrations = this._registrations.filter(
        (r) =>
          !(
            r.key === registration.key &&
            r.step === registration.step &&
            r.element === registration.element
          ),
      );
      this.scheduleSelection();
    };
  }

  updateRegistration(key: string, step: number, updates: Partial<MarkRegistration>): void {
    this._registrations = this._registrations.map((r) =>
      r.key === key && r.step === step ? { ...r, ...updates } : r,
    );
  }

  activate(key: string, step: number): void {
    if (this._activeSelection && this._activeSelection.key !== key) {
      this.markSeen(this._activeSelection.key);
    }
    this._activeSelection = { key, step };
  }

  dismiss(key: string): void {
    this.updateStatus(key, "dismissed");
    this._activeSelection = null;
    this.scheduleSelection();
  }

  complete(key: string): void {
    this.updateStatus(key, "completed");
    if (this._activeSelection?.key === key) {
      this._activeSelection = null;
    }
    this.scheduleSelection();
  }

  markSeen(key: string): void {
    const state = this._states.get(key);
    if (!state || state.status === "unseen") {
      this.updateStatus(key, "seen");
    }
  }

  getStatus(key: string): MarkStatus {
    return this._states.get(key)?.status ?? "unseen";
  }

  isMarkEligible(key: string): boolean {
    // Find which sequence this mark belongs to (if any)
    let markSequenceName: string | null = null;
    for (const [name, seq] of Object.entries(this.sequences)) {
      if (seq.steps.includes(key)) {
        markSequenceName = name;
        break;
      }
    }

    // Standalone marks are always eligible
    if (!markSequenceName) return true;

    const markSequence = this.sequences[markSequenceName];

    // Check if this sequence's prerequisite is met
    if (markSequence.prerequisite && !isSequenceDone(markSequence.prerequisite, this.sequences, this._states)) {
      return false;
    }

    return true;
  }

  getSequenceProgress(seqName: string): { completed: number; total: number } {
    return sequenceProgress(seqName, this.sequences, this._states);
  }

  getMountedSteps(key: string): MarkRegistration[] {
    return this._registrations
      .filter((r) => r.key === key)
      .sort((a, b) => a.step - b.step);
  }

  get seenDwell(): number {
    return this.seenDwellMs;
  }

  private updateStatus(key: string, status: MarkStatus): void {
    const existing = this._states.get(key);
    const now = new Date().toISOString();

    const updated: MarkState = {
      id: existing?.id ?? "",
      markKey: key,
      status,
      seenAt:
        status === "seen" && !existing?.seenAt ? now : (existing?.seenAt ?? null),
      completedAt:
        (status === "completed" || status === "dismissed") && !existing?.completedAt
          ? now
          : (existing?.completedAt ?? null),
    };

    const newMap = new Map(this._states);
    newMap.set(key, updated);
    this._states = newMap;

    // Fire and forget — optimistic
    this.adapter.update(key, status).catch(() => {});
  }

  private scheduleSelection(): void {
    if (!this._initialized) return;
    if (this._settleTimer) clearTimeout(this._settleTimer);
    this._settleTimer = setTimeout(() => {
      if (this._activeSelection) return;
      this._activeSelection = selectActiveMark(
        this._states,
        this._registrations,
        this.sequences,
      );
    }, this.settleDelay);
  }
}

export function createCoachMarkContext(
  adapter: CoachMarkAdapter,
  sequences: SequenceConfig = {},
  settleDelay = 500,
  seenDwellMs = 2000,
): CoachMarkContext {
  const ctx = new CoachMarkContext(adapter, sequences, settleDelay, seenDwellMs);
  setContext(COACH_CONTEXT_KEY, ctx);
  return ctx;
}

export function getCoachMarkContext(): CoachMarkContext {
  return getContext<CoachMarkContext>(COACH_CONTEXT_KEY);
}
