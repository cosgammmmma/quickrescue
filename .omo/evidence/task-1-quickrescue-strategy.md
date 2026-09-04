# Task 1 Evidence — StrategyQuickRescue scaffold + geometry primitives

Date: 2026-09-04
Scope: funcs 1, 2, 3 (project scaffold, Strategy stub contract, MapConstants geometry + stub signatures)

## Files created

- `Strategy/StrategyQuickRescue/StrategyQuickRescue.csproj`
- `Strategy/StrategyQuickRescue/Properties/AssemblyInfo.cs`
- `Strategy/StrategyQuickRescue/Strategy.cs`
- `Strategy/StrategyQuickRescue/MapConstants.cs`
- `Strategy/StrategyQuickRescue/PathFinder.cs`
- `Strategy/StrategyQuickRescue/Steering.cs`
- `Strategy/StrategyQuickRescue/PathExecutor.cs`

All in the same directory `Strategy/StrategyQuickRescue/` (AssemblyInfo under `Properties/` per convention).

## csproj vs sample (StrategyNew2V2.csproj)

Kept identical: ToolsVersion 4.0, OutputType=Library, RootNamespace=URWPGSim2D.Strategy,
TargetFrameworkVersion v3.5, FrameworkPathOverride (net35 reference assemblies, verified present
at `%USERPROFILE%\.nuget\packages\microsoft.netframework.referenceassemblies.net35\1.0.3\...`),
PlatformTarget=x86 (Debug), ALL 10 Reference entries (HintPaths into `..\..\URWPGSim2D\bin\`,
Private=False), DefineConstants, BootstrapperPackage items.

Changed (required):

- `AssemblyName`: `wsy2v2` -> `StrategyQuickRescue`
- `ProjectGuid`: new GUID `{DBF36117-3FC6-4AEE-9641-4DE0930F8D33}`
- `OutputPath` (Debug and Release): `bin\Debug\` / `bin\Release\` -> `..\..\URWPGSim2D\bin\`.
  NOTE: the sample's literal OutputPath is the project-local `bin\Debug\`; the task's verification
  criterion requires the DLL emitted to `URWPGSim2D/bin/`, so OutputPath points at
  `..\..\URWPGSim2D\bin\` (same folder the HintPaths resolve from). Confirmed by build output below.
- Compile items: MapConstants.cs, PathExecutor.cs, PathFinder.cs, Properties\AssemblyInfo.cs,
  Steering.cs, Strategy.cs (sample had BaseAction.cs/GlobalVar.cs — not copied; this wave is a stub)
- Dropped the sample's stale `<None Include="StrategNew2V2.csproj" />` self-reference.

## Build

Command:

```
& "D:\microsoft visual studio\MSBuild\Current\Bin\MSBuild.exe" \
  "E:\progaram\URWPGSim2D\Strategy\StrategyQuickRescue\StrategyQuickRescue.csproj" \
  /p:Configuration=Debug /nologo /v:n
```

Result (Chinese locale MSBuild):

```
StrategyQuickRescue -> E:\progaram\URWPGSim2D\URWPGSim2D\bin\StrategyQuickRescue.dll
已成功生成。   (Build succeeded)
    0 个警告   (0 Warning(s))
    0 个错误   (0 Error(s))
已用时间 00:00:00.21
```

Artifact: `E:\progaram\URWPGSim2D\URWPGSim2D\bin\StrategyQuickRescue.dll` (7168 bytes, 2026-09-04 01:14:38).

## Probe (compiled + executed, not hand-verified)

Probe harness: temp console app referencing the built DLL
(`csc /platform:x86 /r:StrategyQuickRescue.dll /r:URWPGSim2D.Common.dll /r:URWPGSim2D.StrategyLoader.dll`),
executed from `URWPGSim2D/bin` so the 3.5 strategy assembly and its deps resolve. Exit code 0.

Required MUST-DO assertions — all PASS:

```
PASS GetVCode(70)==14
PASS GetVCode(10)==2
PASS Distance((0,0),(30,40))==50.0
PASS IsPointValid(0,0,0)==false        (inside obs1 X[-50,50] Z[-550,550])
PASS IsPointValid(-2000,0,0)==true
PASS IsPointValid(-2151,0,100)==false  (field shrinks to X>=-2150 at infl=100)
PASS IsPointValid(-2149,0,100)==true
```

Additional shape/contract assertions — all PASS:

```
PASS LeftMm==-2250 / RightMm==2250 / TopMm==-1500 / BottomMm==1500
PASS Obstacles.Length==5
PASS FindPath stub returns false (out waypoints non-null empty list)
PASS ShouldUpdatePath stub returns true
PASS SegmentAngle stub returns 0
PASS FormatAngle stub identity
PASS GetTCode stub returns 7
PASS FollowPath stub (tcode 7, vcode 0)
PASS FullName==URWPGSim2D.Strategy.Strategy   (loader hard-requirement)
PASS is MarshalByRefObject
PASS implements URWPGSim2D.StrategyLoader.IStrategy
PASS InitializeLifetimeService()==null
PASS GetTeamName() non-empty ("QuickRescue")
ALL PROBES PASSED  (23/23, exit 0)
```

## Stub signatures delivered (real logic deferred to Todos 2/3/4)

- `PathFinder.FindPath(Point2D start, Point2D target, double inflationMm, out List<Point2D> waypoints)` -> false, empty list
- `PathFinder.ShouldUpdatePath(Point2D fishPos, Point2D target, List<Point2D> currentPath)` -> true
- `Steering.SegmentAngle(Point2D a, Point2D b)` -> 0.0
- `Steering.FormatAngle(double a)` -> a
- `Steering.GetTCode(double currentRad, double desiredRad)` -> 7
- `PathExecutor.FollowPath(Point2D fishPos, double headingRad, List<Point2D> waypoints, ref int waypointIndex, out int tcode, out int vcode)` -> (7, 0)

## Cleanup receipts

- Deleted `URWPGSim2D/bin/quickrescue-probe.exe` (verified absent afterwards)
- Deleted temp probe exe from `%TEMP%\opencode\`
- Temp probe source `quickrescue-probe.cs` and build log remain only in `%TEMP%\opencode\` (outside workspace)
- No simulator core files touched (`Match/`, `Common/`, `StrategyLoader/`, `StrategyNew2V2/` untouched)
- Workspace is not a git repo — no commits made

## Risks / notes

- OutputPath deviation from the sample's literal value is intentional (see above); Debug and Release
  both emit to `URWPGSim2D/bin/`.
- Code is C# 3-compatible (net35 target): no ValueTuple — FollowPath uses out params for (tcode, vcode).
- The project is NOT added to `Strategy/Strategy.sln` (not requested); build via the csproj directly.
- `Common/config.xml` defaults say FieldLengthXMm=3000/ZMm=2000, but MUST-DO specified the QuickRescue
  mission bounds ±2250/±1500 — MUST-DO values used (they also match every probe assertion).
