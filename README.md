# Buffaly.Ontology
*A production-ready, neurosymbolic ontology engine for healthcare & beyond*


> **Buffaly.Ontology** is the open-source core we use to power our ICD-10-CM,  
> SNOMED CT and language-aware medical knowledge graphs.  
> Written in **ProtoScript** (a full programming language, not just a data spec),  
> it lets you *load, query and extend* huge ontologies **in real time**-no SQL, no RDF hassle.

---

## Scope

This repository is the open-source part of our platform.
It does **not** include several commercial components, including agentic tools, medical extensions, and other partner-only platform pieces. Those are available to paying partners.

---

## ✨ Key capabilities

| Feature | Why it matters |
|---------|----------------|
| **ProtoScript language** | Define concepts *and* executable functions in one concise file. Hot-compile at runtime. |
| **Unified graph** | Seamlessly mix ICD-10-CM, SNOMED CT, WordNet, VerbNet, custom vocabularies. |
| **Lazy-loading + caching** | Load only the SNOMED concepts you touch-handle 300 k+ items on a laptop. |
| **Neurosymbolic hooks** | Call out to an LLM to create new prototypes automatically, then store them as first-class objects. |
| **Explainability baked-in** | Every mapping, rule and inference is traceable-crucial for regulated domains such as healthcare. |

---

## Four main pieces

1. **Ontology** - prototype-based data structures and rules:
The `Prototype` graph model supports typed nodes, property edges, children collections, multi-parent `TypeOf` inheritance, structural comparison, categorization checks, and graph parameterization/path operations.
2. **ProtoScript** with documentation:
Language/parser/compiler/interpreter stack (`ProtoScript*` projects), plus manual docs under `docs/ProtoScript/README.md`, and CLI validation tooling in `ProtoScript.CLI*`.
3. **Deterministic NLU** built on the previous:
`Buffaly.NLU` and `Buffaly.NLU.Tagger` provide deterministic token/lexeme tagging, expectation rollout, subtype application, and semantic transfer into ontology structures.
4. **Graph learning and induction algorithms**:
`Ontology.GraphInduction` includes leaf-based transform mining, HCP/HCPTree modeling, structural signatures, and scoring primitives. Some advanced flow remains experimental/in-progress.

---

## 🚀 Quick start

```bash
dotnet add package Buffaly.Ontology
```

```csharp
using Buffaly.Ontology;
using Buffaly.Ontology.ICD10CM;

	// 1. Boot the engine
	OntologyWorld world = OntologyWorld.CreateDefault();

	// 2. Ask for a code
	ICD10Code i517 = world.Lookup<ICD10Code>("I51.7");
	Console.WriteLine(i517.Description);   // -> "Cardiomegaly"

	// 3. Dynamically extend with ProtoScript
	string proto = """
	partial prototype Myocardial_HypertrophySememe : Sememe
	{
	    Children = [HeartSememe, HypertrophySememe];
	}
	""";
	world.CompileProtoScript(proto);
```

```csharp
using ProtoScript.Interpretter;
using ProtoScript.Interpretter.Interpretting;
using ProtoScript.Parsers;
using System.Collections.Generic;

string script = """
namespace SimpsonsOntology
{
	prototype Person {
		String Name = "";
		Collection ParentOf = new Collection();
		function IsParent() : Boolean {
			return ParentOf.Count > 0;
		}
		function HasChild(Person child) : Boolean {
			return ParentOf.Contains(child);
		}
	}

	prototype Bart : Person {
		Name = "Bart Simpson";
	}

	prototype Homer : Person {
		Name = "Homer Simpson";
		ParentOf = [Bart];
	}
}
""";

ProtoScript.File file = Files.ParseFileContents(script);
Compiler compiler = new Compiler();
compiler.Initialize();
ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
NativeInterpretter interpreter = new NativeInterpretter(compiler);
interpreter.Evaluate(compiled);

bool isParent = (bool)interpreter.RunMethodAsObject("Homer", "IsParent", new List<object>());
Console.WriteLine($"Homer is a parent? {isParent}");

Dictionary<string, object> namedArgs = new Dictionary<string, object>
{
	{ "child", interpreter.GetLocalPrototype("Bart") }
};
bool isParentOfBart = (bool)interpreter.RunMethodAsObject("Homer", "HasChild", namedArgs);
Console.WriteLine($"Homer is Bart's parent? {isParentOfBart}");
```
See the unit tests under **Ontology.Tests** and acceptance tests under
**Ontology.Tests.Integration** for examples on how to load
the ontology, query concepts and register custom behaviours.

