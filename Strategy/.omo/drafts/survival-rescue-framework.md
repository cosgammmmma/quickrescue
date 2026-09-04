# survival-rescue-framework

intent: clear
review_required: false
status: awaiting-approval
pending_action: write .omo/plans/survival-rescue-framework.md

## Grounding facts
- `Strategy/Strategy.sln` currently contains only `StrategyNew2V2\StrategyNew2V2.csproj`.
- `Strategy/StrategyFightForLive/StrategyFightForLive.cs` is already a survival-challenge strategy shell.
- `Strategy/StrategyBallMoving/StrategyBallMoving.cs` is a separate strategy shell, but its naming indicates transport rather than rescue.
- `Strategy/StrategyNew2V2/Strategy.cs`, `GlobalVar.cs`, and `BaseAction.cs` contain the richest existing tactical framework: stateful globals, safety-aware movement, target approach, angle normalization, and ball-lock/dribble-related helpers.
- Strategy projects in this repo are classic .NET libraries targeting x86 and referencing `URWPGSim2D.Common`, `URWPGSim2D.StrategyLoader`, and often `URWPGSim2D.StrategyHelper`.
- The attached PDF preview could not be reliably extracted by the media tool; the local rule PDF exists in `比赛规则/`, but the tool-side PDF preview is unsupported in this session.

## Components ledger
| id | component | outcome | evidence |
|---|---|---|---|
| C1 | Survival challenge DLL framework | needs to expose a mission-specific strategy skeleton and safe motion hooks | `StrategyFightForLive/StrategyFightForLive.cs` |
| C2 | Extreme rescue DLL framework | needs a rescue-specific strategy skeleton and mission-state handling | `StrategyNew2V2/Strategy.cs`, `GlobalVar.cs`, `BaseAction.cs` |
| C3 | Packaging/build output | needs assembly/project wiring so the intended DLLs are produced consistently | `StrategyFightForLive.csproj`, `StrategyBallMoving.csproj`, `StrategyNew2V2.csproj`, `Strategy.sln` |
| C4 | Self-battle validation | needs a plan for using the produced DLLs against each other and checking obstacle/dribble behavior | existing strategy structure and helper methods |

## Open decisions / defaults
- Decision A: output shape. Default recommendation is **two separate DLLs** (one per mission) because the repo is already organized as per-mission strategy projects and the solution/build metadata is per project.
- Decision B: baseline implementation family. Default recommendation is **reuse existing shells and helper patterns** rather than creating a new architecture from scratch, because the repo already contains a survival shell and a richer movement framework.
- Decision C: verification emphasis. Default recommendation is to make obstacle avoidance, safe approach, and dribble/ball-lock behavior explicit in the plan acceptance criteria, because those are the user-stated risk areas.

## Gate state
- Exploration is sufficient for planning.
- Plan file is not yet written.
- Waiting for user approval on the proposed approach, especially whether the deliverable should be one shared DLL or two mission-specific DLLs.
