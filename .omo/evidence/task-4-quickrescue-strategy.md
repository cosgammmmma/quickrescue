# Task 4 — waypoint following + conditional path-update triggers (funcs 5, 8)

Date: 2026-09-04. Wave: single agent (direct workspace build allowed).

## Changed files

- `Strategy/StrategyQuickRescue/PathExecutor.cs` — full rewrite of the Wave-1 stub:
  - `PathUpdateState` (new public sealed class): `FishPos`, `FishSpeedMmPs`, `PlannedTarget`,
    `CurrentTarget`, `AssignedBallChanged`, `Waypoints`, `WaypointIndex`, `StuckCycles`,
    `InflationMm`. StuckCycles XML-doc carries the caller contract (increment when
    FishSpeedMmPs < 10, reset to 0 otherwise).
  - `FollowPath(Point2D fishPos, double headingRad, List<Point2D> waypoints, ref int waypointIndex,
    Point2D fallbackTarget, out int tcode, out int vcode)` — fallback steer (tcode via
    `GetTCode`/`SegmentAngle`, vcode 3) on null/empty list; index clamped into [0, Count-1];
    40 mm advance loop `while (idx < Count-1 && distToCurrent <= 40) idx++` (inclusive of
    distance 0 — never past the last waypoint); tcode toward the current waypoint;
    `vcode = GetVCode(distToCurrent)` where distToCurrent is the distance to
    `waypoints[waypointIndex]` AFTER the advance loop (plan formula verbatim — the fish
    slows near each waypoint so inertial turns stay accurate).
  - `ShouldUpdatePath(PathUpdateState state)` — frozen-fish exemption first
    (`|X|>2250 || |Z|>1500` → false), then no-path → true, then one commented `if` per
    trigger: (a) TargetMoved > 60 mm, (b) Stuck > 50 cycles, (c) NextWaypointInvalid under
    `InflationMm`, (d) AssignedBallChanged. Constants `WaypointAdvanceMm=40`,
    `TargetMovedMm=60`, `StuckCyclesLimit=50` are named at class level.
- `Strategy/StrategyQuickRescue/PathFinder.cs` — removed ONLY the `ShouldUpdatePath` stub
  (method + XML doc). `FindPath` and everything else untouched. Nothing referenced the stub.

## Build

```
& "D:\microsoft visual studio\MSBuild\Current\Bin\MSBuild.exe" \
  "Strategy\StrategyQuickRescue\StrategyQuickRescue.csproj" /p:Configuration=Debug
```
Result: 0 errors, 0 warnings → `URWPGSim2D/bin/StrategyQuickRescue.dll`.

## Probe

Temp console compiled with
`C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /platform:x86` referencing
`StrategyQuickRescue.dll` + `URWPGSim2D.Common.dll` + `URWPGSim2D.StrategyLoader.dll`,
exe placed in `URWPGSim2D/bin`, run there (exit code 0), then both probe .cs and .exe deleted.

Results — 20/20 PASS:

| # | Assertion | Result |
|---|-----------|--------|
| 1 | fresh straight path, fish exactly at wp[0], heading 0 → dist 0 <= 40 advances idx to 1, t=7, v=GetVCode(1000)=14 | PASS |
| 2 | fish at (10,0) → idx advances to 1, t=7, v=GetVCode(990)=14 | PASS |
| 3 | fish at (995,0), idx 1 → clamped at end, v=GetVCode(5)=1, t=7 | PASS |
| 4 | idx=99 on 2-waypoint list → clamped to 1, no exception | PASS |
| 5 | empty list fallback: (+500,0) → t=7 v=3; (−500,0) → t=15 v=3 | PASS |
| 6 | null waypoints → same fallback (t=7 v=3), no exception | PASS |
| 7a | TargetMoved 100 mm → true; 30 mm → false | PASS |
| 7b | StuckCycles 51 → true; 50 → false | PASS |
| 7c | waypoint (0,0) invalid @inflation 100 (inside obs1) → true; (−2000,0) → false | PASS |
| 7d | AssignedBallChanged → true; all-clear → false | PASS |
| 7f/g | empty / null waypoints (no path) → true | PASS |
| 8 | frozen fish (−2115,1591), stuck 999, empty path → false (exemption wins) | PASS |
| 9 | fish (2300,0), stuck 999 → false | PASS |
| 10 | fresh-path no-lurch: path heads −X, fish exactly at (0,0) heading +X → idx 1, t=GetTCode(0,π)=15 (immediate turn toward −X, NOT 7 along degenerate self-angle) | PASS |

## Probe-1 correction (orchestrator review fix)

The first submission treated an apparent spec conflict (plan pseudocode vs. probe
assertion 1) as a spec bug and resolved it with a `distToCurrent > 0` advance guard plus a
final-waypoint vcode. Orchestrator review corrected this: the bug was in the probe
assertion, not the plan. The plan formula is authoritative and behaviorally better:

1. `vcode = GetVCode(Distance(fishPos, waypoints[waypointIndex]))` — distance to the
   CURRENT waypoint. A final-waypoint vcode would run vcode 14 through every intermediate
   turn point; the plan deliberately slows the fish near each waypoint (physical fish has
   inertia; turn accuracy matters).
2. The `> 0` guard created a wrong-way lurch: FindPath sets waypoints[0] = the fish's
   compute-time position, so the first FollowPath call on every fresh path has dist == 0
   exactly → no advance → SegmentAngle(fishPos, own position) = atan2(0,0) degenerates to
   angle 0 → the fish steers toward angle 0 regardless of where the path leads. The
   inclusive `<= 40` advance eliminates this: at dist 0 the index advances immediately.
   Probe 10 locks this behavior (path heading −X, fish facing +X → tcode 15, not 7).

Probe assertion 1 was corrected to expect the advance (idx 0 → 1, t=7, v=14); all other
assertions unchanged and still passing; probe 10 added.

## Verification

- MSBuild Debug: 0 errors / 0 warnings (net35, C# 3 — no auto-property initializers,
  expression-bodied members, string interpolation, out-var, or ValueTuple used).
- Probe: 20/20 PASS, process exit code 0.
- LSP: no C# server installed (user previously declined); compiler diagnostics used instead.

## Risks / notes for Todo 5+

- FollowPath is called per cycle with caller-held `waypointIndex`; the clamp makes stale
  indices safe after a replan shortens the list.
- Trigger thresholds are named constants — Todo 6 tuning touches only those.
- Frozen-fish detection is purely positional; a fish legitimately driven out of bounds
  would also be exempted — impossible under normal physics (bounds enforced by simulator).
- A fish standing exactly on a non-final waypoint advances past it in the SAME call
  (inclusive 40 mm threshold), so vcode is computed from the next waypoint — no
  zero-distance vcode-0 stall cycle exists.
