namespace URWPGSim2D.Strategy
{
    using System;
    using System.Collections.Generic;
    using URWPGSim2D.Common;
    using URWPGSim2D.StrategyLoader;

    /// <summary>
    /// Strategy entry point for the QuickRescue mission.
    /// The loader hard-requires the full type name URWPGSim2D.Strategy.Strategy.
    /// Per cycle: sticky greedy ball assignment, a waypoint path to the push point behind
    /// the assigned ball (opposite the rescue zone), then a live-re-aimed push phase once
    /// the fish is lined up behind the ball. Frozen (simulator-pinned out-of-field) fish
    /// and teams with nothing left to rescue idle at (VCode 0, TCode 7).
    /// </summary>
    public class Strategy : MarshalByRefObject, IStrategy
    {
        /// <summary>Rescue zone bounds, mm (Match/QuickRescue.cs GoalHandler).</summary>
        private const double ZoneMinX = 1850.0;
        private const double ZoneMaxX = 2250.0;
        private const double ZoneMinZ = -500.0;
        private const double ZoneMaxZ = 500.0;

        /// <summary>Push standoff behind the ball: fish body radius 78 + ball radius 58 + margin 10, mm.</summary>
        private const double PushOffsetMm = 146.0;

        /// <summary>
        /// Aim distance past the ball while pushing, mm.
        /// Todo 6 QA tuning (2026-09-04, documented threshold tweak): 300 -> 600.
        /// Run 9 trace: the fish enters the push phase mid-orbit with a ~90 deg heading
        /// error; the 300 mm aim point let the 290 mm/s charge turn short and sweep past
        /// the ball laterally (closest approach ~190 mm > 136 mm contact, dot&lt;=0 drop).
        /// A 600 mm aim point keeps the charge line collinear with the ball->zone axis for
        /// longer, so the same sweep passes through the ball instead of beside it.
        /// </summary>
        private const double PushAimAheadMm = 600.0;

        /// <summary>
        /// Distance to the push point at or below which push-phase entry is tested, mm.
        /// Todo 6 QA tuning (2026-09-04, documented threshold tweak): 40 -> 200. With the
        /// simulator's max turn rate 0.394 rad/s the fish's capture radius at approach speed
        /// VCode 3 (67 mm/s) is ~170 mm, so a 40 mm entry window was unreachable (fish orbited
        /// the push point indefinitely, zero ball contacts). 200 mm is reachable from the
        /// approach orbit; the 25-degree alignment gate still guarantees the fish is behind
        /// the ball pointing at the rescue zone before the push begins.
        /// </summary>
        private const double PushEnterDistMm = 200.0;

        /// <summary>Max fish-&gt;ball vs ball-&gt;zone misalignment for push-phase entry, degrees.</summary>
        private const double PushEnterAngleDeg = 25.0;

        /// <summary>
        /// Misalignment that drops the fish out of the push phase, degrees.
        /// Todo 6 QA tuning (2026-09-04, documented threshold tweak): 35 -> 175.
        /// The fish crawls onto the push point heading WEST (toward the standoff), but the
        /// push aim is 180deg back EAST through the ball. With the simulator's max turn rate
        /// (0.394 rad/s) the heading flip takes ~8 s and sweeps the fish ~300 mm off the
        /// ball->zone axis, so any position-based exit gate below ~120 deg aborts every push
        /// during the mandatory flip (run 5: push point reached, zero ball contacts in the
        /// whole half). 175 deg lets the flip complete; the dot&lt;=0 guard (ball escaped
        /// behind the fish) remains as the genuine drop condition.
        /// </summary>
        private const double PushExitAngleDeg = 175.0;

        /// <summary>Obstacle inflation for FindPath and waypoint validity checks, mm.</summary>
        private const double InflationMm = 100.0;

        /// <summary>Speed below which a cycle counts toward the stuck counter, mm/s.</summary>
        private const double StuckSpeedMmPs = 10.0;

