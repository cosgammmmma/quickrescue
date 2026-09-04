# quickrescue-strategy - Work Plan

## TL;DR (For humans)
**What you'll get:** A new standalone `Strategy/StrategyQuickRescue` project (a .NET 3.5 class library) that the URWPGSim2D simulator can load, containing a complete robot-fish strategy: it figures out a safe shortest path around the 5 obstacles, swims each of its 2 fish to a ball, and pushes the 2 balls into the right-side rescue zone — built around exactly the 8 helper functions you specified, plus the thin orchestration that ties them together.

**Why this approach:** The 8 functions you listed are geometry + navigation primitives; the only safe way to make them do something real is a BFS over a grid where each obstacle is "fattened" by the fish's body radius (~78 mm) so paths never clip walls, and a greedy 2-fish/2-ball assignment that pushes each ball toward the single fixed rescue zone (no mirroring — the zone is always on the right, only *which team scores* flips at half-court).

**What it will NOT do:** It will not touch the simulator core (`Match/`, `Common/`, `StrategyLoader/`), will not reuse the sample's 9-ball/5v5 soccer logic, and will not rely on the (empty) tuning table — speed is your exact `dist/5` formula. No GUI changes, no automated test suite (agent-run QA only, as you chose).

**Effort:** Medium
**Risk:** Medium — the turn/speed physics constants (`TCode` mapping, asymmetric turn rates) are not fully documented in source, so the exact steering will be tuned empirically in the simulator rather than derived. Note: "always push right, no defending" means a defending team's active fish will push balls into the right zone and score **for the opponent** (points go to the fixed team per half, not the pusher) — acceptable for the demo success criterion, but not a competitive strategy.
**Decisions to sanity-check:** (1) greedy nearest-ball assignment, (2) "always push balls into the right zone regardless of half" (no defending), (3) grid step 40 mm + obstacle inflation 100 mm.

Your next move: approve, or run a high-accuracy review. Full execution detail follows below.

---

> TL;DR (machine): Medium effort / Medium risk; 6 todos across 5 waves; deliver a compilable `Strategy/StrategyQuickRescue` net35 DLL whose class `URWPGSim2D.Strategy.Strategy` pushes 2 balls into the fixed right rescue zone via BFS+inflation+waypoint following; QA = build + simulator smoke test, no test suite.

## Scope
### Must have
- A new standalone `Strategy/StrategyQuickRescue` project: `.csproj` (net35, OutputType=Library, x86) + `Properties/AssemblyInfo.cs` + `Strategy.cs` + helper files. `RootNamespace` must be `URWPGSim2D.Strategy` and the strategy class must be named `Strategy` so its full type name is exactly `URWPGSim2D.Strategy.Strategy` (hard-required by the loader).
- `Strategy : MarshalByRefObject, IStrategy` with `GetTeamName()`, `Decision[] GetDecision(Mission mission, int teamId)`, and `override object InitializeLifetimeService() { return null; }`. Return exactly `FishCntPerTeam` (=2) `Decision` per call.
- The 8 user-specified functions:
  1. Constant map bounds + obstacle ranges + point-validity check.
  2. Two-point 2D distance (X,Z only; ignore Y).
  3. Fast-move speed: `vcode = (int)(distMm / 5.0)`, clamp to `[0,14]`.
  4. BFS shortest path honoring fish body size, emitting ordered waypoints (turn points + start/end).
  5. Follow waypoints in order at fast speed (turn toward each, advance on arrival).
  6. Compute angle of the segment between two consecutive waypoints.
  7. Legal-turn function: map a desired-vs-current heading delta to a legal `TCode` (0..15).
  8. Conditional path-update function triggered by an extensible condition set (not every frame).
- Full `GetDecision` orchestration: assign balls → navigate to ball → push ball into the rescue zone → loop.

