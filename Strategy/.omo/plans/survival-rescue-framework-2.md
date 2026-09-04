# survival-rescue-framework-2 - Work Plan

## TL;DR (For humans)
**What you'll get:** 两个互相独立的比赛策略 DLL：一个用于生存挑战，一个用于极限救援；两者都包含可加载的策略入口、安全运动框架、避障策略、运球/锁球控制，以及自对打/自验证证据。

**Why this approach:** 现有工程天然按“一个比赛项目一个策略 DLL”组织，而且每个 DLL 都暴露同名策略入口；拆成两个 DLL 可以避免加载器混淆，也能让生存和救援的状态机互不污染。

**What it will NOT do:** 不改仿真器核心、不改 UI、不新增无关比赛项目、不把两个任务塞进一个共享全局状态的大框架。

**Effort:** Medium
**Risk:** Medium - 主要风险来自旧版 .NET/x86 构建环境、PDF 规则需要人工/工具提取确认，以及仿真自对打可能依赖本机模拟器配置。
**Decisions to sanity-check:** 默认交付两个独立 DLL；只共享纯工具逻辑；必须先引用本地规则文件再写任务常量。

Your next move: 使用 `/start-work` 或等价 worker 会话执行本计划；如果你想先做更严格审查，也可以要求 high-accuracy review。Full execution detail follows below.

---

> TL;DR (machine): Medium effort, medium risk; create two x86 .NET strategy DLL frameworks with rule-grounded survival/rescue logic, obstacle avoidance, dribble/ball-lock state machines, build artifacts, and self-battle evidence.

## Scope
### Must have
- Read and cite the local competition PDFs under `比赛规则/` before implementing mission constants or scoring assumptions.
- Complete `IStrategy`-compatible strategy frameworks for survival challenge and extreme rescue.
- Produce two independent DLL artifacts, one per mission, with unambiguous assembly/project/output mapping.
- Preserve loader-required shape: `URWPGSim2D.Strategy.Strategy : MarshalByRefObject, IStrategy`, `InitializeLifetimeService()`, `GetTeamName()`, `GetDecision(Mission mission, int teamId)`.
- Add explicit obstacle avoidance hierarchy for both missions: direct-path safety check, clearance margin, fallback waypoint, stuck recovery.
- Add explicit dribble/ball-lock behavior for rescue/ball-control paths: acquire, maintain, release, anti-jitter, and reselect transitions.
- Build Release artifacts and record exact commands and output DLL paths.
- Run own-DLL self-battle or closest simulator-supported equivalent and record evidence for loading, survival/rescue behavior, obstacle avoidance, and ball handling.
### Must NOT have (guardrails, anti-slop, scope boundaries)
- Must not modify simulator/core engine/UI code.
- Must not collapse both missions into one runtime-branching DLL unless the user explicitly changes scope.
- Must not share mutable tactical state between survival and rescue strategies.
- Must not add external dependencies, NuGet packages, or framework upgrades unless the build is impossible without them and the reason is recorded.
- Must not claim compliance with rules that were not extracted/cited from local rule files.
- Must not leave `VCode`/`TCode` outside valid simulator ranges or produce null/uninitialized `Decision` entries.
- Must not make self-battle verification depend on a human watching the simulator; the worker must capture logs/screenshots/results where available.

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Test decision: tests-after + legacy .NET build commands + static range checks + simulator/self-battle evidence where available.
- Evidence: `.omo/evidence/task-<N>-survival-rescue-framework-2.<ext>` plus build logs and simulator run notes/screenshots under `.omo/evidence/`.
- Mandatory checks:
  - Project build succeeds for both mission DLLs in Release/x86-compatible mode.
  - Expected DLL files exist after build and have mission-specific names.
  - Strategy classes keep the loader-required API shape.
  - Every fish decision is initialized each cycle and `VCode`/`TCode` remain in supported ranges.
  - Avoidance logic has a measurable clearance margin and a blocked-route fallback.
  - Rescue dribble logic has acquire/maintain/release states and avoids rapid target switching.
  - Self-battle or closest runnable simulation loads the generated DLL(s) without crashing.

## Execution strategy
### Parallel execution waves
> Target 5-8 todos per wave. Fewer than 3 (except the final) means you under-split.

- Wave 1: rule extraction, build mapping, architecture isolation, and shared helper boundaries.
- Wave 2: survival framework, rescue framework, navigation/avoidance, and dribble/lock logic.
- Wave 3: solution/build packaging, self-battle harness instructions, and final verification.

### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 | none | 4,5,6,9 | 2,3 |
| 2 | none | 7,8,9 | 1,3 |
| 3 | none | 4,5,6,7,8 | 1,2 |
| 4 | 1,3 | 7,9 | 5,6 |
| 5 | 1,3 | 8,9 | 4,6 |
| 6 | 1,3 | 5,7,8,9 | 4 |
| 7 | 2,4,6 | 9,10 | 8 |
| 8 | 2,5,6 | 9,10 | 7 |
| 9 | 1,4,5,6,7,8 | 10 | none |
| 10 | 7,8,9 | final verification | none |

## Todos
> Implementation + Test = ONE todo. Never separate.
<!-- APPEND TASK BATCHES BELOW THIS LINE WITH edit/apply_patch - never rewrite the headers above. -->
- [x] 1. `比赛规则/`: Extract survival and extreme-rescue rule constants before coding - expect cited mission constraints
  What to do / Must NOT do: Extract the relevant rules from `比赛规则/第十七届RST大赛-智慧海洋赛道-水中机器人仿真组比赛规则及评审(裁判)说明.pdf` and `比赛规则/URWPGSim2D开发人员手册.pdf`; record mission objective, scoring, field/obstacle layout, allowed/disallowed behavior, robot/ball counts, time limits, and any DLL submission constraints. If PDF tooling fails, use an available local PDF reader/OCR/manual text extraction and record the method. Must not invent mission constants from memory.
  Parallelization: Wave 1 | Blocked by: none | Blocks: 4,5,6,9
  References (executor has NO interview context - be exhaustive): `比赛规则/第十七届RST大赛-智慧海洋赛道-水中机器人仿真组比赛规则及评审(裁判)说明.pdf`; `比赛规则/URWPGSim2D开发人员手册.pdf`; strategy API comments in `StrategyFightForLive/StrategyFightForLive.cs:41-111`.
  Acceptance criteria (agent-executable): `.omo/evidence/task-1-survival-rescue-framework-2.md` exists and contains two sections named `生存挑战规则摘录` and `极限救援规则摘录`, each with page/section references or extraction notes; no strategy code constants are changed before this evidence exists.
  QA scenarios (name the exact tool + invocation): happy: run `dir 比赛规则` and attach extraction notes/screenshots/text snippets to `.omo/evidence/task-1-survival-rescue-framework-2.md`; failure: if PDF extraction fails, record the exact tool error and the fallback extraction method in the same evidence file.
  Commit: N | planning/evidence only until implementation begins

- [x] 2. `*.csproj` and `Strategy.sln`: Lock two-DLL build/package mapping - expect unambiguous Release artifacts
  What to do / Must NOT do: Decide and document the exact project/assembly/output mapping for survival and extreme rescue. Preferred mapping: survival uses `StrategyFightForLive/StrategyFightForLive.csproj` -> `StrategyFightForLive.dll`; rescue uses a dedicated rescue project derived from current rescue-capable code or an explicitly renamed/repurposed existing project -> `StrategyExtremeRescue.dll` or another clear mission name. Update solution/build wiring only as needed. Must not overwrite the existing `StrategyNew2V2` artifact without a recorded reason.
  Parallelization: Wave 1 | Blocked by: none | Blocks: 7,8,9,10
  References (executor has NO interview context - be exhaustive): `Strategy.sln:6-17`; `StrategyFightForLive/StrategyFightForLive.csproj:9-13,36-55,56-98`; `StrategyBallMoving/StrategyBallMoving.csproj:9-13,36-55,56-98`; `StrategyNew2V2/StrategNew2V2.csproj:9-11,13-31,33-47`; grep result showing target framework/output/platform fields across projects.
  Acceptance criteria (agent-executable): `.omo/evidence/task-2-survival-rescue-framework-2.md` lists final DLL names, owning project files, output paths, target framework, platform target, and loader mission mapping; `msbuild`/Visual Studio build command to produce each DLL is included.
  QA scenarios (name the exact tool + invocation): happy: run `findstr /n "AssemblyName TargetFrameworkVersion OutputPath PlatformTarget" StrategyFightForLive\StrategyFightForLive.csproj StrategyBallMoving\StrategyBallMoving.csproj StrategyNew2V2\*.csproj` and save output; failure: intentionally check for duplicate final DLL names and record `none` or the collision found.
  Commit: Y | build(package): declare survival and rescue DLL artifact mapping

