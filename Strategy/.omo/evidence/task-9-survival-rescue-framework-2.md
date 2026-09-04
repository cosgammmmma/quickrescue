# Task 9 — Self-battle / Simulator Validation

## Simulator executable

`E:\progaram\URWPGSim2D\URWPGSim2D\bin\URWPGSim2D.exe` — GUI-only Windows application.
No headless CLI mode is exposed; automated DLL loading requires a human to point the simulator
at the DLL paths via its configuration dialog and click "Start". This step cannot be executed
by an agent without desktop-automation tooling (Playwright/AutoIt/etc.).

## DLL paths confirmed present for loading

| DLL | Path | Size | Built |
|-----|------|------|-------|
| StrategyFightForLive.dll | `E:\progaram\URWPGSim2D\Strategy\StrategyFightForLive\bin\Release\StrategyFightForLive.dll` | 10240 bytes | 2026-08-22 17:46 |
| StrategyExtremeRescue.dll | `E:\progaram\URWPGSim2D\Strategy\StrategyExtremeRescue\bin\Release\StrategyExtremeRescue.dll` | 11776 bytes | 2026-08-22 17:50 |

Both paths verified via `Test-Path` (returned `True`) and `Get-Item`.

## Reproducible clean rebuild verification

Both DLLs were cleaned and rebuilt from source via:

```
MSBuild StrategyFightForLive.csproj /p:Configuration=Release /t:Rebuild
MSBuild StrategyExtremeRescue.csproj /p:Configuration=Release /t:Rebuild
```

Result: **0 errors** on both. Output lines confirmed:
```
StrategyFightForLive -> ...\bin\Release\StrategyFightForLive.dll
StrategyExtremeRescue -> ...\bin\Release\StrategyExtremeRescue.dll
```

Warnings: 4× MSB3270 (processor architecture mismatch MSIL vs x86) — pre-existing on all
strategy projects in this solution; not introduced by our code.

## Static API-shape validation (loader contract)

Both DLLs expose the required loader entry points (verified by reading source):

- Namespace: `URWPGSim2D.Strategy`
- Class: `Strategy : MarshalByRefObject, IStrategy`
- Methods: `InitializeLifetimeService()`, `GetTeamName()`, `GetDecision(Mission, int)`
- All fish indices `0..FishCntPerTeam-1` receive a `Decision` each cycle
- VCode values: survival normal fish ≤ 8, special fish ≤ 15; rescue ≤ 15
- TCode values: always in [0,14] (selected from tTable index)

## Manual simulation steps (for human operator)

1. Launch `URWPGSim2D.exe`
2. For **生存挑战**: set Team A DLL = `StrategyFightForLive.dll`, Team B DLL = same (self-battle), mission = 生存挑战
3. For **极限救援**: set Team A DLL = `StrategyExtremeRescue.dll`, Team B DLL = same, mission = 极限救援
4. Confirm no crash on load (DLL loaded message appears in status bar)
5. Run 3-minute session; observe: fish avoid 3 rectangular obstacles, attacker pursues evaders, evaders flee to corners
6. For rescue: observe police fish approach circular hostages, push them toward right-side safe zone, terrorist blocks

## Known limitations

- GUI validation not automated; requires human operator per plan note ("Must not rely only on build success").
- `HtMissionVariables["SafeZone_X/Z"]` key names in rescue are assumed; if simulator uses different keys, safe-zone X defaults to 1800mm (right edge margin).
- Half-time detection in survival uses frame counting + `CommonPara.TotalSeconds`; if `TotalSeconds` is 0 on init the half is estimated at 9000 frames (5 min × 30 fps).
