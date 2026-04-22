import type { SequenceConfig } from "@nocturne/coach";

export const sequences: SequenceConfig = {
  onboarding: {
    priority: 100,
    steps: [
      "onboarding.patient-details",
      "onboarding.devices",
      "onboarding.insulins",
      "onboarding.alerts",
      "onboarding.sharing",
      "onboarding.therapy-profile",
    ],
  },
};