- [x] 3. Strategy API boundaries: Extract pure helper candidates and forbid shared mutable state - expect mission isolation contract
  What to do / Must NOT do: Review `StrategyNew2V2` helper code and identify which movement/navigation routines can be copied/adapted as pure per-DLL helpers. Keep mission tactical state local to each strategy instance or per-mission static scope. Must not share `GlobalVar` mutable state across both mission DLLs.
  Parallelization: Wave 1 | Blocked by: none | Blocks: 4,5,6,7,8
  References (executor has NO interview context - be exhaustive): `StrategyNew2V2/GlobalVar.cs:31-36,58-65,121-140,152-161,173-181`; `StrategyNew2V2/BaseAction.cs:20-107,114-145,247-260`; `StrategyNew2V2/Strategy.cs:21-35,122-209`.
  Acceptance criteria (agent-executable): `.omo/evidence/task-3-survival-rescue-framework-2.md` lists each reused helper, whether it is pure or stateful, and where it will live in each mission project; code review confirms no cross-project shared mutable static state is introduced.
  QA scenarios (name the exact tool + invocation): happy: run `findstr /s /n "static .*GlobalVar\|Bringing_BallID\|lockBallId\|filterBall" StrategyFightForLive\*.cs StrategyNew2V2\*.cs StrategyBallMoving\*.cs` and save baseline/after output; failure: if a shared static is introduced, evidence marks it as a blocker until moved or justified.
  Commit: Y | refactor(strategy): isolate reusable movement helpers from mission state

- [x] 4. `StrategyFightForLive`: Build survival challenge strategy framework - expect safe mission loop and initialized decisions
  What to do / Must NOT do: Implement the survival-challenge framework inside the survival project after rule extraction. Keep loader-required class shape and reserved lifetime method. Add cycle initialization, mission state snapshot, safe target selection, obstacle-aware movement, boundary protection, stuck recovery, and conservative fallback behavior. Must not leave the existing empty `GetDecision()` body or return stale decisions.
  Parallelization: Wave 2 | Blocked by: 1,3 | Blocks: 7,9
  References (executor has NO interview context - be exhaustive): `StrategyFightForLive/StrategyFightForLive.cs:10-113`; strategy comment contract at `StrategyFightForLive/StrategyFightForLive.cs:60-104`; `StrategyNew2V2/BaseAction.cs:20-107` for safe approach and turn/speed quantization ideas; local rule extraction evidence from task 1.
  Acceptance criteria (agent-executable): Build succeeds; static inspection shows every fish index from `0..mission.CommonPara.FishCntPerTeam-1` receives a `Decision` each cycle; all assignments to `VCode` and `TCode` are clamped or selected within simulator-supported ranges; `.omo/evidence/task-4-survival-rescue-framework-2.md` explains the survival state machine.
  QA scenarios (name the exact tool + invocation): happy: run project build command from task 2 for survival and save `bin\Release\StrategyFightForLive.dll` existence plus build log; failure: run a static search for unclamped `VCode =`/`TCode =` assignments and record any unsafe assignment until fixed.
  Commit: Y | feat(survival): add obstacle-safe survival challenge framework

- [x] 5. Rescue strategy project: Build extreme rescue framework with dribble/lock state machine - expect stable ball handling
  What to do / Must NOT do: Implement or adapt the rescue strategy in the selected rescue project. Add rescue mission phases from the rules, robot role assignment, target/ball selection, dribble acquisition, lock maintenance, release/reselect conditions, anti-jitter timers, and stuck/blocked recovery. Must not let target selection switch every cycle while a robot is already carrying/dribbling a ball.
  Parallelization: Wave 2 | Blocked by: 1,3 | Blocks: 8,9
  References (executor has NO interview context - be exhaustive): `StrategyNew2V2/Strategy.cs:122-209` for ball ID selection and `LockCarryingBall`; `StrategyNew2V2/Strategy.cs:221-258` for dribble/shield call pattern; `StrategyNew2V2/GlobalVar.cs:34-36,100-111,152-161`; `StrategyNew2V2/BaseAction.cs:8-18` for filter/stalemate/lock fields; local rule extraction evidence from task 1.
  Acceptance criteria (agent-executable): Build succeeds; `.omo/evidence/task-5-survival-rescue-framework-2.md` contains a state transition table for `Acquire -> Carry/Dribble -> Deliver/Rescue -> Release/Recover`; static inspection finds explicit lock timeout/release conditions and no per-cycle unconditional target reselection while locked.
  QA scenarios (name the exact tool + invocation): happy: build rescue project and save DLL existence plus state-machine evidence; failure: search for target-selection calls that ignore current lock state and record blockers until guarded.
  Commit: Y | feat(rescue): add extreme rescue dribble and lock framework

