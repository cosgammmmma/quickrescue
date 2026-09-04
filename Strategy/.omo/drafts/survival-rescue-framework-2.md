---
slug: survival-rescue-framework-2
status: drafting
intent: clear
pending-action: write .omo/plans/survival-rescue-framework-2.md
approach: two independent mission DLL frameworks, one for survival challenge and one for extreme rescue, with shared pure movement helpers only where safe
---

# Draft: survival-rescue-framework-2

## Components (topology ledger)
<!-- Lock the SHAPE before depth. One row per top-level component that can succeed or fail independently. -->
<!-- id | outcome (one line) | status: active|deferred | evidence path -->
| C1 | Survival challenge strategy package outputs a mission-specific DLL with `IStrategy` compatibility and obstacle-safe movement hooks | active | `StrategyFightForLive/StrategyFightForLive.cs:10-113`, `StrategyFightForLive/StrategyFightForLive.csproj:9-13` |
| C2 | Extreme rescue strategy package outputs a separate mission-specific DLL with rescue/dribble/ball-lock behavior | active | `StrategyNew2V2/Strategy.cs:15-258`, `StrategyNew2V2/GlobalVar.cs:31-36`, `StrategyNew2V2/BaseAction.cs:1-107` |
| C3 | Build and solution wiring produce deterministic x86 Release DLL artifacts without loader ambiguity | active | `Strategy.sln:6-17`, `StrategyFightForLive/StrategyFightForLive.csproj:36-55`, `StrategyBallMoving/StrategyBallMoving.csproj:36-55`, `StrategyNew2V2/StrategyNew2V2.csproj` |
| C4 | Self-battle validation proves the generated DLLs can load and run against themselves/baselines with measurable obstacle and dribble outcomes | active | strategy API notes in `StrategyFightForLive/StrategyFightForLive.cs:41-111` and `StrategyBallMoving/StrategyBallMoving.cs:41-111` |

## Open assumptions (announced defaults)
<!-- Record any default you adopt instead of asking, so the user can veto it at the gate. -->
<!-- assumption | adopted default | rationale | reversible? -->
| output shape | two independent DLLs, one per mission | repo already uses per-mission strategy assemblies and the loader expects a `URWPGSim2D.Strategy.Strategy` implementation per DLL | yes |
| shared code | only pure utility/navigation helpers may be shared; mission tactical state stays separate | prevents survival and rescue state machines from corrupting each other | yes |
| test strategy | tests-after + build validation + simulator/self-battle QA | existing legacy .NET project lacks test harness; highest-value verification is compile + loader/run evidence | yes |
| PDF rule extraction | worker must manually inspect the local PDFs or any text extracted from them before coding mission constants | current session's PDF media extractor reported unsupported PDF input | yes |

## Findings (cited - path:lines)
- `StrategyFightForLive/StrategyFightForLive.cs:10-113` is a valid shell: namespace `URWPGSim2D.Strategy`, class `Strategy : MarshalByRefObject, IStrategy`, `GetTeamName()`, `GetDecision()` and reserved lifetime service code.
- `StrategyBallMoving/StrategyBallMoving.cs:10-113` is an equivalent strategy shell for ball transport and can be used as fallback reference for a simple mission-specific DLL layout.
- `StrategyNew2V2/Strategy.cs:15-258` contains richer tactical flow, mission state access, ball scoring/status keys, target selection, lock-carrying-ball calls, and dribble/shield calls.
- `StrategyNew2V2/GlobalVar.cs:31-36` defines shared state for `BaseAction`, balls, boundary/dribble flags, and carried ball IDs.
- `StrategyNew2V2/BaseAction.cs:20-107` contains safety-aware movement primitives: safe approach speed and bang-bang `FastMoveTo` speed/turn control.
- `StrategyFightForLive/StrategyFightForLive.csproj:9-13` and `StrategyBallMoving/StrategyBallMoving.csproj:9-13` are library projects targeting .NET Framework v3.5 with assembly names matching their missions.
- `StrategyFightForLive/StrategyFightForLive.csproj:36-55` and `StrategyBallMoving/StrategyBallMoving.csproj:36-55` define Debug/Release output paths and x86 Debug platform targeting; Release platform target must be verified/normalized by the worker.
- `Strategy.sln:6-17` currently builds only `StrategyNew2V2`, so survival/rescue projects need explicit solution/build inclusion or direct project-build commands.
- Metis review flagged package naming, mission separation, obstacle hierarchy, dribble lock state machine, and deterministic self-battle thresholds as mandatory plan constraints.

## Decisions (with rationale)
- Build two DLL packages, not one runtime-branching DLL. Rationale: each DLL can expose the same `URWPGSim2D.Strategy.Strategy` type without loader ambiguity.
- Treat survival and extreme rescue as separate tactical policies. Rationale: objectives and map interactions differ; sharing mutable global state would be risky.
- Reuse `BaseAction`-style motion ideas for safe approach and turning, but require the worker to copy/adapt only compatible pure helper logic into each mission package. Rationale: existing helper code is mission-coupled through `GlobalVar`.
- Make obstacle avoidance and dribble/ball-lock behavior explicit acceptance criteria rather than informal comments. Rationale: the user named these as high-risk problems.
- Verification will be tests-after: compile checks, artifact checks, static review of command saturation/ranges, and simulator/self-battle evidence where the local simulator supports it.

## Scope IN
- Inspect the two local PDFs in `比赛规则/` and extract the survival challenge and extreme rescue mission constants before coding.
- Complete strategy framework code for survival challenge and extreme rescue.
- Ensure generated DLLs are x86/.NET-compatible with `IStrategy` loader expectations.
- Add safe movement/avoidance primitives, dribble/ball-lock state transitions, and stuck/oscillation recovery.
- Produce Release DLLs and record exact build commands/artifact paths.
- Run own-DLL self-battle or closest simulator-supported equivalent and record evidence.

## Scope OUT (Must NOT have)
- No simulator/core engine/UI changes.
- No extra missions beyond survival challenge and extreme rescue.
- No single shared mutable global state across the two mission DLLs.
- No external dependencies or package upgrades unless already required by the repo.
- No hard-coded paths outside the existing repo and simulator bin/reference layout.
- No claiming PDF-rule compliance without citing the exact local rule section used.

## Open questions
- none; user approved the two-DLL approach.

## Approval gate
status: approved
<!-- When exploration is exhausted and unknowns are answered, set status: awaiting-approval. -->
<!-- That durable record is the loop guard: on a later turn read it and resume at the gate instead of re-running exploration. -->

## Review receipts
- Metis gap analysis: `ses_fd738b831ffeTNNY38465WwOHP`; findings folded into plan constraints.
