---
slug: quickrescue-strategy
status: drafting
intent: clear
review_required: true
pending-action: run high-accuracy (dual) review of .omo/plans/quickrescue-strategy.md
approach: Four-component plan — (1) geometry + navigation primitives, (2) BFS pathfinding over an obstacle+body-inflated grid, (3) waypoint-following executor with extensible retrigger, (4) full GetDecision orchestration compiled into a strategy DLL.
---

# Draft: quickrescue-strategy

## Components (topology ledger)
<!-- Lock the SHAPE before depth. One row per top-level component that can succeed or fail independently. -->
<!-- id | outcome (one line) | status: active|deferred | evidence path -->
1 | Geometry + navigation primitives (funcs 1,2,3,6,7): field/obstacle constants, point-validity, distance, vcode speed, segment-angle, legal-turn | active | Common/Environment.cs, Common/RoboFish.cs
2 | BFS shortest path over inflated grid (func 4): returns ordered waypoints incl. turn points | active | Match/QuickRescue.cs (obstacles/field)
3 | Path execution + extensible retrigger (funcs 5,8): fast-move through waypoints, recalc on trigger set | active | Common/RoboFish.cs (Decision)
4 | Strategy orchestration + build: GetDecision loop, 2-ball assignment, push-into-rescue-zone, csproj/DLL | active | StrategyLoader/StrategyLoader.cs, Strategy/StrategyNew2V2/*

## Open assumptions (announced defaults)
<!-- Record any default you adopt instead of asking, so the user can veto it at the gate. -->
<!-- assumption | adopted default | rationale | reversible? -->
Ball assignment | greedy: each fish takes nearest unassigned ball; reassign when a ball already inside rescue zone | 2 balls / 2 fish; simplest correct split | yes
Grid resolution + obstacle inflation | inflate rect obstacles by fish body radius (~78 mm) + margin; grid step ~40 mm | fish body 150x44 → FishBodyRadiusMm ≈ 78 (RoboFish.cs:137) | yes
Team handling | single symmetric strategy reading `mission.TeamsRef[teamId]`; rescue zone is fixed X∈[1850,2250], Z∈[-500,500]; NO mirroring (unlike sample's RightToLeft) | half-court exchange reverses TeamsRef; zone is constant | yes
vcode mapping | `vcode = dist(mm)/5` (integer quotient), cap at 14 | user-specified func 3 | n/a (given)

## Findings (cited - path:lines)
- Field 4500x3000 mm: QuickRescue.ResetField sets FieldLengthXMm=4500, FieldLengthZMm=3000; Environment.cs:819-822 derives LeftMm=-2250, RightMm=2250, TopMm=-1500, BottomMm=1500.
- 5 rect obstacles 100x1100 mm at (0,0), (550,950), (-550,950), (550,-950), (-550,-950): QuickRescue.cs.
- 2 balls start at (-1350,0,1050), (-1350,0,-1050): QuickRescue.cs.
- Rescue zone fixed X∈[1850,2250], Z∈[-500,500] (both half-court phases): QuickRescue.cs GoalHandler.
- 2 fish/team. Body 150x44; FishBodyRadiusMm = (int)sqrt(150²+44²)/2 ≈ 78 (RoboFish.cs:109-110,137); Ball RadiusMm=58.
- Decision: VCode 0-15 (speed), TCode 0-15 (7 = straight) — RoboFish.cs:1439-1451.
- IStrategy: `GetTeamName()` + `Decision[] GetDecision(Mission mission, int teamId)`; array length = FishCntPerTeam = 2 — StrategyLoader/StrategyLoader.cs.
- Sample StrategyNew2V2 uses 9-ball loop + `teamId!=0` mirroring — NOT reusable verbatim (QuickRescue = 2 balls, fixed zone).
- csproj template: net35, OutputType=Library, RootNamespace=URWPGSim2D.Strategy, AssemblyName=<name> — Strategy/StrategyNew2V2/*.csproj.
- `DataBasedOnExperiment` speed/turn table has no value in source → plan uses the user's explicit vcode formula instead.

## Decisions (with rationale)
- Test strategy: none — agent-executed QA only (user).
- Project placement: new standalone project `Strategy/StrategyQuickRescue` (user).
- BFS for shortest path (func 4) — user decision.
- Full runnable strategy (not just the 8 helpers) — user decision.
- Extensible path-update triggers (not every frame) — user decision.
- Single symmetric strategy, fixed rescue zone, no mirroring — adopted default.

## Scope IN
- 8 specified functions (map consts/validity, distance, fast-move vcode, BFS path w/ body size + waypoints, waypoint execution, segment angle, legal turn, path update).
- Full GetDecision orchestration: assign balls → navigate to ball → push ball into rescue zone → loop.
- New strategy project (csproj + Strategy.cs + helpers) compiling to a DLL loadable by the simulator.

## Scope OUT (Must NOT have)
- No edits to Match/, Common/, StrategyLoader/, or the simulator core.
- No 9-ball / 5v5 soccer logic from the sample.
- No reliance on the (unpopulated) DataBasedOnExperiment tuning table.
- No GUI/front-end changes.

## Open questions
- Test strategy (TDD / tests-after / none) — mandatory confirm. → RESOLVED: none (agent-executed QA only)
- Project placement/naming (new standalone project vs. modify sample). → RESOLVED: new project Strategy/StrategyQuickRescue

## Approval gate
status: approved
<!-- User approved 2026-09-03: "批准，ulw". Pending action: write .omo/plans/quickrescue-strategy.md. -->
<!-- That durable record is the loop guard: on a later turn read it and resume at the gate instead of re-running exploration. -->