- [x] 6. Navigation/avoidance helpers: Add clearance-aware movement used by both missions - expect no direct obstacle-driving path
  What to do / Must NOT do: Add or adapt helper routines for distance/angle, direct path obstruction, nearest safe waypoint, obstacle clearance, boundary margin, and speed throttling near obstacles/balls/goals. Use `mission.EnvRef.ObstaclesRect`, `mission.EnvRef.ObstaclesRound`, field bounds, and robot heading. Must not only steer directly at the target when an obstacle intersects the path.
  Parallelization: Wave 2 | Blocked by: 1,3 | Blocks: 4,5,7,8,9
  References (executor has NO interview context - be exhaustive): environment info comments in `StrategyFightForLive/StrategyFightForLive.cs:80-97` and `StrategyBallMoving/StrategyBallMoving.cs:80-97`; `StrategyNew2V2/BaseAction.cs:20-107,126-145`; `StrategyNew2V2/GlobalVar.cs:173-181` speed/turn tables.
  Acceptance criteria (agent-executable): `.omo/evidence/task-6-survival-rescue-framework-2.md` documents clearance margin, fallback waypoint policy, stuck threshold, and boundary margin; static inspection confirms both mission strategies call the avoidance helper before high-speed movement toward a target.
  QA scenarios (name the exact tool + invocation): happy: run `findstr /s /n "ObstaclesRect\|ObstaclesRound\|Avoid\|Clearance\|Safe" StrategyFightForLive\*.cs <rescue-project>\*.cs` and attach output; failure: record a blocked direct path scenario and prove helper selects stop/turn/waypoint rather than full-speed direct drive.
  Commit: Y | feat(nav): add obstacle clearance and blocked-route recovery

- [x] 7. Build survival DLL: Normalize project/solution inclusion and produce Release artifact - expect `StrategyFightForLive.dll`
  What to do / Must NOT do: Ensure survival project builds via a documented command, either by adding it to the solution or invoking its `.csproj` directly. Normalize Release platform target if needed so the loader can run x86-compatible output. Must not break existing `StrategyNew2V2` solution build.
  Parallelization: Wave 3 | Blocked by: 2,4,6 | Blocks: 9,10
  References (executor has NO interview context - be exhaustive): `StrategyFightForLive/StrategyFightForLive.csproj:9-13,36-55,56-98,116`; `Strategy.sln:6-17`.
  Acceptance criteria (agent-executable): The documented build command exits 0; expected survival DLL exists in the chosen Release output folder; `.omo/evidence/task-7-survival-rescue-framework-2.log` contains the full build output.
  QA scenarios (name the exact tool + invocation): happy: run `msbuild StrategyFightForLive\StrategyFightForLive.csproj /p:Configuration=Release /p:Platform="AnyCPU"` or the final documented equivalent and save output; failure: delete/clean the output folder then rebuild to prove the DLL is generated from source, not stale.
  Commit: Y | build(survival): produce Release survival DLL

- [x] 8. Build rescue DLL: Normalize selected rescue project and produce Release artifact - expect mission-specific rescue DLL
  What to do / Must NOT do: Ensure the selected rescue project builds via a documented command and outputs the mission-specific rescue DLL name chosen in task 2. If deriving from `StrategyNew2V2`, preserve a backup path or clear rename plan. Must not silently reuse `wsy2v2.dll`/`StrategyNew2V2.dll` if the final artifact name in task 2 says otherwise.
  Parallelization: Wave 3 | Blocked by: 2,5,6 | Blocks: 9,10
  References (executor has NO interview context - be exhaustive): `StrategyNew2V2/StrategyNew2V2.csproj`; `StrategyNew2V2/StrategNew2V2.csproj:9-11,13-31,33-47`; `StrategyNew2V2/Strategy.cs:15-258`; task 2 mapping evidence.
  Acceptance criteria (agent-executable): The documented build command exits 0; expected rescue DLL exists in the chosen Release output folder; `.omo/evidence/task-8-survival-rescue-framework-2.log` contains the full build output and the final DLL path.
  QA scenarios (name the exact tool + invocation): happy: run final rescue build command from task 2 and save output; failure: clean output then rebuild and verify no stale differently named DLL is being mistaken for success.
  Commit: Y | build(rescue): produce Release extreme rescue DLL

