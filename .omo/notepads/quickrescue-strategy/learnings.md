# learnings.md

## Task 1 (2026-09-04) — scaffold + geometry primitives

- MSBuild lives at `D:\microsoft visual studio\MSBuild\Current\Bin\MSBuild.exe` (VS2019, on this
  machine NOT on PATH). net35 reference assemblies present via NuGet package
  `microsoft.netframework.referenceassemblies.net35` v1.0.3 — the csproj `FrameworkPathOverride`
  resolves as-is. Build: `msbuild Strategy/StrategyQuickRescue/StrategyQuickRescue.csproj /p:Configuration=Debug`.
- Sample `StrategyNew2V2.csproj` literally outputs to project-local `bin\Debug\`, NOT
  `..\..\URWPGSim2D\bin\`. Task verification required the DLL in `URWPGSim2D/bin/`, so
  StrategyQuickRescue's OutputPath (Debug+Release) is `..\..\URWPGSim2D\bin\`. Everything else
  (references, HintPaths, FrameworkPathOverride, PlatformTarget=x86, RootNamespace) is byte-identical
  in intent to the sample.
- Loader contract confirmed by probe: full type name must be exactly `URWPGSim2D.Strategy.Strategy`;
  `Strategy : MarshalByRefObject, IStrategy`; `InitializeLifetimeService()` must return null;
  IStrategy = `GetTeamName()` + `GetDecision(Mission, int)` (StrategyLoader/StrategyLoader.cs:23).
- `Decision` struct (Common/RoboFish.cs:1439): `VCode` 0..15 (0 slowest), `TCode` 0..15 (7 straight).
  `FishBodyRadiusMm` = sqrt(BodyLength^2+BodyWidth^2)/2 (RoboFish.cs:137) — useful as inflation
  baseline for later waves.
- `Common/config.xml` default field is 3000x2000, but QuickRescue mission bounds are ±2250/±1500
  with 5 fixed obstacle AABBs (obs1 center pillar X[-50,50]xZ[-550,550]; obs2/3 bottom
  X±[500,600]xZ[400,1500]; obs4/5 top X±[500,600]xZ[-1500,-400]) — hardcoded in MapConstants.
- net35 + old-style csproj: keep code C# 3-compatible. No ValueTuple — FollowPath returns
  (tcode, vcode) via out params. All geometry lives in `MapConstants` (Point2D, ObstacleAabb,
  Distance, GetVCode=(int)(d/5) clamped [0,14], IsPointValid=shrunk field AND not-in-inflated-AABB).
- Wave-1 stub semantics (so later waves know the baseline): FindPath->false+empty list,
  ShouldUpdatePath->true, SegmentAngle->0, FormatAngle->identity, GetTCode->7,
  FollowPath->(7,0). GetDecision stub holds position (V=0,T=7) for every fish, sized by
  `mission.CommonPara.FishCntPerTeam`.
- Probe method that worked: compile a temp console with framework csc
  (`C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /platform:x86 /r:<built dll> /r:Common /r:StrategyLoader`),
  copy exe into `URWPGSim2D/bin`, run there (assembly resolution), delete exe after. 23/23 PASS.
- Project is intentionally NOT in `Strategy/Strategy.sln`; build the csproj directly.

## Task 2 (2026-09-04) — BFS FindPath + inflation + compression

- `PathFinder.FindPath` implemented: 40mm grid, cell center = `LeftMm+(i+0.5)*40 / TopMm+(j+0.5)*40`,
  Nx=112/Nz=75 (floor; slim right/bottom strip cell-less). Walkable iff `IsPointValid(center, inflation)`.
  4-connected BFS (order -X,+X,-Z,+Z) + parent-chain reconstruct + collinear compression (cross eps 1e-9,
  tested against last KEPT point). Waypoints = [original start, turn-point centers..., original target].
- Test hook for "compressed < raw": overload `FindPath(..., out waypoints, out rawCellCount)`; the
  required 4-arg signature delegates to it. rawCellCount = BFS cell count, 0 on failure.
- Snap edge case: a VALID point's nearest cell center can still be invalid (point <20mm outside an
  inflated box, center inside). Handled by expanding Chebyshev ring scan (fixed dz/dx order, first
  walkable wins) — deterministic. Never triggered by the probe lanes but costs nothing.
- Geometry fact that shaped verification: NO horizontal lane is obstacle-free under 100mm inflation
  (|z|<=650 → obs1 x-range; |z|>650 → obs2-5 z-ranges at x=±[400,700]). The plan's suggested z=1200
  lane crosses inflated obs2/obs3 (Z[300,1600]) — probe confirmed the hit and substituted vertical
  lane x=300 (misses obs1 X[-150,150] and obs2-5 X ±[400,700]): BFS length 2820 vs 2800 straight.
- Interior axis-aligned segments between valid centers are provably clear of inflated AABBs
  (boxes >=300mm wide vs 40mm center spacing → any overlap would swallow a raw center). Only the
  <=28mm end stubs (original point → snapped center) are non-axis-aligned; corner-touch is a
  theoretical edge case near inflated corners, documented in evidence, not hit in probes.
- Concurrent-agent isolation protocol works: `/p:BaseIntermediateOutputPath=<temp>\obj\ /p:OutputPath=<temp>\bin\`
  keeps obj/ and URWPGSim2D/bin/ untouched; probe = copy URWPGSim2D/bin/*.dll to temp probe dir,
  overwrite StrategyQuickRescue.dll with the fresh build, csc.exe compile + run in-place. 12/12 PASS.
- A1 route check: (-2000,0)->(2000,0)@100 yields 8 waypoints detouring BOTH obs1 (z=-680 lane) and
  obs4 (up to z=-280 at x=370) — true BFS optimum under inflation, 135 raw cells compressed to 8.

## Task 3 (2026-09-04) — steering math (SegmentAngle/FormatAngle/GetTCode)

- Concurrent-build isolation works: `/p:BaseIntermediateOutputPath=<tmp>\obj\\` +
  `/p:OutputPath=<tmp>\bin\\` (trailing backslashes) keeps a parallel agent's csproj obj/ and
  `URWPGSim2D/bin/` untouched. Probe = copy `URWPGSim2D/bin/*.dll` to a temp probe dir,
  overwrite `StrategyQuickRescue.dll` with the isolated build, csc+run there. 21/21 PASS.
- Reference `FormatAngle` loop (`while(a>PI) a-=2PI; while(a<=-PI) a+=2PI;`) is float-safe for
  the probe boundaries: 100π→-7.1e-15, 101π→π+2.5e-14, ±π/±2π/±3π exact (verified in
  PowerShell doubles before compiling). Cosmetic only: ~12345π can land at -π+3.6e-10
  (same angle as +π, just below the canonical side) — no contract exercises that range.
- `GetTCode` sweep structure: on (-π, π) the output is a unit-step monotonic staircase
  0→15; the ONLY adjacent jump is at exactly delta=-π where `FormatAngle(-π)=+π` gives 15
  while -π+ε gives 0. NOT a glitch — full-left(0) ≡ full-right(15) at 180°; clamp absorbs
  the 7±8 round overshoot. Probe 8 should therefore assert range on [-π,π] + unit steps on
  the open interval, not "no 15↔0 anywhere".
- `Math.Round(delta*8/π, MidpointRounding.AwayFromZero)`: .NET's default is ToEven — the
  AwayFromZero flag is REQUIRED to match the spec table (e.g. exact half steps π/16·k).
- TCode sign (positive delta → left vs right) intentionally untuned; documented in the
  XML comment on GetTCode. Todo 6 flips empirically if needed — the only spot is the
  `7 + ...` expression in `Steering.GetTCode`.
- Evidence: `.omo/evidence/task-3-quickrescue-strategy.md`.

## Task 4 (2026-09-04) — FollowPath + ShouldUpdatePath triggers

- Integration contract for Todo 5 (exact signatures, both in `PathExecutor.cs`):
  `FollowPath(Point2D fishPos, double headingRad, List<Point2D> waypoints, ref int waypointIndex,
  Point2D fallbackTarget, out int tcode, out int vcode)` and
  `ShouldUpdatePath(PathUpdateState state)`. `PathUpdateState` is a public sealed class of
  plain fields (C# 3: no auto-prop initializers) — caller fills ALL fields each cycle.
- The old `PathFinder.ShouldUpdatePath` stub is DELETED from PathFinder.cs; triggers now
  live only in PathExecutor. PathFinder.cs contains pathfinding only.
- SPEC CONFLICT in the original task packet was a PROBE BUG, not a plan bug (fixed on
  orchestrator review): probe assertion 1 wrongly expected index to stay 0 with the fish
  exactly on wp[0]. Corrected semantics = the plan formula verbatim: advance
  `while (idx < Count-1 && distToCurrent <= 40)` (INCLUSIVE of distance 0 — FindPath sets
  waypoints[0] = the fish's compute-time position, so a `> 0` guard would steer at the
  degenerate atan2(0,0)=0 self-angle on every fresh path: wrong-way lurch, locked against
  by probe 10: path heading -X, fish facing +X -> tcode 15 not 7), and
  `vcode = GetVCode(distToCurrent)` from the CURRENT waypoint after the advance loop
  (slows near EACH waypoint for inertial turn accuracy — a final-waypoint vcode would run
  14 through every turn point).
- Thresholds are named constants on `PathExecutor`: WaypointAdvanceMm=40, TargetMovedMm=60,
  StuckCyclesLimit=50 — Todo 6 tunes there.
- Frozen-fish exemption is checked BEFORE the no-path trigger inside `ShouldUpdatePath`
  (|X|>2250 or |Z|>1500 → false even with empty waypoints and StuckCycles=999).
- StuckCycles contract (documented on the field): caller increments when fish speed < 10 mm/s,
  resets to 0 otherwise; ShouldUpdatePath only compares `> 50`.
- Trigger (c) guards `WaypointIndex` in range before indexing — a stale index after a replan
  must not throw; FollowPath also clamps the ref index before any use (probe 4).
- Probe protocol unchanged from Task 1 (csc v4.0 /platform:x86, run exe inside
  URWPGSim2D/bin, delete after): 20/20 PASS, exit 0.

## Task 5 (2026-09-04) — full GetDecision orchestration

- Mission graph is fully constructible from the real types (`Mission()`,
  `Team<RoboFish>()`, `RoboFish()`, `Ball()`, `MissionCommonPara` all public
  parameterless ctors; fields public) — no plain-data snapshot adapter needed.
  `RoboFish()` prints a harmless `从配置文件读取参数出错` line when SysConfig can't
  find config.xml; same fallback exists in the real server.
- NEW probe gotcha: constructing RoboFish/Mission loads the v2.0 mixed-mode XNA
  assembly → CLR4 FileLoadException. Fix = probe-local `probe.exe.config` with
  `useLegacyV2RuntimeActivationPolicy="true"` (+supportedRuntime v4.0). Earlier waves
  never constructed Common objects so never hit this. Delete the config with the exe.
- `RoboFish.VelocityMmPs` is a SCALAR float speed, not a vector — `Math.Abs` gives the
  FishSpeedMmPs for PathUpdateState/StuckCycles.
- White-box state checks WITHOUT polluting Strategy.cs: reflection over private fields
  (`assignedBall`, `plannedPush`) from the probe. Cleaner than inferring assignment from
  TCode geometry.
- ASSERTION DESIGN lesson: do not assert on first-waypoint steering direction — BFS
  L-paths can leg +X first even when the target is deep −Z (observed: reassigned fish0,
  t=7/v=14, plannedPush=(-1489.5,-1093.1)). Assert on the planned-target state instead.
- Push-aim geometry: fish at the exact push point behind the LOWER ball with heading +X
  yields tcode 8, not 7 — ball→zoneCenter is ≈17.2° off +X for ball (-1350,-1050).
  Tolerance |tcode-7|≤1 for "tcode≈7".
- Orchestration shape that worked: UpdateAssignments hoisted ABOVE the per-fish loop
  (greedy needs cross-fish view); `Assign()` centralizes change-flag + push-drop; zone
  reassignment deliberately lets both fish stack on the one remaining ball; misalignment
  push-exit nulls waypoints → no-path trigger replans same cycle.
- Probe 8 recipe for a deterministic FindPath failure: ball0=(290,0) → push point
  (144,0) inside inflated obs1 (X[-150,150]×Z[-650,650]) → vcode-3 fallback. (300,0)
  does NOT work: push point (154,0) is 4mm outside the inflated box.
- Evidence: `.omo/evidence/task-5-quickrescue-strategy.md`.

## Todo 6 QA (2026-09-04) — TCode turn-direction sign RESOLVED from source (no sim needed)

- Open question from Task 3: does positive delta in `GetTCode` mean LEFT or RIGHT of the fish?
  Task-3 evidence deliberately left it untuned; the later `Steering.cs` NOTE (lines 31-40,
  dated "Todo 6 QA") claimed verification against Core.dll's DataBasedOnExperiment + a "run 2"
  falsifying the minus variant — provenance was uncertain until now.
- CLOSING EVIDENCE = the organizer's OWN shipped helper `StrategyHelper/StrategyHelper.cs`
  (compiles against the same `DataBasedOnExperiment` tables):
  - Field frame: X right, Z down, Y 0. Angles = atan2(Z,X) in (-PI,PI] (its `GetAngleDegree`).
  - `deltaTheta = dirFishToDestPtRad - fish.BodyDirectionRad`, normalized to (-PI,PI].
    Source comment (lines 64-67): positive delta = target direction to the fish's RIGHT;
    negative = LEFT.
  - Turn selection (lines 116-141): deltaTheta<0 → LEFT-turn codes, and comment at 117-118:
    "左转档位对应的角速度值为负值" (left-turn codes = NEGATIVE angular velocity).
    deltaTheta>0 → RIGHT-turn codes > 7 via `while (code<14 && table[code]<targetAngularV) code++`
    (positive target → climbs above 7).
  - PoseToPose phase 2 (lines 216-271) drives `thetae = destDir - bodyDir` to 0 with that same
    sign mapping ⇒ positive angular velocity INCREASES BodyDirectionRad, negative decreases.
- CONCLUSION: `GetTCode = 7 + round(delta*8/π)` (delta>0 → code>7 → positive angvel → right turn)
  EXACTLY matches the official convention. PLUS sign is correct; no flip at the call site or
  here. If the live sim ever shows mirror-flipped turning, the bug is NOT the code sign.
- Ledger entry `6b-sign-convention-resolved → confirmed` (2026-09-04T08:26+08:00). Live 6B is
  now purely a final smoke run (DLL loads in real DSS sim, match wiring, right-zone scoring),
  NOT a tuning run.


