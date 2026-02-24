# ProtoScript.Core

ProtoScript language/compiler/runtime stack, workbench, graph induction, and ProtoScript tests.

## What This Repo Owns
- `ProtoScript`, `ProtoScript.Parsers`, `ProtoScript.Interpretter`
- `ProtoScript.CLI`, `ProtoScript.CLI.Validation`
- `ProtoScript.Workbench.Api`, `ProtoScript.Workbench.Web`
- `Ontology.GraphInduction`
- `ProtoScript.Tests`, `ProtoScript.Tests.Integration`

## Solution
- `ProtoScript.sln`

Current solution membership:
- `ProtoScript`
- `ProtoScript.Parsers`
- `ProtoScript.Interpretter`
- `ProtoScript.CLI`
- `ProtoScript.CLI.Validation`
- `ProtoScript.Workbench.Api`
- `ProtoScript.Workbench.Web`
- `ProtoScript.Tests`
- `ProtoScript.Tests.Integration`

`Ontology.GraphInduction` is present in this repo but not currently included in `ProtoScript.sln`.

## Build
From repo root:

```powershell
Scripts\update_dlls.bat
dotnet build ProtoScript.sln
```

## Test
From repo root:

```powershell
dotnet test ProtoScript.Tests\ProtoScript.Tests.csproj
dotnet test ProtoScript.Tests.Integration\ProtoScript.Tests.Integration.csproj --filter "TestCategory=Integration"
```

Integration tests load sample project files from Buffaly NLU project directories.

## Docs
- ProtoScript reference: `docs/ProtoScript/README.md`

## Dependency Model
- Shared binaries are resolved from local `lib\` and `Deploy\`.
- `Scripts\update_dlls.bat` refreshes dependencies from sibling split repos.

## Related Repositories
- Ontology core: https://github.com/Intelligence-Factory-LLC/Ontology.Core
- ProtoScript core: https://github.com/Intelligence-Factory-LLC/ProtoScript.Core
- Buffaly NLU: https://github.com/Intelligence-Factory-LLC/Buffaly.NLU