### Must NOT have (guardrails, anti-slop, scope boundaries)
- No edits to `Match/`, `Common/`, `StrategyLoader/`, or any simulator core file.
- No 9-ball / 5v5 soccer logic copied from `StrategyNew2V2`.
- No reliance on `RoboFish.DataBasedOnExperiment` (unpopulated) — use the explicit `dist/5` formula.
- No GUI/front-end changes. No automated test project (user chose none).
- No mirroring / half-court logic for the *target zone*: the zone is always X∈[1850,2250], Z∈[-500,500].
- No defensive behavior (blocking the opponent). Every fish always works to push its ball into the right zone.

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Test decision: **none** (user). Verification = successful `msbuild` compile + simulator smoke test + recorded evidence.
- Evidence: `.omo/evidence/task-<N>-quickrescue-strategy.md` (or `.txt`/`.png`) per todo, plus a final simulator screenshot showing balls inside the rescue zone.

## Execution strategy
### Parallel execution waves
> This task is genuinely sequential (foundation → primitives → integration). Wave 1 is a single "contract" todo; waves 2–5 have fewer than 3 todos by necessity, not under-splitting.
- **Wave 1** — Todo 1 (scaffold + shared contract + funcs 1/2/3). Defines the types/signatures everything else compiles against.
- **Wave 2** (parallel) — Todo 2 (BFS, func 4) ∥ Todo 3 (steering, funcs 6/7). Both depend only on Todo 1.
- **Wave 3** — Todo 4 (executor, funcs 5/8). Depends on 2+3.
- **Wave 4** — Todo 5 (orchestration + assignment + push). Depends on 2+3+4.
- **Wave 5** — Todo 6 (build + load + QA + evidence). Depends on all.

### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 | — | 2,3,4,5,6 | — |
| 2 | 1 | 4,5,6 | 3 |
| 3 | 1 | 4,5,6 | 2 |
| 4 | 2,3 | 5,6 | — |
| 5 | 2,3,4 | 6 | — |
| 6 | 1,2,3,4,5 | — | — |

