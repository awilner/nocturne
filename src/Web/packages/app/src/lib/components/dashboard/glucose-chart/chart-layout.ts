export const SWIM_LANE_HEIGHT = 0.04;

export type SwimLanePosition = {
  top: number;
  bottom: number;
  visible: boolean;
};

export type SwimLanePositions = {
  pumpMode: SwimLanePosition;
  override: SwimLanePosition;
  profile: SwimLanePosition;
  activity: SwimLanePosition;
};

type SwimLanes = {
  pumpMode: boolean;
  override: boolean;
  profile: boolean;
  activity: boolean;
};

let cachedSwimLaneHeight = 0;
let cachedBasalTrackBottom = 0;
let cachedSwimLanes: SwimLanes | null = null;
let cachedSwimLanePositions: SwimLanePositions | null = null;

export function getSwimLanePositions(
  contextHeight: number,
  basalTrackBottom: number,
  swimLanes: SwimLanes
): SwimLanePositions {
  const swimLaneHeight = contextHeight * SWIM_LANE_HEIGHT;

  if (
    cachedSwimLanePositions &&
    swimLaneHeight === cachedSwimLaneHeight &&
    basalTrackBottom === cachedBasalTrackBottom &&
    cachedSwimLanes &&
    swimLanes.pumpMode === cachedSwimLanes.pumpMode &&
    swimLanes.override === cachedSwimLanes.override &&
    swimLanes.profile === cachedSwimLanes.profile &&
    swimLanes.activity === cachedSwimLanes.activity
  ) {
    return cachedSwimLanePositions;
  }

  let currentY = basalTrackBottom;
  const positions: SwimLanePositions = {
    pumpMode: { top: 0, bottom: 0, visible: false },
    override: { top: 0, bottom: 0, visible: false },
    profile: { top: 0, bottom: 0, visible: false },
    activity: { top: 0, bottom: 0, visible: false },
  };

  const laneOrder = ["pumpMode", "override", "profile", "activity"] as const;
  for (const lane of laneOrder) {
    const visible = swimLanes[lane];
    positions[lane] = {
      top: currentY,
      bottom: visible ? currentY + swimLaneHeight : currentY,
      visible,
    };
    if (visible) currentY += swimLaneHeight;
  }

  cachedSwimLaneHeight = swimLaneHeight;
  cachedBasalTrackBottom = basalTrackBottom;
  cachedSwimLanes = { ...swimLanes };
  cachedSwimLanePositions = positions;

  return positions;
}