        /// <summary>Rescue zone center, mm.</summary>
        private static readonly Point2D ZoneCenter = new Point2D((ZoneMinX + ZoneMaxX) / 2.0, 0.0);

        private Decision[] decisions = null;

        // Per-fish state, arrays indexed by fish index, lazily created by EnsureState.
        private int[] assignedBall = null;          // index into EnvRef.Balls; -1 = none (idle)
        private bool[] assignedBallChanged = null;  // assignment changed since the current path was planned
        private Point2D[] plannedPush = null;       // push point the current path was built for
        private List<Point2D>[] waypoints = null;   // current path (null/empty = none)
        private int[] waypointIndex = null;         // index into waypoints currently steered at
        private int[] stuckCycles = null;           // consecutive cycles below StuckSpeedMmPs
        private bool[] pushing = null;              // true while in the push phase (waypoints ignored)

        public string GetTeamName()
        {
            return "QuickRescue";
        }

        public Decision[] GetDecision(Mission mission, int teamId)
        {
            return GetDecisionCore(mission, teamId);
        }

        private Decision[] GetDecisionCore(Mission mission, int teamId)
        {
            int fishCount = mission.CommonPara.FishCntPerTeam;
            EnsureState(fishCount);

            Team<RoboFish> team = mission.TeamsRef[teamId];
            List<Ball> balls = mission.EnvRef.Balls;

            UpdateAssignments(team, balls, fishCount);

            for (int i = 0; i < fishCount; i++)
            {
                RoboFish fish = team.Fishes[i];
                Point2D fishPos = new Point2D(fish.PositionMm.X, fish.PositionMm.Z);
                double heading = fish.BodyDirectionRad;

                // Frozen check first: a simulator-pinned (out-of-field) fish idles;
                // never pathfind for it.
                if (Math.Abs(fish.PositionMm.X) > MapConstants.RightMm
                    || Math.Abs(fish.PositionMm.Z) > MapConstants.BottomMm)
                {
                    this.decisions[i].VCode = 0;
                    this.decisions[i].TCode = 7;
                    continue;
                }

                double speed = Math.Abs((double)fish.VelocityMmPs);
                if (speed < StuckSpeedMmPs)
                {
                    this.stuckCycles[i]++;
                }
                else
                {
                    this.stuckCycles[i] = 0;
                }

                if (this.assignedBall[i] < 0)
                {
                    // Nothing left to rescue (both balls inside the zone): idle.
                    this.decisions[i].VCode = 0;
                    this.decisions[i].TCode = 7;
                    continue;
                }

                Ball ball = balls[this.assignedBall[i]];
                Point2D ballPos = new Point2D(ball.PositionMm.X, ball.PositionMm.Z);
                Point2D pushPoint = ComputePushPoint(ballPos);

                if (this.pushing[i])
                {
                    // Push-phase exit: ball rescued (crossed the zone line) or alignment lost.
                    if (ballPos.X >= ZoneMinX)
                    {
                        this.pushing[i] = false;
                    }
                    else
                    {
                        double exitAngleDeg;
                        double exitDot = Alignment(fishPos, ballPos, out exitAngleDeg);
                        if (exitDot <= 0.0 || exitAngleDeg > PushExitAngleDeg)
                        {
                            this.pushing[i] = false;
                            this.waypoints[i] = null; // force a replan to a fresh push point
                        }
                    }
                }
                else if (MapConstants.Distance(fishPos, pushPoint) <= PushEnterDistMm)
                {
                    // Push-phase entry: at the push point and collinear behind the ball
                    // (ball between fish and zone center).
                    double enterAngleDeg;
                    double enterDot = Alignment(fishPos, ballPos, out enterAngleDeg);
                    if (enterDot > 0.0 && enterAngleDeg <= PushEnterAngleDeg)
                    {
                        this.pushing[i] = true;
                    }
                }

                if (this.pushing[i])
                {
                    // Ignore waypoints; re-aim every cycle from the ball's live position,
                    // past the ball toward the zone center.
                    Point2D aimDir = NormalizeDirection(
                        ZoneCenter.X - ballPos.X, ZoneCenter.Z - ballPos.Z, -1.0, 0.0);
                    Point2D target = new Point2D(
                        ballPos.X + aimDir.X * PushAimAheadMm,
                        ballPos.Z + aimDir.Z * PushAimAheadMm);
                    this.decisions[i].TCode = Steering.GetTCode(heading, Steering.SegmentAngle(fishPos, target));
                    this.decisions[i].VCode = 14;
                    continue;
                }

                PathUpdateState state = new PathUpdateState();
                state.FishPos = fishPos;
                state.FishSpeedMmPs = speed;
                state.PlannedTarget = this.plannedPush[i];
                state.CurrentTarget = pushPoint;
                state.AssignedBallChanged = this.assignedBallChanged[i];
                state.Waypoints = this.waypoints[i];
                state.WaypointIndex = this.waypointIndex[i];
                state.StuckCycles = this.stuckCycles[i];
                state.InflationMm = InflationMm;
                if (PathExecutor.ShouldUpdatePath(state))
                {
                    List<Point2D> fresh;
                    if (PathFinder.FindPath(fishPos, pushPoint, InflationMm, out fresh) && fresh.Count > 0)
                    {
                        this.waypoints[i] = fresh;
                    }
                    else
                    {
                        // Invalid/unreachable push point: keep an empty list; FollowPath's
                        // fallback steers straight at the ball at vcode 3.
                        this.waypoints[i] = new List<Point2D>();
                    }
                    this.waypointIndex[i] = 0;
                    this.plannedPush[i] = pushPoint;
                    this.assignedBallChanged[i] = false;
                }

                int tcode;
                int vcode;
                int index = this.waypointIndex[i];
                PathExecutor.FollowPath(fishPos, heading, this.waypoints[i], ref index, ballPos, out tcode, out vcode);
                this.waypointIndex[i] = index;
                this.decisions[i].VCode = vcode;
                this.decisions[i].TCode = tcode;
            }
            return this.decisions;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        /// <summary>Lazy (re)creation of the decision buffer and per-fish state arrays.</summary>
        private void EnsureState(int fishCount)
        {
            if (this.decisions != null && this.decisions.Length == fishCount)
            {
                return;
            }
            this.decisions = new Decision[fishCount];
            this.assignedBall = new int[fishCount];
            this.assignedBallChanged = new bool[fishCount];
            this.plannedPush = new Point2D[fishCount];
            this.waypoints = new List<Point2D>[fishCount];
            this.waypointIndex = new int[fishCount];
            this.stuckCycles = new int[fishCount];
            this.pushing = new bool[fishCount];
            for (int i = 0; i < fishCount; i++)
            {
                this.assignedBall[i] = -1;
            }
        }

        /// <summary>
        /// Sticky greedy assignment, processed in fish index order. An assigned fish keeps its
        /// ball while it stays outside the rescue zone; a fish whose ball entered the zone is
        /// moved to the other ball, or idles when both balls are inside. An unassigned fish
        /// takes the nearest ball that is neither in the zone nor owned by another fish.
        /// </summary>
        private void UpdateAssignments(Team<RoboFish> team, List<Ball> balls, int fishCount)
        {
            int ballCount = balls.Count;

            for (int i = 0; i < fishCount; i++)
            {
                int b = this.assignedBall[i];
                if (b < 0 || b >= ballCount)
                {
                    continue;
                }
                if (IsInZone(balls[b].PositionMm.X, balls[b].PositionMm.Z))
                {
                    int other = (b == 0) ? 1 : 0; // the mission has exactly 2 balls
                    if (other < ballCount && !IsInZone(balls[other].PositionMm.X, balls[other].PositionMm.Z))
                    {
                        Assign(i, other);
                    }
                    else
                    {
                        Assign(i, -1); // both balls inside the zone: idle
                    }
                }
            }

            for (int i = 0; i < fishCount; i++)
            {
                if (this.assignedBall[i] >= 0)
                {
                    continue;
                }
                RoboFish fish = team.Fishes[i];
                Point2D fishPos = new Point2D(fish.PositionMm.X, fish.PositionMm.Z);
                int best = -1;
                double bestDist = double.MaxValue;
                for (int b = 0; b < ballCount; b++)
                {
                    if (IsInZone(balls[b].PositionMm.X, balls[b].PositionMm.Z))
                    {
                        continue;
                    }
                    bool taken = false;
                    for (int j = 0; j < fishCount; j++)
                    {
                        if (j != i && this.assignedBall[j] == b)
                        {
                            taken = true;
                            break;
                        }
                    }
                    if (taken)
                    {
                        continue;
                    }
                    double d = MapConstants.Distance(fishPos,
                        new Point2D(balls[b].PositionMm.X, balls[b].PositionMm.Z));
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = b;
                    }
                }
                if (best >= 0)
                {
                    Assign(i, best);
                }
            }
        }

