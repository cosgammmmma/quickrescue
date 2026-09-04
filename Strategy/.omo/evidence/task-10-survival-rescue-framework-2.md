# Task 10 — Final Packaging Manifest

## Reproducibility checklist (clean source → final DLLs)

### Prerequisites

| Item | Value |
|------|-------|
| MSBuild | `D:\microsoft visual studio\MSBuild\Current\Bin\MSBuild.exe` |
| Target framework | .NET Framework 3.5 |
| Platform target | MSIL (AnyCPU) — loads into x86 simulator via its own loader |
| Reference DLLs | `E:\progaram\URWPGSim2D\URWPGSim2D\bin\` (pre-installed) |

### Build commands

```bat
REM Survival Challenge
"D:\microsoft visual studio\MSBuild\Current\Bin\MSBuild.exe" ^
  "E:\progaram\URWPGSim2D\Strategy\StrategyFightForLive\StrategyFightForLive.csproj" ^
  /p:Configuration=Release /t:Rebuild

REM Extreme Rescue
"D:\microsoft visual studio\MSBuild\Current\Bin\MSBuild.exe" ^
  "E:\progaram\URWPGSim2D\Strategy\StrategyExtremeRescue\StrategyExtremeRescue.csproj" ^
  /p:Configuration=Release /t:Rebuild
```

Expected result: `0 Error(s)` for both.

---

## Artifact map

| Mission | Project | Source file | Output DLL | Size |
|---------|---------|-------------|------------|------|
| 生存挑战 (Survival Challenge) | `StrategyFightForLive\StrategyFightForLive.csproj` | `StrategyFightForLive\StrategyFightForLive.cs` | `StrategyFightForLive\bin\Release\StrategyFightForLive.dll` | 10240 B |
| 极限救援 (Extreme Rescue) | `StrategyExtremeRescue\StrategyExtremeRescue.csproj` | `StrategyExtremeRescue\StrategyExtremeRescue.cs` | `StrategyExtremeRescue\bin\Release\StrategyExtremeRescue.dll` | 11776 B |

DLL path existence verified at manifest time: both `True`.

---

## Evidence files

| Task | File | Contents |
|------|------|----------|
| T1 | `.omo/evidence/task-1-survival-rescue-framework-2.md` | Rule extraction: field size, fish counts, obstacle positions, scoring |
| T2 | `.omo/evidence/task-2-survival-rescue-framework-2.md` | DLL/project mapping decisions |
| T3 | `.omo/evidence/task-3-survival-rescue-framework-2.md` | Helper boundary analysis: pure vs stateful fields |
| T4 | `.omo/evidence/task-4-survival-rescue-framework-2.md` | Survival strategy state machine description |
| T5 | `.omo/evidence/task-5-survival-rescue-framework-2.md` | Rescue strategy state machine + project setup |
| T9 | `.omo/evidence/task-9-survival-rescue-framework-2.md` | Build verification + manual simulator validation steps |
| T10 | `.omo/evidence/task-10-survival-rescue-framework-2.md` | This file |

---

## Strategy summary

### StrategyFightForLive (生存挑战)

- **Field**: 3000×2000 mm, 3 rectangular obstacles (400×400mm) at (0,0), (0,700), (0,-700)
- **Teams**: 4 fish per team; 1 catcher (attacker role) + 3 evaders
- **Half-time swap**: frame counter × msPerCycle estimates 5-min halfway; teamId=0 attacks first
- **Attacker fish 0**: Bang-bang pursuit toward nearest opponent evader; VCode ≤ 15
- **Defender fish 0**: Intercepts catcher-to-evader path; VCode ≤ 15
- **Evader fish 1-3**: Flee to assigned field corners, avoid obstacles; VCode ≤ 8
- **Obstacle avoidance**: Path-to-target checked against 350mm obstacle exclusion radius; if blocked, lateral offset applied and clamped to field bounds

### StrategyExtremeRescue (极限救援)

- **Field**: 4500×3000 mm, 5 rectangular obstacles (1100×100mm)
- **Teams**: 3 fish total: 2 police (attacker) + 1 terrorist (defender)
- **Police fish 0/1**: Three-state machine — `SeekBall` → `CarryToGoal` → `Unstuck`; each locks an independent hostage; pushes hostage to safe zone (X=1800mm default or from `HtMissionVariables`)
- **Terrorist fish 0**: Positions on intercept line between nearest hostage and safe zone
- **Hostages**: Read from `mission.EnvRef.ObstaclesRound`; fallback to `mission.EnvRef.Balls`
- **Anti-jitter**: Lock ID persists until hostage reaches safe zone or stuck timer (60 frames) fires

---

## Known limitations / follow-up items

1. **GUI simulator required for runtime validation** — automated headless testing not possible without desktop-automation tooling. See task-9 evidence for manual steps.
2. **`HtMissionVariables` safe-zone keys** — assumed key names `SafeZone_X` / `SafeZone_Z`; if simulator uses different keys, rescue defaults to X=1800mm.
3. **MSB3270 warnings** — architecture mismatch warnings (MSIL vs x86) are pre-existing across all strategy projects in this solution; not a correctness issue for the simulator's x86 AppDomain loader.
4. **Half-time detection** — survival uses frame estimation; if `CommonPara.TotalSeconds == 0` on init, half is assumed at 9000 frames.
