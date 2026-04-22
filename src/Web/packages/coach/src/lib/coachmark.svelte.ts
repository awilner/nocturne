import type { CoachMarkOptions, MarkRegistration } from "./types.js";
import type { CoachMarkContext } from "./context.svelte.js";

// Module-level context reference, set by the provider
let _ctx: CoachMarkContext | null = null;

export function setCoachMarkContextRef(ctx: CoachMarkContext): void {
  _ctx = ctx;
}

export function coachmark(options: CoachMarkOptions) {
  return (element: HTMLElement) => {
    const ctx = _ctx;
    if (!ctx) {
      console.warn("[coach] No CoachMarkProvider found. Mark ignored:", options.key);
      return;
    }

    const steps =
      options.steps ??
      (options.title
        ? [{ title: options.title, description: options.description ?? "" }]
        : []);

    if (steps.length === 0) {
      console.warn(`[coach] Mark "${options.key}" has no content.`);
      return;
    }

    const stepIndex = options.step ?? 0;
    const stepContent = steps[stepIndex] ?? steps[0];

    const registration: MarkRegistration = {
      key: options.key,
      step: stepIndex,
      title: stepContent.title,
      description: stepContent.description,
      action: options.action,
      completedWhen: options.completedWhen,
      completeOn: options.completeOn,
      priority: options.priority ?? 0,
      element,
    };

    const unregister = ctx.register(registration);

    // Create hotspot dot
    const dot = document.createElement("button");
    dot.className = "coach-hotspot";
    dot.setAttribute("aria-label", "Show tip");
    dot.setAttribute("type", "button");
    dot.addEventListener("click", (e) => {
      e.stopPropagation();
      ctx.activate(options.key, stepIndex);
    });

    // Position the element relatively if needed
    const computedPosition = getComputedStyle(element).position;
    if (computedPosition === "static") {
      element.style.position = "relative";
    }
    element.appendChild(dot);

    // completeOn event listener management
    let completeOnCleanup: (() => void) | null = null;

    function attachCompleteOnListener(): void {
      if (completeOnCleanup || !options.completeOn) return;

      const { event, target } = options.completeOn;
      let targetEl: HTMLElement | null = null;

      if (!target) {
        targetEl = element;
      } else if (typeof target === "string") {
        targetEl = element.querySelector(target);
      } else {
        targetEl = target;
      }

      if (!targetEl) return; // target not available yet, retry on next poll

      const handler = () => {
        ctx.complete(options.key);
      };

      targetEl.addEventListener(event, handler, { once: true });
      completeOnCleanup = () => targetEl!.removeEventListener(event, handler);
    }

    // Visibility update interval
    const updateVisibility = () => {
      const status = ctx.getStatus(options.key);
      const eligible = ctx.isMarkEligible(options.key);

      if (status === "completed" || status === "dismissed" || !eligible) {
        dot.style.display = "none";
      } else if (status === "seen") {
        dot.classList.remove("coach-hotspot--pulse");
        dot.style.display = "";
      } else {
        dot.classList.add("coach-hotspot--pulse");
        dot.style.display = "";
      }

      // Check completedWhen callback
      if (options.completedWhen && status !== "completed" && status !== "dismissed") {
        if (options.completedWhen()) {
          ctx.complete(options.key);
        }
      }

      // Lazily attach completeOn listener (handles bind:this refs that arrive after mount)
      if (options.completeOn && !completeOnCleanup && status !== "completed" && status !== "dismissed") {
        attachCompleteOnListener();
      }
    };

    updateVisibility();
    const interval = setInterval(updateVisibility, 500);

    // Cleanup
    return () => {
      clearInterval(interval);
      completeOnCleanup?.();
      unregister();
      dot.remove();
    };
  };
}
