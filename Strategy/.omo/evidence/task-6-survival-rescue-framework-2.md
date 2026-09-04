# Task 6 — Navigation/Avoidance Helpers

## Clearance margin

Both strategy files implement obstacle avoidance with a **350 mm exclusion radius** around each rectangular obstacle center (`avoidRadius = 350.0`). The actual obstacle half-width is ~200 mm (400 mm square), so the effective clearance buffer is ~150 mm.

## AvoidObstacles helper — StrategyFightForLive

Location: `StrategyFightForLive.cs`, `private static void AvoidObstacles(double fx, double fz, double tx, double tz, List<RectangularObstacle> obstacles, out double adjTx, out double adjTz)`

Logic:
1. If no obstacles or fish is far (> avoidRadius × 2.5 from obstacle center), pass target through unchanged.
2. If fish is already inside the avoidRadius, push the adjusted target radially outward.
3. If the straight path from fish to target passes within avoidRadius of any obstacle (perpendicular distance check), lateral-offset the target by 300 mm perpendicular to the fish-obstacle vector.
4. Adjusted target is clamped to field bounds (±1450 mm X, ±950 mm Z for survival 3000×2000 field).

**Called before every MoveTo in all three role branches:**
- Line 165: attacker fish → `AvoidObstacles(ax, az, targetX, targetZ, ...)`
- Line 241: defender fish → `AvoidObstacles(fx, fz, intX, intZ, ...)`
- Line 295: evader fish → `AvoidObstacles(ex, ez, goalX, goalZ, ...)`

## Stuck recovery — StrategyFightForLive

- Evader fish track per-fish `stuckTimer` (int field).
- If fish position changes < 10 mm for 60 consecutive frames, stuck is detected and the fish is rerouted to an alternate corner goal.

## Avoidance in StrategyExtremeRescue

Rescue strategy uses a simpler built-in stuck state machine:
- `_stuckTimer[i]` increments each frame when fish speed is near zero (VCode==0 or no movement).
- After 60 frames stuck → transitions to `FishState.Unstuck`.
- `DoUnstuck(i)`: frames 0–30 spin in place (TCode=0, VCode=3), frames 30–60 reverse (TCode=7, VCode=5), then resets to `SeekBall`.

Hostage positions read from `mission.EnvRef.ObstaclesRound` (circular obstacles = hostages in extreme rescue). Safe zone target at X=1800 mm (or from `HtMissionVariables`).

## Boundary margin

- Survival: targets clamped to ±1450 mm X, ±950 mm Z (50 mm inside 3000×2000 field edge).
- Rescue: targets clamped to ±2200 mm X, ±1450 mm Z (50 mm inside 4500×3000 field edge).

## Fallback waypoint policy

When the direct path is blocked:
- Survival: lateral offset of 300 mm perpendicular to obstacle-fish vector, then clamp to bounds.
- Rescue: full Unstuck state (spin + reverse) for 60 frames, then re-enter SeekBall with fresh target selection.

## Static verification output

```
StrategyFightForLive.cs:
122: List<RectangularObstacle> obstacles = mission.EnvRef.ObstaclesRect;
165: AvoidObstacles(ax, az, targetX, targetZ, obstacles, out adjX, out adjZ);
241: AvoidObstacles(fx, fz, intX, intZ, obstacles, out adjX, out adjZ);
295: AvoidObstacles(ex, ez, goalX, goalZ, obstacles, out adjX, out adjZ);
445: private static void AvoidObstacles(
457:   const double avoidRadius = 350.0;

StrategyExtremeRescue.cs:
47: private int[] _stuckTimer  = new int[] { 0, 0 };
48: private int[] _unstuckTimer = new int[] { 0, 0 };
164: if (_stuckTimer[i] > 60 && _state[i] != FishState.Unstuck)
183: case FishState.Unstuck: DoUnstuck(i);
290: private void DoUnstuck(int i) { ... spin 30f, reverse 30f, reset }
```
