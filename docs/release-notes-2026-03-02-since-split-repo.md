# Release Notes (Since Split-Repo Move)

This summary covers work after `35f5756` (`2026-02-24`, "docs: refresh README for split-repo ownership and links") through `6ef277e` (`2026-03-02`).

## Highlights

### Runtime and compiler reliability
- Added runtime prototype resolver and method binder helpers, with tests.
- Hardened constructor null handling and surfaced compile failures end-to-end.
- Wrapped unexpected compile exceptions with stage/file context.
- Preserved source locations for method resolution diagnostics.
- Prevented prototype compile null `AddRange` crash.
- Fixed `DefinePrototypes` annotation crash.

### Imports, includes, externs, and references
- Improved include path diagnostics and path-literal handling.
- Added support for unquoted include paths and `.pts` import aliases.
- Added extern runtime object declarations with compile-time method checks.
- Fixed extern object null comparisons and declared-global binding behavior.
- Added DLL path references with fail-fast loading and dependency resolution.
- Shadow-copied referenced DLLs before loading to avoid source locks.

### Test coverage and regression hardening
- Added compile-project layout regression tests for include/import/extern scenarios.
- Added isolated import/extern regression tests and malformed import error shaping.
- Added regression coverage for async .NET invocation receiver and auto-await inference behavior.
- Added regression tests for prototype compiler, runtime binding helpers, and project layouts.
- Fixed reference statement test literals so the test project builds.

### Documentation and licensing
- Expanded public README content after the split-repo move.
- Documented include/import path literals and extern runtime object declarations.
- Added GPL-3.0 LICENSE and README licensing references.

## Commit Log (Post-Move)
- `1427c05` (2026-02-24): docs: expand public README for ProtoScript.Core
- `229af3b` (2026-02-24): Update README
- `a6cf722` (2026-02-24): docs: reference GPL-3.0 license in README
- `0708eff` (2026-02-24): Add LICENSE
- `4817f3f` (2026-02-24): Improve include-path diagnostics and enforce quoted include literals
- `a189f4e` (2026-02-25): Allow unquoted include paths and import .pts path aliases
- `58ed177` (2026-02-25): Add extern runtime object declarations with compile-time method checks
- `03f715b` (2026-02-25): Document include/import path literals and extern runtime object declarations
- `e6ebd28` (2026-02-28): Add DLL path references with fail-fast loading, dependency resolution, and tests
- `2c34067` (2026-02-28): Fix reference statement tests string literals so test project builds
- `d4b547a` (2026-02-28): Harden constructor null handling and surface compile failures end-to-end
- `936e275` (2026-02-28): Fix async .NET instance invocation receiver and add regression test
- `0e0ed10` (2026-02-28): Align async method inferred types with auto-await runtime semantics
- `7b049ee` (2026-03-01): Prevent prototype compile null AddRange crash and add regression test
- `3519f8d` (2026-03-01): Wrap unexpected compile exceptions with stage and file context
- `070e806` (2026-03-01): Preserve source location for method resolution diagnostics
- `5acee91` (2026-03-01): Shadow-copy referenced DLLs before loading to avoid source file locks
- `5c02875` (2026-03-02): Fix DefinePrototypes annotation crash and add project-layout regression repro
- `c02fcb4` (2026-03-02): Add compile-project layout regression tests for include/import/extern scenarios
- `badcdc9` (2026-03-02): Add isolated import/extern regression tests and shape malformed import errors
- `0df53e0` (2026-03-02): Fix extern object null comparisons and add safe declared-global binding
- `0186b09` (2026-03-02): Check in remaining parser/compiler and project layout regression changes
- `6ef277e` (2026-03-02): Add runtime prototype resolver and method binder helpers with tests
