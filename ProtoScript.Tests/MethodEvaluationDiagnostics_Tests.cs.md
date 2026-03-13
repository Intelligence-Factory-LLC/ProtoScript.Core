# MethodEvaluationDiagnostics_Tests.cs Change History

## Initial Creation (2026-03-13)
- Added regression tests for method-evaluation failure shaping when a non-function symbol is invoked as a function.
- Design Decision: lock in actionable diagnostics (`is not a function`) and prevent fallback `NullReferenceException/Object reference` messages, including in best-effort file-skipping flow.
