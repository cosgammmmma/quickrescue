# decisions.md

## GetVCode refactor (2026-09-04) — restore user spec, relocate tuned law (Option C)

- DECISION: keep the empirically tuned approach law (the DLL behavior the live sim was
  tuned against) but move it out of the shared map function. `MapConstants.GetVCode` is
  now the pure user-spec formula `(int)(distMm / 5.0)` clamped to [0, 14] (probe asserts
  GetVCode(70)==14, GetVCode(10)==2). The tuned law `(int)(distMm / 200.0)` floor 3 cap 14
  (plus its full Todo-6 QA tuning comment) now lives verbatim as the private static helper
  `PathExecutor.ApproachVCode`, called from the one real consumer
  (`FollowPath`, `vcode = ApproachVCode(distToCurrent)`).
- Behavior identity: the tuned law executes at the same single FollowPath call site as
  before the refactor, so the DLL's per-cycle decision output is unchanged. `GetDecision`
  no longer wraps `GetDecisionCore` in a try/catch trace; exceptions propagate raw.
- Cleanup: TEMPORARY task-6 QA trace scaffolding fully removed from Strategy.cs (per-cycle
  CSV writer, fields, both call sites). Post-edit greps over StrategyQuickRescue\*.cs:
  zero matches for Trace(/traceCycle/tracePath/TEMPORARY; GetVCode appears only as its
  MapConstants.cs definition and in doc comments — no code call sites remain.
- Verification: MSBuild Debug exit 0 (fresh `URWPGSim2D/bin/StrategyQuickRescue.dll`);
  framework-csc probe (x86, run from bin dir) 11/11 PASS incl. monotone non-decreasing
  sweep d=0..400 step 5 within [0,14] and loader type `URWPGSim2D.Strategy.Strategy`
  still resolving; probe exe + source deleted. Decisions.md convention: append only.


