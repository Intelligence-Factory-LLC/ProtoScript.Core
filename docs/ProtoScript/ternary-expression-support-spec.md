# ProtoScript Ternary Expression Support Spec

## Goal
Add support for compiling and executing ternary expressions in ProtoScript:

`condition ? whenTrue : whenFalse`

Current behavior throws `Unexpected` during parse when `?` is encountered.

## Scope
- Parser support in `ProtoScript.Parsers/Expressions.cs`.
- Compiler support in `ProtoScript.Interpretter/Compiler.cs`.
- Runtime evaluation support in `ProtoScript.Interpretter/NativeInterpretter.cs`.
- Automated coverage in `Ontology.Tests`.

## Out of Scope
- New syntax forms beyond standard ternary `? :`.
- Changes to unrelated operator semantics.

## Parsing Design
- Parse ternary after initial left-side expression term is resolved.
- Represent ternary as:
	- `BinaryOperator("?")`
	- `Left` = condition expression
	- `Right` = `BinaryOperator(":")`
	- `":" .Left` = true branch
	- `":" .Right` = false branch
- Require a `:` token after parsing the true branch.
- Support nesting by recursively parsing branch expressions.
- Preserve precedence by assigning ternary lower precedence than existing binary operators in `GetPrecedence`.

## Compilation Design
- Add compiled expression type `ConditionalOperator` with:
	- `Condition`
	- `TrueExpression`
	- `FalseExpression`
- In compiler binary-operator dispatch:
	- Route `BinaryOperator("?")` to `CompileConditionalOperator`.
- `CompileConditionalOperator` behavior:
	- Validate `Right` is a `BinaryOperator(":")`.
	- Compile condition/true/false subexpressions.
	- Infer result type from branch compatibility:
		- If one branch type can be assigned to the other, use the broader compatible target.
		- If incompatible, emit compiler diagnostic and keep a deterministic inferred type.

## Runtime Design
- Extend expression dispatch in `NativeInterpretter.Evaluate(Compiled.Expression exp)` for `ConditionalOperator`.
- Evaluate condition via existing boolean coercion (`EvaluateAsBool`).
- Short-circuit semantics:
	- If condition is true, evaluate only true branch.
	- If condition is false, evaluate only false branch.

## Test Plan
Create `Ontology.Tests/TernaryOperator_Tests.cs` with the following coverage:

1. `Ternary_BasicTrueBranch_ReturnsTrueExpression`
- Script returns `true ? "A" : "B"`.
- Assert result is `"A"`.

2. `Ternary_BasicFalseBranch_ReturnsFalseExpression`
- Script returns `false ? "A" : "B"`.
- Assert result is `"B"`.

3. `Ternary_Nested_RightAssociative_BehavesCorrectly`
- Script uses nested ternary, e.g. `false ? "A" : true ? "B" : "C"`.
- Assert result is `"B"`.

4. `Ternary_Precedence_WithComparison_BehavesCorrectly`
- Script uses `1 < 2 ? "A" : "B"`.
- Assert result is `"A"`.

5. `Ternary_ShortCircuit_DoesNotEvaluateUnselectedBranch`
- Script uses branch with invalid operation in non-selected side.
- Example: `true ? "OK" : missingSymbol`.
- Assert call returns `"OK"` with no runtime failure.

## Acceptance Criteria
- Parsing no longer throws `Unexpected` for valid ternary expressions.
- Compiler produces a compiled conditional expression for `? :`.
- Runtime returns correct branch result and short-circuits correctly.
- New ternary tests pass.
- Existing tests continue to pass for touched areas.
