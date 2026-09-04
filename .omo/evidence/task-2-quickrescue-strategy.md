# Task 2 — BFS shortest path + obstacle inflation + waypoint compression (PathFinder.FindPath)

Date: 2026-09-04. Wave 2, func 4. Changed files: `Strategy/StrategyQuickRescue/PathFinder.cs` ONLY.

## Implementation summary

- `FindPath(Point2D, Point2D, double, out List<Point2D>)` — exact stub signature kept; body replaced.
  `ShouldUpdatePath` stub left byte-identical (later wave owns it).
- Grid: step 40mm over field [-2250,2250] x [-1500,1500]. Convention: cell (i,j) covers
  `X [LeftMm + i*40, LeftMm + (i+1)*40)`, `Z [TopMm + j*40, TopMm + (j+1)*40)`; representative point =
  cell center `LeftMm + (i+0.5)*40 / TopMm + (j+0.5)*40`. Counts: Nx=floor(4500/40)=112, Nz=75
  (slim right/bottom strip has no cells; points there snap to nearest covered cell).
- Walkability: cell walkable iff `MapConstants.IsPointValid(cx, cz, inflationMm)` at its center
  (field shrink + inflated-AABB exclusion in one call). Precomputed into a bool[8400] per call.
- Start/target gate: `!IsPointValid(start|target, inflationMm)` → return false + empty list.
- Snap: per-axis `Math.Round((p - origin)/40 - 0.5)`, clamped; if that cell not walkable,
  expanding Chebyshev rings scanned in fixed (dz, dx) order, first walkable wins (deterministic).
- BFS: 4-connected (order -X,+X,-Z,+Z; NO diagonals → no corner cutting), Queue<int>,
  parent int[] (parent[start]=start), early exit on dequeue of target → shortest in cell steps.
- Reconstruct parent chain, reverse → start→target. Raw point list = [start, cell centers..., target].
- Compression: drop middle point when |cross(lastKept→mid, mid→next)| <= 1e-9; always keep first/last.
  First = original start, last = original target (both valid); interior points are grid centers.
- Raw-count test hook: overload `FindPath(..., out List<Point2D> waypoints, out int rawCellCount)`
  (rawCellCount = cells in uncompressed BFS path, 0 on failure). The 4-arg overload delegates to it.
- C# 3 only (net35): Queue/List/arrays, no out-var, no interpolation, no expression-bodied members.

## Build (isolated protocol — concurrent agent builds the same csproj)

```
& "D:\microsoft visual studio\MSBuild\Current\Bin\MSBuild.exe" "Strategy\StrategyQuickRescue\StrategyQuickRescue.csproj" /p:Configuration=Debug /p:BaseIntermediateOutputPath="C:\Users\28954\AppData\Local\Temp\opencode\qr-t2\obj\\" /p:OutputPath="C:\Users\28954\AppData\Local\Temp\opencode\qr-t2\bin\\" /v:m /nologo
```

Result: SUCCESS — `StrategyQuickRescue -> C:\Users\28954\AppData\Local\Temp\opencode\qr-t2\bin\StrategyQuickRescue.dll` (0 errors, 0 warnings).
No writes to `Strategy/StrategyQuickRescue/obj/` or `URWPGSim2D/bin/`.

## Probe

Dir `qr-t2\probe\`: copied `URWPGSim2D\bin\*.dll`, overwrote `StrategyQuickRescue.dll` with the fresh
isolated build. Compiled with
`C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /platform:x86 /r:StrategyQuickRescue.dll /r:URWPGSim2D.Common.dll /r:URWPGSim2D.StrategyLoader.dll Probe.cs`
from the probe dir; ran `Probe.exe` there. Segment-vs-AABB check = 2D Liang-Barsky slab clip,
inclusive (boundary touch counts as hit), boxes inflated by 100.

Full output:

```
PASS  A1 FindPath((-2000,0),(2000,0),100) true, non-empty
    waypoints=8 rawCells=135
    wp[0] = (-2000, 0)
    wp[1] = (-190, 0)
    wp[2] = (-190, -680)
    wp[3] = (370, -680)
    wp[4] = (370, -280)
    wp[5] = (2010, -280)
    wp[6] = (2010, 0)
    wp[7] = (2000, 0)