## Todos
> Implementation + Test = ONE todo. Never separate.
<!-- APPEND TASK BATCHES BELOW THIS LINE WITH edit/apply_patch - never rewrite the headers above. -->
- [x] 1. Scaffold the `Strategy/StrategyQuickRescue` project + shared contract + geometry primitives (funcs 1, 2, 3)
  What to do / Must NOT do: Copy `Strategy/StrategyNew2V2/StrategyNew2V2.csproj` to `Strategy/StrategyQuickRescue/StrategyQuickRescue.csproj`; keep ALL references, `FrameworkPathOverride`, `PlatformTarget=x86`, `OutputType=Library`, and `RootNamespace=URWPGSim2D.Strategy`; set `AssemblyName=StrategyQuickRescue` and confirm OutputPath mirrors the sample (writes to `..\..\URWPGSim2D\bin\`). Add `Properties/AssemblyInfo.cs`. Create `Strategy.cs` with a compiling stub `Strategy : MarshalByRefObject, IStrategy` (`GetTeamName`, `GetDecision`, `InitializeLifetimeService`), and a `MapConstants.cs` defining: field bounds, the 5 obstacle AABBs (below), `Point2D`, `Distance(p1,p2)`, `GetVCode(distMm)`, `IsPointValid(x,z,inflationMm)` (inflation also shrinks the walkable field: valid `X∈[-2250+infl, 2250-infl]`, `Z∈[-1500+infl, 1500-infl]`). Add empty stub signatures for `FindPath`, `Steer`/`GetTCode`, `SegmentAngle`, `FollowPath`, `ShouldUpdatePath` so the project compiles immediately. MUST NOT: change RootNamespace or the class name (loader needs `URWPGSim2D.Strategy.Strategy`); do not touch simulator core files.
  Parallelization: Wave 1 | Blocked by: — | Blocks: 2,3,4,5,6
  References (executor has NO interview context - be exhaustive):
  - Loader type-name + interface: `StrategyLoader/StrategyLoader.cs` (IStrategy: `GetTeamName()` + `Decision[] GetDecision(Mission,int)`; `MarshalByRefObject`; factory `Create(strCachedStrategyFullName, "URWPGSim2D.Strategy.Strategy", null)`); `StrategyLoader/TeamStrategyUiTypes.cs:182` hardcodes `"URWPGSim2D.Strategy.Strategy"`.
  - csproj template: `Strategy/StrategyNew2V2/StrategyNew2V2.csproj` (net35, Library, `RootNamespace=URWPGSim2D.Strategy`, references `URWPGSim2D.Common/StrategyHelper/StrategyLoader.dll` + XNA 3.1 x86 + net35 `FrameworkPathOverride`).
  - Class shape: `Strategy/StrategyNew2V2/Strategy.cs` (`Strategy : MarshalByRefObject, IStrategy`, `InitializeLifetimeService()=>null`).
  - Field bounds (mm): `Common/Environment.cs` `Field` — `LeftMm=-2250, RightMm=2250, TopMm=-1500, BottomMm=1500` (4500×3000). `Match/QuickRescue.cs:348` `FieldLengthXMm=4500`.
  - Obstacles (from `Match/QuickRescue.cs:164-184`; `RectangularObstacle(name, pos, Color.Green, 100, 1100, 0)` — `LengthMm`→X, `WidthMm`→Z, `DirectionRad=0`, per `Common/Environment.cs` `RectangularObstacle` ctor + `CalculateCollisionDetectionParas`):
    - obs1 (0,0): X∈[-50,50], Z∈[-550,550]
    - obs2 (550,950): X∈[500,600], Z∈[400,1500]
    - obs3 (-550,950): X∈[-600,-500], Z∈[400,1500]
    - obs4 (550,-950): X∈[500,600], Z∈[-1500,-400]
    - obs5 (-550,-950): X∈[-600,-500], Z∈[-1500,-400]
  - Distance/vcode: user formula `vcode=(int)(distMm/5.0)` clamp [0,14]. 2D distance uses `PositionMm.X`/`.Z` (ignore `.Y`).
  Acceptance criteria (agent-executable): `msbuild Strategy/StrategyQuickRescue/StrategyQuickRescue.csproj /p:Configuration=Debug` succeeds (0 errors, 0 warnings tolerated) and emits the DLL. `grep -n "namespace URWPGSim2D.Strategy"` and `class Strategy` present; `GetVCode(70)==14`, `GetVCode(10)==2`, `Distance((0,0),(30,40))==50.0`, `IsPointValid(0,0,0)==false` (inside obs1), `IsPointValid(-2000,0,0)==true`, `IsPointValid(-2151,0,100)==false`, `IsPointValid(-2149,0,100)==true` (field boundary shrinks by inflation).
  QA scenarios (name the exact tool + invocation): build via `msbuild`; assert with a small compiled probe OR inline asserts in `MapConstants` run during a temporary `GetTeamName` self-test (then removed). Evidence `.omo/evidence/task-1-quickrescue-strategy.md`.
  Commit: N | chore(strategy): scaffold StrategyQuickRescue project + geometry primitives

- [x] 2. Implement BFS shortest path with body-size obstacle inflation + waypoint compression (func 4)
  What to do / Must NOT do: Implement `FindPath(Point2D start, Point2D target, double inflationMm, out List<Point2D> waypoints)` in `PathFinder.cs`. Grid step **40 mm** over the field; a cell is valid iff `IsPointValid(cx,cz,inflationMm)` with `inflationMm = 100` (fish body radius 78 + 22 margin). Use 4-connected neighbors (prevents corner-cutting into inflated obstacles). Build parent chain from start to target, snap start/target to their nearest valid grid centers, then compress the returned list to turn points only (drop collinear middle points via cross-product ≈ 0) keeping start/end. Return empty/`false` if unreachable (callers must handle empty — see Todos 4/5 fallback). MUST NOT: use 8-neighbor diagonal cutting; hardcode a hand path; ignore inflation.
  Parallelization: Wave 2 | Blocked by: 1 | Blocks: 4,5,6
  References: `MapConstants.cs` (bounds, obstacles, `IsPointValid`, `Point2D`); fish body radius `Common/RoboFish.cs:137` `FishBodyRadiusMm≈78`; body 150×44 (`RoboFish.cs:109-110`).
  Acceptance criteria (agent-executable): compile via `msbuild`; a probe asserts `FindPath` from `(-2000,0)` to `(2000,0)` returns a non-empty waypoint list whose every point satisfies `IsPointValid(pt,100)` and whose adjacent segments do not intersect any inflated AABB; compressed path has strictly fewer points than raw grid path.
  QA scenarios: probe run for happy (clear lane `(-2000,0)→(2000,0)` along bottom/center gaps) + failure (start inside an obstacle → returns unreachable). Evidence `.omo/evidence/task-2-quickrescue-strategy.md`.
  Commit: N | feat(strategy): BFS pathfinding with obstacle inflation + waypoint compression

- [x] 3. Implement steering math: segment angle + legal-turn TCode mapping (funcs 6, 7)
  What to do / Must NOT do: In `Steering.cs`: `SegmentAngle(a,b)` = `atan2(b.Z-a.Z, b.X-a.X)`; `FormatAngle(a)` wraps to (-π,π] (so `π` and `-π` both map to `π`, `-3π`→`π`); `GetTCode(currentRad, desiredRad)` = `clamp(7 + round(delta*8/π), 0, 15)` where `delta=FormatAngle(desired-current)`. Sign convention (per `Decision` struct, `Common/RoboFish.cs:1448` — "0=left/7=straight/15=right"): `TCode=7` straight, `0` full-left, `15` full-right; the geometric sign of positive `delta`→TCode is **empirical** — tune sign (not just scale) in Todo 6. MUST NOT: emit TCode outside [0,15]; forget normalization across ±π.
  Parallelization: Wave 2 | Blocked by: 1 | Blocks: 4,5,6
  References: `Common/RoboFish.cs:1439-1451` `Decision.VCode/TCode` (0..15, 7 = straight); `RoboFish.BodyDirectionRad` (heading, radians, +X axis).
  Acceptance criteria (agent-executable): compile; probe asserts `GetTCode(0,0)==7`, `GetTCode(0,π)==15`, `GetTCode(0,-π)==15` (-π≡π under the (-π,π] wrap), `GetTCode(0,π/2)==11`, `GetTCode(0,-π/2)==3`, and `FormatAngle(3π)==π`, `FormatAngle(-3π)==π`, `FormatAngle(-2π)==0`.
  QA scenarios: probe table above (happy) + boundary (delta just past π wraps correctly, no 15→0 jump). Evidence `.omo/evidence/task-3-quickrescue-strategy.md`.
  Commit: N | feat(strategy): steering angle + TCode legal-turn mapping

- [x] 4. Implement waypoint following + conditional path-update triggers (funcs 5, 8)
  What to do / Must NOT do: In `PathExecutor.cs`: `FollowPath(fishPos, heading, waypoints, ref index)` → returns `(TCode, VCode)`; compute desired heading to `waypoints[index]`, `TCode=GetTCode(heading, desired)`, `VCode=GetVCode(Distance(fishPos, waypoints[index]))`; advance `index` when within **40 mm** of the waypoint; if `waypoints` is empty/unreachable, return a straight-line steer toward the assigned ball (TCode from `GetTCode`, low `VCode=3`) instead of indexing the list. `ShouldUpdatePath(state)` returns true iff any trigger: (a) target moved > 60 mm from planned position, (b) fish speed < 10 mm/s for > 50 consecutive cycles (stuck) — EXCEPT exempt a fish the simulator has frozen (`QuickRescue.cs:427-452` pins the defending team's `Fishes[1]` at zero velocity each half; detect via position never changing since spawn, OR position outside the field `|X|>2250` or `|Z|>1500` — the frozen fish is pinned at `(-2115,0,1591)`, `Z>1500`, so treat out-of-bounds as an immediate frozen signal and skip pathfinding for that fish), (c) next waypoint invalid under current inflation, (d) assigned ball changed. Triggers live in a clearly-extensible `switch`/list. MUST NOT: recompute the path every cycle; advance past end of list without clamping `index`.
  Parallelization: Wave 3 | Blocked by: 2,3 | Blocks: 5,6
  References: `PathFinder.cs` (waypoint type), `Steering.cs` (`GetTCode`), `MapConstants.cs` (`GetVCode`, `Distance`); speed source `RoboFish.VelocityMmPs`.
  Acceptance criteria (agent-executable): compile; probe feeds a straight 2-waypoint path and asserts index advances and VCode/TCode are within legal ranges; a `ShouldUpdatePath` probe flips each of (a)-(d) and asserts true, and false when none fire.
  QA scenarios: happy (normal follow) + failure (stuck 51 cycles → update=true). Evidence `.omo/evidence/task-4-quickrescue-strategy.md`.
  Commit: N | feat(strategy): waypoint executor + conditional path update

- [x] 5. Implement ball assignment + push-target computation + full GetDecision orchestration
  What to do / Must NOT do: In `Strategy.cs`, per fish keep state (assigned ball, planned ball position, waypoints, index, stuck counter). Each `GetDecision`: (1) resolve target ball — greedy nearest unassigned ball; if its ball is already inside the rescue zone (X∈[1850,2250], Z∈[-500,500]) reassign to the other ball; (2) compute push point = `ballPos - normalize(zoneCenter - ballPos) * (78+58+10)` (≈146 mm behind the ball toward the zone, `zoneCenter=(2050,0)`); (3) if `ShouldUpdatePath` → `FindPath(fishPos, pushPoint, 100)`; if `FindPath` returns empty/unreachable → steer straight at the ball at low `VCode=3` (TCode from `GetTCode`) and let `ShouldUpdatePath` re-plan; (4) `FollowPath` → build `Decision[i]`; (5) PUSH phase — when the fish is within the 40 mm advance threshold of `pushPoint` AND is collinear behind the ball, stop path-following and drive straight through/past the ball toward the zone: target = `ballPos + normalize(zoneCenter - ballPos) * 300`, `VCode=14`, re-aim each cycle from the ball's live position; declare push complete when `ball.X >= 1850`; if the fish loses alignment (ball no longer between fish and zone within tolerance), re-run `FindPath` to a fresh push point. If both balls already in zone, output `VCode=0, TCode=7` (idle). MUST NOT: mirror the zone; defend; exceed array length 2; read team 1 fish for team 0 decisions.
  Parallelization: Wave 4 | Blocked by: 2,3,4 | Blocks: 6
  References: ball radius `Common/Environment.cs` `Ball.RadiusMm=58`; `Match/QuickRescue.cs:307-308` ball starts `(-1350,0,±1050)`; rescue zone `Match/QuickRescue.cs:504` & `:568` (`X>=1850 && X<=2250 && Z>=-500 && Z<=500`); fish starts `QuickRescue.cs:249-256`; team access `mission.TeamsRef[teamId].Fishes`, balls `mission.EnvRef.Balls` (exactly 2), `mission.CommonPara.FishCntPerTeam=2` (`QuickRescue.cs:91`). Frozen-fish: `QuickRescue.cs:427-452` freezes the **defending** team's `Fishes[1]` each half — first half `Teams[1].Fishes[1]` (`:427-439`), second half `Teams[0].Fishes[1]` (`:440-452`); the attacking/scoring team keeps **both** fish active, the defender keeps only `Fishes[0]`. The frozen fish is pinned at `(-2115,0,1591)` (outside the field, `Z>1500`). The strategy still emits 2 `Decision` but the frozen one is overridden — see Todo 4 stuck-trigger exemption.
  Acceptance criteria (agent-executable): compile; probe with a stubbed `Mission` verifies `GetDecision` returns length-2 `Decision[]`, VCode∈[0,14], TCode∈[0,15]; assignment probe: two balls, two fish → each fish assigned a distinct ball; ball-in-zone → reassigned; push-phase probe: with a fish at the push point behind the ball, `GetDecision` returns `VCode=14` with a target past the ball (push), not `VCode≈0` (idle).
  QA scenarios: happy (2 balls/2 fish, both navigable; fish behind ball enters push phase and drives forward) + failure (both balls in zone → idle). Evidence `.omo/evidence/task-5-quickrescue-strategy.md`.
  Commit: N | feat(strategy): ball assignment + push + GetDecision orchestration

- [x] 6. Build the DLL, verify loader compatibility, and run simulator smoke QA
  What to do / Must NOT do: `msbuild Strategy/StrategyQuickRescue/StrategyQuickRescue.csproj` (Debug, x86). Confirm output DLL lands where the simulator can load it. Confirm the class full name resolves to `URWPGSim2D.Strategy.Strategy` (reflect via the loader's `Create` path or `ildasm`/`monodis`). Launch `URWPGSim2D` with `StrategyQuickRescue.dll` selected for both teams, run one QuickRescue match, and capture a screenshot/log showing both balls entering the right zone (X≥1850) or scores incrementing. Tune the `GetTCode` turn scale (empirical — the compiled `Locomotion`/`DeflectionAngleOfJoint` mapping is not in source) and stuck/advance thresholds if the fish circle or stall. MUST NOT: edit simulator core; leave debug self-test code in the shipped `Strategy.cs`.
  Parallelization: Wave 5 | Blocked by: 1,2,3,4,5 | Blocks: —
  References: loader `StrategyLoader/TeamStrategyUiTypes.cs` (DLL selection + copy + `Create(strCachedStrategyFullName, "URWPGSim2D.Strategy.Strategy", null)`); simulator exe under `URWPGSim2D/bin/`.
  Acceptance criteria (agent-executable): `msbuild` returns 0 errors; DLL exists; type name resolves; simulator match shows ball(s) crossing X≥1850 into the zone (score ≥1) within the match time, OR evidence of correct behavior with a documented tuning note.
  QA scenarios: happy (both balls pushed in, score increments) + failure (fish stalls/circles → adjust `GetTCode` sign or advance threshold, re-run). Evidence `.omo/evidence/task-6-quickrescue-strategy.md` + screenshot.
  Commit: N | chore(strategy): build + simulator QA evidence

## Final verification wave
> Runs in parallel after ALL todos. ALL must APPROVE. Surface results and wait for the user's explicit okay before declaring complete.
- [ ] F1. Plan compliance audit
- [ ] F2. Code quality review
- [ ] F3. Real manual QA
- [ ] F4. Scope fidelity

## Commit strategy
- Workspace is **not a git repo** (confirmed in env). No commits will be created; all todos `Commit: N`. If the user initializes git later, use atomic per-todo commits matching the `chore|feat` summaries above.

## Success criteria
- `Strategy/StrategyQuickRescue` builds cleanly (net35, x86) with zero errors and no edits outside its own directory.
- Loader-compatible class `URWPGSim2D.Strategy.Strategy` implements `IStrategy`/`MarshalByRefObject`.
- All 8 user-specified functions present and behavior matches the stated formulas (verified by probes in todos 1–4).
- Simulator smoke run shows the 2 fish navigating around the 5 obstacles and pushing the 2 balls into the fixed right rescue zone (score increments).
- No reliance on the sample's soccer logic or the empty tuning table; speed uses the explicit `dist/5` formula.
