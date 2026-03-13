# AnnotationRuntimeDiagnostics_Tests.cs Change History

## Initial Creation (2026-03-13)
- Added regression test proving method-level annotation parameter mismatches emit detailed runtime diagnostics instead of opaque `Cannot assign value`.
- Design Decision: preserve existing behavior (runtime failure) while enforcing actionable error text to quickly identify malformed annotation usage in project files.