PASS  A2 every waypoint IsPointValid(.., 100)
PASS  A3 compressed segments clear of inflated AABBs
PASS  A4 compressed count (8) < raw cell count (135)
PASS  A5 FindPath((0,0),(2000,0),100) false + empty
PASS  A6 FindPath((-2000,0),(0,0),100) false + empty
PASS  A7 no waypoint inside inflated obs1 region
    segment (-2000, 1200) -> (2000, 1200) hits obstacle 1
    suggested lane (-2000,1200)->(2000,1200) straight-segment clear: False (expected False -> substituting documented clear lane x=300)
PASS  A8a suggested z=1200 lane confirmed NOT clear (substitution justified)
PASS  A8b substitute lane (300,-1400)->(300,1400) straight segment clear
    lane path: waypoints=4 rawCells=71 length=2820.000 straight=2800 ratio=1.007143
PASS  A8c substitute lane FindPath true, non-empty
PASS  A8d path length 2820.0 <= 1.5x straight (4200)
PASS  A8e substitute lane segments clear of inflated AABBs
ALL PASS
```

Probe exit code 0. 12/12 PASS.

## Assertion-8 lane substitution (documented per plan)

The plan's suggested lane `(-2000,1200)->(2000,1200)` is NOT clear: obs2 inflated by 100 is
X[400,700] x Z[300,1600] (obs3 mirrored at X[-700,-400]); z=1200 lies inside that z-range, so the
straight segment crosses both (probe A8a confirms a hit on obstacle index 1). No horizontal lane is
clear at all (|z|<=650 hits obs1's x-range; 650<|z| hits obs2-5's z-ranges at x in ±[400,700]).
Substitute: vertical lane x=300, (300,-1400)->(300,1400) — x=300 misses obs1 X[-150,150] and
obs2-5 X ±[400,700]; probe A8b verifies the straight segment clear. BFS path length 2820 vs 2800
straight (ratio 1.007 < 1.5) — near-optimal.

## Path sanity note (A1 route)

The A1 route detours around BOTH obs1 (drops to z=-680 before crossing x in [-150,150]) and obs4
(climbs to z=-280 at x=370, i.e. above inflated obs4 Z[..,-300], before crossing x in [400,700]) —
consistent with a true BFS shortest 4-connected path under 100mm inflation, not a hardcoded detour.

## Known limitation (documented for later waves)

First/last segments (original point → snapped center, <= ~28mm diagonal) are not axis-aligned; a
start/target within ~half a cell of an inflated-AABB corner could in theory produce a corner-touch.
Not hit by any probed lane; call-site points (fish pos, push point) are arbitrary, so Wave 3+
should treat a false/empty or downstream segment-check failure as "steer straight at low speed"
per the orchestration fallback. Interior grid segments are provably clear (axis-aligned between
valid centers; every inflated box is >= 300mm wide vs 40mm spacing, so any overlap would contain a
raw center — contradiction).

## Cleanup receipts

- `C:\Users\28954\AppData\Local\Temp\opencode\qr-t2\` deleted entirely (see DoneClaim; `Test-Path` = False).
- `grep TODO|FIXME` on `Strategy/StrategyQuickRescue/`: no matches.
- Workspace tree: only `Strategy/StrategyQuickRescue/PathFinder.cs` modified by this task
  (timestamp 01:26:22; Steering.cs 01:22:57 predates this session — concurrent agent's file, untouched).
- No writes to `URWPGSim2D/bin/` or `Strategy/StrategyQuickRescue/obj/` (verified: obj/ absent).

## Risks

- None blocking. `FindPath` recomputes the 8400-cell grid per call — trivial cost at 100ms decision
  cadence; cache only if profiling says otherwise.
- Overload with `rawCellCount` is public API surface; it is the documented test hook for assertion 4.