        /// <summary>Records an assignment; a real change flags the replan trigger and drops the push phase.</summary>
        private void Assign(int fishIndex, int ballIndex)
        {
            if (this.assignedBall[fishIndex] != ballIndex)
            {
                this.assignedBall[fishIndex] = ballIndex;
                this.assignedBallChanged[fishIndex] = true;
                this.pushing[fishIndex] = false;
            }
        }

        /// <summary>True when (x, z) lies inside the rescue zone rectangle.</summary>
        private static bool IsInZone(double x, double z)
        {
            return x >= ZoneMinX && x <= ZoneMaxX && z >= ZoneMinZ && z <= ZoneMaxZ;
        }

        /// <summary>
        /// Standoff point behind the ball on the ball-&gt;zone axis: the spot the fish must
        /// reach so that driving forward pushes the ball toward the zone center.
        /// </summary>
        private static Point2D ComputePushPoint(Point2D ballPos)
        {
            Point2D dir = NormalizeDirection(
                ZoneCenter.X - ballPos.X, ZoneCenter.Z - ballPos.Z, -1.0, 0.0);
            return new Point2D(ballPos.X - dir.X * PushOffsetMm, ballPos.Z - dir.Z * PushOffsetMm);
        }

        /// <summary>Unit vector of (x, z); the fallback direction when the vector is (near) zero.</summary>
        private static Point2D NormalizeDirection(double x, double z, double fallbackX, double fallbackZ)
        {
            double len = Math.Sqrt(x * x + z * z);
            if (len < 1e-9)
            {
                return new Point2D(fallbackX, fallbackZ);
            }
            return new Point2D(x / len, z / len);
        }

        /// <summary>
        /// Alignment of the fish-&gt;ball and ball-&gt;zone-center vectors. Returns their dot
        /// product; angleDeg receives the angle between them in degrees (180 when either
        /// vector is degenerate).
        /// </summary>
        private static double Alignment(Point2D fishPos, Point2D ballPos, out double angleDeg)
        {
            double ux = ballPos.X - fishPos.X;
            double uz = ballPos.Z - fishPos.Z;
            double vx = ZoneCenter.X - ballPos.X;
            double vz = ZoneCenter.Z - ballPos.Z;
            double dot = ux * vx + uz * vz;
            double lu = Math.Sqrt(ux * ux + uz * uz);
            double lv = Math.Sqrt(vx * vx + vz * vz);
            if (lu < 1e-9 || lv < 1e-9)
            {
                angleDeg = 180.0;
                return dot;
            }
            double cos = dot / (lu * lv);
            if (cos > 1.0)
            {
                cos = 1.0;
            }
            if (cos < -1.0)
            {
                cos = -1.0;
            }
            angleDeg = Math.Acos(cos) * 180.0 / Math.PI;
            return dot;
        }
    }
}
