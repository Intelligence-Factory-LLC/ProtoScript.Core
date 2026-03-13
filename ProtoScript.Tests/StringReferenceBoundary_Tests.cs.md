# StringReferenceBoundary_Tests.cs Change History

## Initial Creation (2026-03-13)
- Added regression tests for `StringRef` boundary behavior:
- `StringRef` returns as opaque handle.
- Handles auto-resolve when passed back to functions expecting `string` or `String`.
- `string` return contract remains unchanged.
- Design Decision: verify observable contract at runtime boundary rather than internal implementation details.
