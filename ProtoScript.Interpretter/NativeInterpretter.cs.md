# NativeInterpretter.cs Change History

## Return Type Coercion on External Invocation (2026-03-13)
- Added declared return-type coercion in the external `RunMethod(...)` invocation path used by `RunMethodAsObject(...)`.
- Design Decision: align external method execution with internal evaluation behavior so runtime return contracts (including `StringRef`) are enforced consistently.

## Narrowed External Coercion Scope (2026-03-13)
- Adjusted external `RunMethod(...)` coercion to convert only declared `StringRef` and `string` returns.
- Design Decision: preserve legacy non-string return behavior on this hot path while ensuring explicit string contracts cross the C# boundary correctly.
