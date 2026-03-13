# ProtoScriptCliValidationIncludeTests.cs Change History

## Import Path Alias Validation Behavior (2026-03-12)
- Replaced legacy success expectation for `import Sub/File.pts;` with validation-failure assertions.
- Design Decision: validate parser-facing contract end-to-end through CLI validation diagnostics so callers receive actionable parse guidance (`Use include ...`).