- [~] 9. Self-battle/simulator validation: Run generated DLLs against themselves/baselines - expect load/run evidence and behavior thresholds
  What to do / Must NOT do: Use the local URWPGSim2D simulator workflow to load the generated DLLs and run self-battle or closest supported simulation: survival vs itself/baseline, rescue vs itself/baseline, and if supported survival-vs-rescue as a stress run. Record load success, crashes, obstacle collisions, stuck duration, ball possession stability, and score/objective progress. Must not rely only on build success.
  Parallelization: Wave 3 | Blocked by: 1,4,5,6,7,8 | Blocks: 10
  References (executor has NO interview context - be exhaustive): generated DLL paths from tasks 7 and 8; rule evidence from task 1; strategy API comments in `StrategyFightForLive/StrategyFightForLive.cs:41-111`; mission environment references in `StrategyBallMoving/StrategyBallMoving.cs:80-97`.
  Acceptance criteria (agent-executable): `.omo/evidence/task-9-survival-rescue-framework-2.md` records exact simulator executable/config used, DLL paths loaded, run duration/cycles, pass/fail thresholds, and observed results; no unhandled crash occurs during load/run.
  QA scenarios (name the exact tool + invocation): happy: run simulator with both generated DLLs or record exact GUI/CLI steps plus screenshots/logs under `.omo/evidence/`; failure: intentionally attempt to load a missing/wrong DLL once or document loader error behavior, then prove correct DLL path loads.
  Commit: N | evidence-only validation after build commits

- [x] 10. Final packaging manifest: Create handoff bundle record - expect reproducible submission checklist
  What to do / Must NOT do: Create a concise manifest under `.omo/evidence/` listing final source projects, DLL names, output paths, build commands, simulator validation evidence, and known limitations. Must not copy binary DLLs into `.omo/` unless explicitly useful; reference their real output locations.
  Parallelization: Wave 3 | Blocked by: 7,8,9 | Blocks: final verification
  References (executor has NO interview context - be exhaustive): task 2 mapping evidence; task 7/8 build logs; task 9 simulator evidence; `Strategy.sln:6-17`.
  Acceptance criteria (agent-executable): `.omo/evidence/task-10-survival-rescue-framework-2.md` exists and includes a reproducibility checklist from clean source to final DLLs; every DLL path in the manifest exists at verification time.
  QA scenarios (name the exact tool + invocation): happy: run `dir /s /b *StrategyFightForLive*.dll *Rescue*.dll *StrategyNew2V2*.dll` or final equivalent and save output; failure: if any manifest path is missing, mark packaging blocked and rebuild before final verification.
  Commit: Y | docs(release): add DLL build and validation manifest

## Final verification wave
> Runs in parallel after ALL todos. ALL must APPROVE. Surface results and wait for the user's explicit okay before declaring complete.
- [x] F1. Plan compliance audit: verify each todo has evidence, each Must Have is satisfied, and no Must NOT guardrail was violated.
- [x] F2. Code quality review: inspect strategy code for loader API compatibility, mission isolation, range-safe decisions, no unused broad framework slop, and no shared mutable cross-mission state.
- [x] F3. Real manual QA: run the simulator/self-battle workflow with generated DLLs and attach logs/screenshots/results, not just a textual claim.
- [x] F4. Scope fidelity: compare delivered DLL names, mission mapping, rule citations, and validation results against the user's original request.

## Commit strategy
- Keep commits atomic by concern: package mapping, helper isolation, survival framework, rescue framework, navigation/avoidance, build artifacts/manifest.
- Do not commit generated binaries unless the project convention requires binary delivery in-repo; if committed, use a dedicated release/artifact commit and note why.
- Evidence files under `.omo/evidence/` may be kept uncommitted unless the worker/session expects them for handoff; regardless, final response must cite their paths.

## Success criteria
- Two independent strategy DLLs are produced from source with documented build commands and exact output paths.
- Survival challenge strategy has a non-empty rule-grounded decision loop, initializes all decisions, avoids obstacles/bounds, and has stuck recovery.
- Extreme rescue strategy has a rule-grounded rescue loop, dribble/ball-lock state machine, anti-jitter target retention, and blocked-route recovery.
- The generated DLLs load in the local simulator or the closest available loader workflow without crashing.
- Self-battle/baseline validation evidence exists and specifically comments on obstacle avoidance, dribbling/ball handling, and any remaining limitations.
- No simulator/core/UI code or unrelated mission strategy is changed outside the approved scope.
