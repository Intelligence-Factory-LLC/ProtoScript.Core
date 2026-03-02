using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class CompileProjectLayoutRegression_Tests
	{
		[TestInitialize]
		public void Setup()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void CompileProject_WithIncludesImportsAndExternPrototype_Succeeds()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				WriteProjectFiles(
					tempDir,
					projectContents:
@"include ""Imports.pts"";
include ""Skill.pts"";",
					importsContents:
@"reference Ontology.Simulation Ontology.Simulation;
import Ontology.Simulation Ontology.Simulation.StringWrapper String;
extern prototype ExternalThing;
extern String RuntimeMessage;",
					skillContents:
@"prototype Skill
{
	function Echo() : String
	{
		return RuntimeMessage;
	}
}");

				Compiler compiler = new Compiler();
				compiler.Initialize();

				compiler.CompileProject(Path.Combine(tempDir, "Project.pts"));

				Assert.AreEqual(0, compiler.Diagnostics.Count);
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		[TestMethod]
		public void CompileProject_WithMalformedImportPath_ThrowsHelpfulParseError()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				WriteProjectFiles(
					tempDir,
					projectContents:
@"import Invalid Path/Skill.pts;
include ""Imports.pts"";
include ""Skill.pts"";",
					importsContents:
@"reference Ontology.Simulation Ontology.Simulation;
import Ontology.Simulation Ontology.Simulation.StringWrapper String;
extern prototype ExternalThing;
extern String RuntimeMessage;",
					skillContents:
@"prototype Skill
{
	function Echo() : String
	{
		return RuntimeMessage;
	}
}");

				Compiler compiler = new Compiler();
				compiler.Initialize();

				ProtoScript.Parsers.ProtoScriptParsingException err =
					Assert.ThrowsException<ProtoScript.Parsers.ProtoScriptParsingException>(
						() => compiler.CompileProject(Path.Combine(tempDir, "Project.pts")));

				Assert.IsNotNull(err.Expected);
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		private static void WriteProjectFiles(string tempDir, string projectContents, string importsContents, string skillContents)
		{
			System.IO.File.WriteAllText(Path.Combine(tempDir, "Project.pts"), projectContents);
			System.IO.File.WriteAllText(Path.Combine(tempDir, "Imports.pts"), importsContents);
			System.IO.File.WriteAllText(Path.Combine(tempDir, "Skill.pts"), skillContents);
		}

		private static string CreateTempDirectory()
		{
			string path = Path.Combine(Path.GetTempPath(), "ProtoScriptCompileProject_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(path);
			return path;
		}

		private static void DeleteDirectory(string path)
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}
		}
	}
}
