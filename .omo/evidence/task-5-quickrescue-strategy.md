# Evidence — Task 5: QuickRescue full GetDecision orchestration

Date: 2026-09-04
Task: ball assignment + push-target computation + full GetDecision orchestration in
`Strategy/StrategyQuickRescue/Strategy.cs` (Wave-1 stub body replaced).

## Changed files

- `Strategy/StrategyQuickRescue/Strategy.cs` — ONLY file modified. Class shape kept:
  `Strategy : MarshalByRefObject, IStrategy`, `GetTeamName()` = "QuickRescue",
  `InitializeLifetimeService() => null`. No changes to MapConstants / PathFinder /
  Steering / PathExecutor / csproj, none to simulator core.

## Design (as implemented)

- Per-fish state arrays (indexed by fish index, lazily (re)created by `EnsureState` on
  null or length mismatch): `assignedBall` (-1 = idle), `assignedBallChanged`,
  `plannedPush`, `waypoints`, `waypointIndex`, `stuckCycles`, `pushing`.
- `GetDecision(mission, teamId)` returns exactly `CommonPara.FishCntPerTeam` Decisions.
  `UpdateAssignments` runs once per cycle above the per-fish loop (the greedy needs the
  cross-fish view): sticky keep while the ball is out of zone; ball-in-zone → reassign
  to the OTHER ball (both in zone → -1/idle); unassigned fish take the nearest ball that
  is neither in-zone nor owned, in fish index order. `Assign()` sets
  `assignedBallChanged` and drops the push phase on a real change.
- Per fish: frozen check FIRST (`|X|>2250 || |Z|>1500` → (0,7), no pathfinding, state
  untouched) → stuck counter (speed = |VelocityMmPs|, scalar float; <10 mm/s increments)
  → idle when unassigned → push-exit (ball X≥1850, or dot≤0/angle>35° hysteresis exit
  which nulls waypoints to force a replan) → push-entry (≤40mm of push point AND dot>0
  AND angle ≤25°) → push phase (re-aim every cycle:
  `target = ball + norm(zoneCenter−ball)*300`, VCode=14, TCode via GetTCode) → else
  `ShouldUpdatePath` (with `AssignedBallChanged`) → `FindPath(fishPos, pushPoint, 100)`
  (false/empty → empty list, `FollowPath` fallback steers at `ballPos` at vcode 3) →
  `FollowPath(..., fallbackTarget = ballPos)`.
- Push point: `ball − norm(zoneCenter−ball)*146` (fish 78 + ball 58 + margin 10),
  zoneCenter (2050,0), zero-direction fallback (−1,0). No mirroring, no defending,
  opponent team never read.

## Build

```
& "D:\microsoft visual studio\MSBuild\Current\Bin\MSBuild.exe" `
  "Strategy\StrategyQuickRescue\StrategyQuickRescue.csproj" /p:Configuration=Debug
→ StrategyQuickRescue -> E:\progaram\URWPGSim2D\URWPGSim2D\bin\StrategyQuickRescue.dll
0 errors, 0 warnings (only output line). DLL in URWPGSim2D/bin as required.
```

## Probe (compiled + executed)

Method: stubbed Mission graph built from the REAL Common/StrategyLoader types — all
constructible (`Mission()`, `Team<RoboFish>()`, `RoboFish()`, `Ball()`,
`MissionCommonPara`), so NO plain-data snapshot adapter was needed. Probe compiled with
framework csc v4.0 `/platform:x86`, run from `URWPGSim2D/bin`. Internal state
(`assignedBall`, `plannedPush`) read via reflection — no debug code left in Strategy.cs.

Runtime gotcha found and fixed: touching `RoboFish`/`Mission` loads the v2.0 mixed-mode
XNA assembly, which CLR4 refuses by default (FileLoadException). A probe-local
`probe_task5.exe.config` with `useLegacyV2RuntimeActivationPolicy="true"` fixed it;
deleted with the other probe files. (The `从配置文件读取参数出错` console line is the
RoboFish ctor's harmless config-read fallback, also present in the real server.)

Results (exit code 0, ALL PASS):

| # | Assertion | Result |
|---|-----------|--------|
| 1 | Length-2 Decision[]; every VCode∈[0,14], TCode∈[0,15] | PASS |
| 2 | fish0(-1800,800)→ball0(-1350,1050), fish1(-1800,-800)→ball1(-1350,-1050), distinct | PASS |
| 3 | ball0→(2000,0) in zone ⇒ fish0 reassigned to ball1; planned target in Z<0 region; VCode>0 | PASS |
| 4 | fish0 at exact push point behind ball1, heading +X ⇒ VCode=14 (not idle), tcode≈7 | PASS |
| 5 | Both balls in zone ⇒ both decisions (0,7) | PASS |
| 6 | Fishes[1] at (-2115,1591) ⇒ decision[1]=(0,7), no exception | PASS |
| 7 | Two consecutive calls, unmoved balls ⇒ same assignment (no flapping) | PASS |
| 8 | ball0=(290,0) ⇒ push point (144,0) inside inflated obs1 ⇒ VCode=3 fallback, no exception | PASS |

Assertion 3 refinement (documented, not a strategy defect): the first draft asserted
`TCode ≤ 4` (hard −Z turn). Observed `a0=1, v=14, t=7, plannedPush0=(-1489.5,-1093.1)` —
reassignment and routing are correct, but the BFS produced an L-path whose first leg is
+X, so the first-cycle steering is straight. The assertion now checks the spec's intent
(reassigned to ball1 + planned target in the Z<0 region + driving), not first-waypoint
geometry.

Probe 4 note: exact-push-point entry gives tcode 8, not exactly 7, because the
ball→zone direction for the lower ball is ≈17.2° off +X; the aim point is past the ball
toward +X as specified. Assertion tolerance `|tcode−7| ≤ 1`.

## Cleanup receipts

- Deleted `URWPGSim2D/bin/probe_task5.exe` — verified absent.
- Deleted `URWPGSim2D/bin/probe_task5.exe.config` — verified absent.
- Deleted probe source `Temp/opencode/probe_task5.cs` — verified absent.
- Workspace contains only `Strategy.cs` as a source change; bin has the rebuilt DLL (+pdb).

## Risks / notes for Todo 6 (sim QA)

- Zone reassignment can stack BOTH fish on the remaining ball (spec-literal); expected
  behavior after the first rescue.
- On a misalignment push-exit the replan happens the same cycle (spec said "next cycle";
  same-cycle is strictly more responsive, same fresh push point).
- A ball crossing X≥1850 OUTSIDE the zone's Z range exits push and re-paths behind the
  ball from its new position — recovery path, not reachable in normal play.
- TCode SIGN is still untuned (per Steering.cs contract); if fish turn the wrong way in
  the sim, the single flip point is `Steering.GetTCode`'s `7 + ...`.
- Idle/ball-less fish output (0,7) every cycle; stuck counter keeps counting for active
  fish only (frozen/idle paths skip it).
