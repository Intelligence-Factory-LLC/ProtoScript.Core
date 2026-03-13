# Compiler.cs Change History

## Include Missing-File Diagnostics (2026-03-12)
- Updated include-file parsing flow to carry include-site context into missing-file failures.
- Design Decision: wrap missing include targets as `ProtoScriptCompilerException` with `IncludeStatement.Info` so callers receive file/offset for the failing include line instead of a contextless runtime error.
