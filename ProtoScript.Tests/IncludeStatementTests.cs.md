# IncludeStatementTests.cs Change History

## Import Path Alias Contract Update (2026-03-12)
- Updated import-path alias tests to assert parse failure for `import <path>.pts;`.
- Design Decision: enforce explicit syntax contract where file inclusion must use `include` and `import` remains type-import only.
