using ProtoScript.Interpretter;
using System.IO;

namespace ProtoScript.Tests.Integration
{
	[TestClass]
	public sealed class GettingStarted_EndToEndTests
	{
		[TestInitialize]
		public void Init()
		{
			Ontology.Initializer.Initialize();
		}

		private static string ReadProjectScript(string fileName)
		{
			string projectsDirectory = ResolveProjectsDirectory();
			string path = Path.Combine(projectsDirectory, fileName);
			if (!System.IO.File.Exists(path))
			{
				throw new FileNotFoundException("Could not locate project script file.", path);
			}

			return System.IO.File.ReadAllText(path);
		}

		private static string ResolveProjectsDirectory()
		{
			DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
			while (directory != null)
			{
				string splitRepoPath = Path.Combine(directory.FullName, "repos", "buffaly-nlu", "Buffaly.Ontology.Portal", "wwwroot", "projects");
				if (Directory.Exists(splitRepoPath))
				{
					return splitRepoPath;
				}

				string splitStandalonePath = Path.Combine(directory.FullName, "buffaly-nlu", "Buffaly.Ontology.Portal", "wwwroot", "projects");
				if (Directory.Exists(splitStandalonePath))
				{
					return splitStandalonePath;
				}

				string legacyPath = Path.Combine(directory.FullName, "Buffaly.Ontology.Portal", "wwwroot", "projects");
				if (Directory.Exists(legacyPath))
				{
					return legacyPath;
				}

				directory = directory.Parent;
			}

			throw new DirectoryNotFoundException("Could not locate Buffaly.Ontology.Portal project scripts directory.");
		}

		private static object? RunMain(string script, string methodName)
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(script);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			return interpretter.RunMethodAsObject(null, methodName, new List<object>());
		}

		// Purpose: Validate that the HelloWorld sample project executes end-to-end from source file content.
		[TestMethod]
		[TestCategory("Integration")]
		[TestCategory("EndToEnd")]
		[TestProperty("Category", "Integration")]
		public void InterpretHelloWorld_ProjectMain_ExecutesSuccessfully()
		{
			string script = ReadProjectScript("hello.pts");
			object? result = RunMain(script, "main");
			Assert.IsNotNull(result, "HelloWorld main() should produce a value.");
		}

		// Purpose: Validate that Simpsons sample definitions can be loaded and resolved end-to-end.
		[TestMethod]
		[TestCategory("Integration")]
		[TestCategory("EndToEnd")]
		[TestProperty("Category", "Integration")]
		public void InterpretSimpsons_ProjectLookup_ExecutesSuccessfully()
		{
			string script = ReadProjectScript("Simpsons.pts") + "\nfunction __acceptance_main() : Prototype { return SimpsonsOntology.Bart; }\n";
			object? result = RunMain(script, "__acceptance_main");
			Assert.IsNotNull(result, "Simpsons lookup should resolve SimpsonsOntology.Bart.");
		}
	}
}