---

## 📦 Repository layout

```
├─ repos/
│  ├─ ontology-core/
│  │  ├─ Ontology/
│  │  ├─ Ontology.Parsers/
│  │  ├─ Ontology.Simulation/
│  │  ├─ Ontology.Tests/
│  │  ├─ Ontology.Tests.Integration/
│  │  ├─ Ontology.Core.sln
│  │  ├─ Deploy/
│  │  ├─ lib/
│  │  └─ Scripts/
│  ├─ protoscript-core/
│  │  ├─ ProtoScript/
│  │  ├─ ProtoScript.Parsers/
│  │  ├─ ProtoScript.Interpretter/
│  │  ├─ ProtoScript.CLI/
│  │  ├─ ProtoScript.CLI.Validation/
│  │  ├─ ProtoScript.Workbench.Api/
│  │  ├─ ProtoScript.Workbench.Web/
│  │  ├─ Ontology.GraphInduction/
│  │  ├─ ProtoScript.Core.sln
│  │  ├─ ProtoScript.Workbench.sln
│  │  ├─ Deploy/
│  │  ├─ lib/
│  │  └─ Scripts/
│  └─ buffaly-nlu/
│     ├─ Buffaly.NLU/
│     ├─ Buffaly.NLU.Tagger/
│     ├─ ProtoScript.Extensions/
│     ├─ Buffaly.Ontology.Portal/
│     ├─ Buffaly.NLU.sln
│     ├─ Deploy/
│     ├─ lib/
│     └─ Scripts/
├─ Scripts/                 # Legacy root scripts
├─ examples/
│  ├─ HelloWorld/
│  └─ Simpsons/
├─ tests/
│  └─ archive/
│     └─ spikes/
└─ lib/
```

## 🧩 Solution split

- `repos/ontology-core/Ontology.Core.sln` for ontology core + tests.
- `repos/protoscript-core/ProtoScript.Core.sln` for ProtoScript language/compiler/CLI and graph induction.
- `repos/protoscript-core/ProtoScript.Workbench.sln` for non-NLU workbench API + standalone editor web app.
- `repos/buffaly-nlu/Buffaly.NLU.sln` for NLU/tagger and portal-hosted NLU workflows.

Cross-repo dependencies are DLL-based through each repo-local `Deploy/` folder and synchronized with `Scripts/update_dlls.bat`.

## 📚 Samples
- [HelloWorld](examples/HelloWorld/README.md)
- [Simpsons](examples/Simpsons/Simpsons.md)
---

## 🛠 Building and testing

Build each repo solution from the workspace root:

```bash
dotnet build repos/ontology-core/Ontology.Core.sln --no-restore /m:1
dotnet build repos/protoscript-core/ProtoScript.Core.sln --no-restore /m:1
dotnet build repos/protoscript-core/ProtoScript.Workbench.sln --no-restore /m:1
dotnet build repos/buffaly-nlu/Buffaly.NLU.sln --no-restore /m:1
```

Refresh external/shared DLLs before build:

```bash
repos\ontology-core\Scripts\update_dlls.bat
repos\protoscript-core\Scripts\update_dlls.bat
repos\buffaly-nlu\Scripts\update_dlls.bat
```

Run unit tests (default):

```bash
dotnet test repos/ontology-core/Ontology.Tests/Ontology.Tests.csproj
```

Run integration tests (explicit):

```bash
dotnet test repos/ontology-core/Ontology.Tests.Integration/Ontology.Tests.Integration.csproj --filter "TestCategory=Integration"
```

See `docs/testing/README.md` for full test inventory, classifications, and commands.

---

## 📖 Documentation

- [ProtoScript reference manual (modularized)](docs/ProtoScript/README.md)
- [ProtoScript CLI usage](docs/ProtoScript.CLI.Usage.md)
- [ProtoScript CLI validation spec](docs/ProtoScript.CLI.Validation.Spec.md)

---

## 🛡 Licence

Buffaly.Ontology is released under the **GNU General Public License v3.0**.

See [`LICENSE`](LICENSE) for the full text.

---

## 🏥 Need help with medical ontologies?

We've spent years deploying explainable, neurosymbolic AI in clinical settings
(ICD-10, SNOMED, CPT, LOINC, you name it).
If you'd like guidance or custom development, drop us a line at **[support@intelligencefactory.ai](mailto:support@intelligencefactory.ai)** or visit **https://intelligencefactory.ai**.

---

*© 2026 Intelligence Factory, LLC* - *Safe, controlled and understandable AI for mission-critical domains*
