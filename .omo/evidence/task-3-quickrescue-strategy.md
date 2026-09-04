# Task 3 Evidence — Steering math (SegmentAngle / FormatAngle / GetTCode)

Date: 2026-09-04
Plan checkbox: "3. Implement steering math: segment angle + legal-turn TCode mapping (funcs 6, 7)"

## Changed files

- `Strategy/StrategyQuickRescue/Steering.cs` (ONLY file modified)
  - `SegmentAngle(a, b)` -> `Math.Atan2(b.Z - a.Z, b.X - a.X)`
  - `FormatAngle(a)` -> reference loop semantics, wraps to `(-PI, PI]`, loop-based (huge-input safe)
  - `GetTCode(currentRad, desiredRad)` -> `delta = FormatAngle(desiredRad - currentRad)`;
    `t = 7 + (int)Math.Round(delta * 8.0 / Math.PI, MidpointRounding.AwayFromZero)`, clamped `[0, 15]`.
    XML comment documents that the geometric sign of positive delta (left vs right) is empirical,
    to be tuned in simulator QA (Todo 6). No sign flip baked in.
  - Signatures unchanged from Wave-1 stubs; C# 3 only (no expression-bodied members,
    no string interpolation, no nameof); no TODO/FIXME (grep verified).

## Build (isolated protocol — no writes to project obj/ or URWPGSim2D/bin)

Command:

```
& "D:\microsoft visual studio\MSBuild\Current\Bin\MSBuild.exe" "Strategy\StrategyQuickRescue\StrategyQuickRescue.csproj" /p:Configuration=Debug /p:BaseIntermediateOutputPath="C:\Users\28954\AppData\Local\Temp\opencode\qr-t3\obj\\" /p:OutputPath="C:\Users\28954\AppData\Local\Temp\opencode\qr-t3\bin\\" /v:m /nologo
```

Result: SUCCESS — `StrategyQuickRescue -> C:\Users\28954\AppData\Local\Temp\opencode\qr-t3\bin\StrategyQuickRescue.dll`, 0 errors, 0 warnings.

## Probe

Setup: `qr-t3\probe\` = copy of `URWPGSim2D\bin\*.dll`, then `StrategyQuickRescue.dll`
overwritten with the fresh isolated build (timestamp 2026/9/4 1:23:15).
Compiled with `C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /platform:x86 /nologo /out:Probe.exe Probe.cs /r:StrategyQuickRescue.dll /r:URWPGSim2D.Common.dll /r:URWPGSim2D.StrategyLoader.dll`
from the probe dir; executed in the probe dir. Exit code 0.

### Results (21/21 PASS)

| # | Assertion | Result |
|---|-----------|--------|
| T1 | `GetTCode(0, 0) == 7` | PASS |
| T2 | `GetTCode(0, π) == 15` | PASS |
| T3 | `GetTCode(0, -π) == 15` (-π ≡ π under (-π,π] wrap) | PASS |
| T4 | `GetTCode(0, π/2) == 11` | PASS |
| T5 | `GetTCode(0, -π/2) == 3` | PASS |
| F1 | `FormatAngle(3π) ≈ π` (diff 0) | PASS |
| F2 | `FormatAngle(-3π) ≈ π` (diff 0) | PASS |
| F3 | `FormatAngle(-2π) ≈ 0` (diff 0) | PASS |
| F4 | `FormatAngle(π) ≈ π` (diff 0) | PASS |
| F5 | `FormatAngle(-π) ≈ π` (diff 0) | PASS |
| F6 | `FormatAngle(100π) ≈ 0` (act -7.1e-15, diff 7.1e-15) | PASS |
| F7 | `FormatAngle(101π) ≈ π` (diff 2.5e-14) | PASS |
| S1 | `SegmentAngle((0,0),(1,0)) ≈ 0` | PASS |
| S2 | `SegmentAngle((0,0),(0,1)) ≈ π/2` | PASS |
| S3 | `SegmentAngle((0,0),(-1,0)) ≈ +π` | PASS |
| S4 | `SegmentAngle((0,0),(0,-1)) ≈ -π/2` | PASS |
| M1 | sweep delta ∈ [-π, π] (20001 samples): all TCode in [0,15] | PASS |
| M2 | sweep (-π, π): adjacent outputs differ ≤ 1 (monotonic staircase, no 15→0 glitch) | PASS |
| M3 | full-turn equivalence: `GetTCode(0,-π)=15`, `GetTCode(0,-π+ε)=0` | PASS |
| R1 | `GetTCode(π/2, π/2) == 7` | PASS |
| R2 | `GetTCode(π, 0) == GetTCode(0, -π) == 15` (relative-delta invariance) | PASS |

Final line: `ALL PASS`, `EXITCODE=0`.

## Interpretation note (probe 8)

The only adjacent discontinuity in the delta-sweep is at exactly `delta = -π`:
`FormatAngle(-π) = +π` → TCode 15, while `-π + ε` → TCode 0. This is not a wrap-around
glitch — it is the mandated (-π, π] wrap plus the fact that full-left (0) and full-right (15)
are geometrically the same 180° turn (assertion T3 explicitly pins `GetTCode(0,-π)==15`).
The open-interval staircase is unit-step monotonic (M2), and the closed interval stays in
[0,15] (M1). Clamping absorbs the `round()` overshoot (7±8) at both ends.

## Loop vs float boundaries

Pre-probe float check of the reference loop (PowerShell doubles): 100π → -7.1e-15,
101π → π + 2.5e-14, ±π/±2π/±3π exact. Loop kept per spec; errors far below the 1e-9 tolerance.
(Known cosmetic: for ~12345π the loop can land at -π+3.6e-10 instead of +π — same angle,
not exercised by any contract; loop remains spec-exact.)

## Constraints honored

- Only `Steering.cs` modified. `PathFinder.cs` untouched (owned by another agent).
- No build of the workspace csproj without redirects; no writes to
  `Strategy/StrategyQuickRescue/obj/` or `URWPGSim2D/bin/`.
- TCode always in [0,15]; normalization across ±π never skipped; no sign flip added.
- Simulator core (`Match/`, `Common/`, `StrategyLoader/`) untouched.

## Cleanup receipts

- `C:\Users\28954\AppData\Local\Temp\opencode\qr-t3\` deleted entirely after evidence capture;
  deletion verified (`Test-Path` → False). Receipt appended below after verification.

## Risks / handoff notes

- Sign convention of positive delta → left/right is deliberately untuned (documented in the
  XML comment); Todo 6 must verify empirically in the simulator and flip at the call site or
  here if needed — single place: the `7 + ...` expression.
- `Math.Round(..., AwayFromZero)` means half-step deltas (odd multiples of π/16) round away
  from straight; symmetric, deterministic — QA should be aware.
- Consumers: Todo 4 (FollowPath) calls `GetTCode(heading, SegmentAngle(...))`; Todo 5 push
  phase reuses the same mapping. Both inherit the (-π, π] + clamp guarantees.
