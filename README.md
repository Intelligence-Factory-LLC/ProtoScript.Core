# ProtoScript.Core
*A full programming language for prototypes, rules, and executable ontologies*

> **Buffaly** is a neurosymbolic platform for building large, explainable knowledge graphs and running language-aware inference over them.
> It is powered by **Ontology** (a prototype graph runtime) and **ProtoScript** (an executable language for defining prototypes, rules, and functions).

This repository contains ProtoScript (parser/compiler/interpreter), the workbench API + editor, graph induction code, and ProtoScript tests.

## Scope
This repository is part of the open-source split of our platform.
It does **not** include several commercial components (partner-only datasets, agentic tooling, and medical extensions).

## ✨ Key capabilities (ProtoScript)
| Feature | Why it matters |
|---|---|
| 🧾 **Executable ontology definitions** | Define prototypes, fields, and functions in one concise file format. |
| ⚙️ **Compiler + interpreter** | Parse, compile, and evaluate ProtoScript in-process. |
| 🧰 **CLI tooling** | Validate parse/compile/runtime behavior from automation. |
| 🧪 **Unit + integration tests** | ProtoScript tests live with ProtoScript (`ProtoScript.Tests*`). |
| 🧑‍💻 **Workbench** | Standalone editor web app + API endpoints for compile/interpret/debug/symbol lookup. |

## What This Repo Owns
1. Language/runtime: `ProtoScript`, `ProtoScript.Parsers`, `ProtoScript.Interpretter`
2. Tooling: `ProtoScript.CLI`, `ProtoScript.CLI.Validation`
3. Workbench: `ProtoScript.Workbench.Api`, `ProtoScript.Workbench.Web`
4. Tests: `ProtoScript.Tests`, `ProtoScript.Tests.Integration`
5. Induction: `Ontology.GraphInduction` (present in repo; not currently included in `ProtoScript.sln`)

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

## 📚 ProtoScript reference docs (jump links)
The reference manual is split into focused sections under `docs/ProtoScript/`.

- Index: [docs/ProtoScript/README.md](docs/ProtoScript/README.md)
- Introduction: [docs/ProtoScript/introduction.md](docs/ProtoScript/introduction.md)
- Ontology context: [docs/ProtoScript/ontology-context.md](docs/ProtoScript/ontology-context.md)
- What are prototypes?: [docs/ProtoScript/what-are-prototypes.md](docs/ProtoScript/what-are-prototypes.md)
- Syntax and features: [docs/ProtoScript/syntax-and-features.md](docs/ProtoScript/syntax-and-features.md)
- Native value prototypes: [docs/ProtoScript/native-value-prototypes.md](docs/ProtoScript/native-value-prototypes.md)
- Examples of prototype creation: [docs/ProtoScript/examples-of-prototype-creation.md](docs/ProtoScript/examples-of-prototype-creation.md)
- Simpsons example: [docs/ProtoScript/simpsons-example.md](docs/ProtoScript/simpsons-example.md)
- Relationships: [docs/ProtoScript/relationships.md](docs/ProtoScript/relationships.md)
- Shadows and LGG: [docs/ProtoScript/shadows-and-lgg.md](docs/ProtoScript/shadows-and-lgg.md)
- Prototype paths: [docs/ProtoScript/prototype-paths.md](docs/ProtoScript/prototype-paths.md)
- Subtypes: [docs/ProtoScript/subtypes.md](docs/ProtoScript/subtypes.md)
- Transformation functions: [docs/ProtoScript/transformation-functions.md](docs/ProtoScript/transformation-functions.md)
- Ternary expression support spec: [docs/ProtoScript/ternary-expression-support-spec.md](docs/ProtoScript/ternary-expression-support-spec.md)

## 🚀 Build
The split repos are designed to sit as **siblings** on disk so `Scripts\update_dlls.bat` can copy DLLs between `..\ontology-core`, `..\protoscript-core`, and `..\buffaly-nlu`.
If you clone into different folder names, update the paths inside `Scripts\update_dlls.bat`.

From repo root:

```powershell
Scripts\update_dlls.bat
dotnet build ProtoScript.sln
```

## 🧪 Test
From repo root:

```powershell
dotnet test ProtoScript.Tests\ProtoScript.Tests.csproj
dotnet test ProtoScript.Tests.Integration\ProtoScript.Tests.Integration.csproj --filter "TestCategory=Integration"
```

Integration tests load sample project files from the Buffaly portal project directories in `Buffaly.NLU`.

## 📦 Dependency model
- Shared binaries are resolved from local `lib\` and `Deploy\`.
- `Scripts\update_dlls.bat` refreshes DLLs from sibling split repos.

## Related repositories
- Ontology core: https://github.com/Intelligence-Factory-LLC/Ontology.Core
- ProtoScript core: https://github.com/Intelligence-Factory-LLC/ProtoScript.Core
- Buffaly NLU: https://github.com/Intelligence-Factory-LLC/Buffaly.NLU

## 🏥 Need help?
We deploy explainable, neurosymbolic systems in regulated domains (healthcare and beyond).
If you'd like guidance or custom development, email **support@intelligencefactory.ai** or visit **https://intelligencefactory.ai**.

*© 2026 Intelligence Factory, LLC*
