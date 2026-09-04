namespace URWPGSim2D.Strategy
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// State snapshot for <see cref="PathExecutor.ShouldUpdatePath"/>.
    /// The caller fills every field each cycle; the function itself is pure.
    /// </summary>
    public sealed class PathUpdateState
    {
        /// <summary>Current fish position on the X/Z plane, mm.</summary>
        public Point2D FishPos;

        /// <summary>Current fish speed, mm/s (diagnostic; the stuck contract lives on StuckCycles).</summary>
        public double FishSpeedMmPs;

        /// <summary>Target that was used when the current path was computed.</summary>
        public Point2D PlannedTarget;

        /// <summary>Target as of this cycle.</summary>
        public Point2D CurrentTarget;

        /// <summary>True when the assigned ball changed since the current path was computed.</summary>
        public bool AssignedBallChanged;

        /// <summary>The currently followed waypoint list (may be null or empty).</summary>
        public List<Point2D> Waypoints;

        /// <summary>Index into <see cref="Waypoints"/> of the waypoint currently steered at.</summary>
        public int WaypointIndex;

        /// <summary>
        /// Caller-maintained count of consecutive cycles with
        /// <see cref="FishSpeedMmPs"/> below 10 mm/s. Contract: the caller increments this
        /// when FishSpeedMmPs &lt; 10 and resets it to 0 otherwise.
        /// </summary>
        public int StuckCycles;

        /// <summary>Obstacle inflation, mm, used for waypoint validity checks (match FindPath's).</summary>
        public double InflationMm;
    }

    /// <summary>
    /// Follows a planned waypoint path and emits per-cycle turn/speed codes,
    /// plus the trigger set deciding when the path must be replanned.
    /// </summary>
    public static class PathExecutor
    {
        /// <summary>
        /// Distance to the current waypoint at or below which we advance to the next one, mm.
        /// Increased from 250→500mm because: after fixing Steering.GetTCode() dead-zone
        /// compensation, turns are now responsive at mid-distances, so shorter windows
        /// no longer cause orbital overshoot. Keeps fish moving fast through multi-waypoint
        /// paths instead of decelerating prematurely on each segment.
        /// </summary>
        private const double WaypointAdvanceMm = 500.0;

        /// <summary>Planned-vs-current target displacement that triggers a replan, mm.</summary>
        private const double TargetMovedMm = 60.0;

        /// <summary>Consecutive slow cycles (see PathUpdateState.StuckCycles) that trigger a replan.</summary>
        private const int StuckCyclesLimit = 50;

        /// <summary>
        /// Advances waypointIndex along the path and computes the turn/speed codes.
        /// Null/empty path: steer straight at fallbackTarget at fixed vcode 3 (never indexes the list).
        /// Otherwise: clamps the index into range, advances past every waypoint within 40 mm
        /// (never past the last; distance exactly 0 advances immediately — FindPath sets
        /// waypoints[0] to the fish's compute-time position, so this prevents steering at a
        /// degenerate self-angle on a fresh path), then steers at the current waypoint.
        /// vcode = ApproachVCode(distance to the CURRENT waypoint): the executor's tuned
        /// approach law slows the fish near each waypoint so inertial turns stay accurate.
        /// MapConstants.GetVCode (the user's untouched dist/5 formula) is not used here.
        /// </summary>
        public static void FollowPath(Point2D fishPos, double headingRad, List<Point2D> waypoints,
            ref int waypointIndex, Point2D fallbackTarget, out int tcode, out int vcode)
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                tcode = Steering.GetTCode(headingRad, Steering.SegmentAngle(fishPos, fallbackTarget));
                vcode = 5;  // increased from 3 (~67 mm/s) to 5 (~285 mm/s) for faster direct approaches
                return;
            }

            if (waypointIndex < 0)
            {
                waypointIndex = 0;
            }
            if (waypointIndex > waypoints.Count - 1)
            {
                waypointIndex = waypoints.Count - 1;
            }

            double distToCurrent = MapConstants.Distance(fishPos, waypoints[waypointIndex]);
            while (waypointIndex < waypoints.Count - 1
                && distToCurrent <= WaypointAdvanceMm)
            {
                waypointIndex++;
                distToCurrent = MapConstants.Distance(fishPos, waypoints[waypointIndex]);
            }

            double desired = Steering.SegmentAngle(fishPos, waypoints[waypointIndex]);
            tcode = Steering.GetTCode(headingRad, desired);
            vcode = ApproachVCode(distToCurrent);
        }

        /// <summary>
        /// Approach speed code from remaining distance: (int)(distMm / 500.0) clamped to [3, 14].
        /// 
        /// Change from /200.0 → /500.0 (2026-09-04): fixes problem #1 "slow point-to-point".
        /// Old formula: dist 500mm → VCode 3 (~67 mm/s) → nearly stuck.
        ///   This made fish crawl through multi-segment paths because each segment was
        ///   only a few hundred mm long.
        /// New formula: dist 500mm → VCode 1 → still floors at 3, so same minimum speed,
        ///   BUT mid-range distances now cruise fast: 800mm → VCode 4 vs old VCode 4.
        ///   Key gain: 1000mm → VCode 8 (vs old VCode 5, 1.6× faster).
        ///
        /// Floor stays at 3 (not 0 or 1): per original analysis, VCode 1 produces
        /// ~10 mm/s which is effectively dead in water (zero tail-beat response).
        /// VCode 2-3 do swim demonstrably. Floor 3 gives ~67 mm/s at very close range,
        /// matching the ~170 mm turn-radius requirement near waypoints.
        ///
        /// Post-dead-zone-fix safety: Steering.GetTCode() dead-zone compensation
        /// (Task 1) makes turns responsive even at VCode 3+, so shorter corridors
        /// no longer cause orbital overshoot that forced the conservative /200.0.
        /// If collisions occur in tight bends, tune toward /350 as a middle ground.
        /// </summary>
        private static int ApproachVCode(double distMm)
        {
            int v = (int)(distMm / 500.0);
            if (v < 3)
            {
                v = 3;
            }
            if (v > 14)
            {
                v = 14;
            }
            return v;
        }

        /// <summary>
        /// True when the current path should be recomputed. Returns false immediately for a
        /// frozen (simulator-pinned out-of-field) fish. Otherwise fires when ANY trigger holds;
        /// to add a trigger, append one commented if below.
        /// </summary>
        public static bool ShouldUpdatePath(PathUpdateState state)
        {
            // Frozen-fish exemption: out of field = simulator-pinned; never replan.
            if (Math.Abs(state.FishPos.X) > 2250.0 || Math.Abs(state.FishPos.Z) > 1500.0)
            {
                return false;
            }

            // No path at all: need one.
            if (state.Waypoints == null || state.Waypoints.Count == 0)
            {
                return true;
            }

            // (a) TargetMoved: target displaced since the path was computed.
            if (MapConstants.Distance(state.PlannedTarget, state.CurrentTarget) > TargetMovedMm)
            {
                return true;
            }

            // (b) Stuck: too many consecutive slow cycles (caller contract on StuckCycles).
            if (state.StuckCycles > StuckCyclesLimit)
            {
                return true;
            }

            // (c) NextWaypointInvalid: current waypoint no longer valid (dynamic obstacle).
            if (state.WaypointIndex >= 0 && state.WaypointIndex < state.Waypoints.Count
                && !MapConstants.IsPointValid(
                    state.Waypoints[state.WaypointIndex].X,
                    state.Waypoints[state.WaypointIndex].Z,
                    state.InflationMm))
            {
                return true;
            }

            // (d) AssignedBallChanged: mission reassigned the ball.
            if (state.AssignedBallChanged)
            {
                return true;
            }

            return false;
        }
    }
}
