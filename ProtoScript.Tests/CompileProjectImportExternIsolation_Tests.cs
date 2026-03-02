using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class CompileProjectImportExternIsolation_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void CompileProject_IncludeReferenceImportExtern_DoesNotThrowOpaqueUnexpected()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				WriteProjectFiles(tempDir, malformedImportAlias: false);

				Compiler compiler = new Compiler();
				compiler.Initialize();

				try
				{
					compiler.CompileProject(Path.Combine(tempDir, "Project.pts"));
				}
				catch (ProtoScriptCompilerException ex)
				{
					Assert.IsFalse(
						ex.Explanation?.Contains("Compilation failed during DefinePrototypes: Exception: Unexpected", StringComparison.OrdinalIgnoreCase) ?? false,
						$"Expected actionable diagnostics, got opaque explanation: {ex.Explanation}");
					throw;
				}
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		[TestMethod]
		public void CompileProject_MalformedImportAlias_ThrowsSpecificImportParseError()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				WriteProjectFiles(tempDir, malformedImportAlias: true);
				string projectPath = Path.Combine(tempDir, "Project.pts");

				ProtoScriptParsingException ex = Assert.ThrowsException<ProtoScriptParsingException>(() =>
				{
					Compiler compiler = new Compiler();
					compiler.Initialize();
					compiler.CompileProject(projectPath);
				});

				string explanation = ex.Explanation ?? string.Empty;
				Assert.IsTrue(
					ex.Expected.Contains("import alias", StringComparison.OrdinalIgnoreCase)
					|| explanation.Contains("import", StringComparison.OrdinalIgnoreCase)
					|| ex.Expected.Contains("identifier", StringComparison.OrdinalIgnoreCase),
					$"Expected import-specific parse error but got Expected='{ex.Expected}', Explanation='{ex.Explanation}'");
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		private static void WriteProjectFiles(string tempDir, bool malformedImportAlias)
		{
			string importLine = malformedImportAlias
				? "import Ontology.Simulation On"
				: "import Ontology.Simulation OntologySimulation;";

			System.IO.File.WriteAllText(
				Path.Combine(tempDir, "Project.pts"),
@"include Imports.pts;
include Skill.pts;");

			System.IO.File.WriteAllText(
				Path.Combine(tempDir, "Imports.pts"),
$@"reference Ontology Ontology;
reference Ontology.Simulation Ontology.Simulation;
reference ProtoScript.Interpretter ProtoScript.Interpretter;
reference BasicUtilities BasicUtilities;
reference Ontology.Agents Ontology.Agents;

{importLine}
import Ontology.Agents OntologyAgents;

extern IOpsAgentRuntimeHost _opsAgent;");

			System.IO.File.WriteAllText(
				Path.Combine(tempDir, "Skill.pts"),
@"prototype MetaActionSkill extends OpsAction {
  string Execute() {
    return ""ok"";
  }
}");
		}

		private static string CreateTempDirectory()
		{
			string path = Path.Combine(Path.GetTempPath(), "ProtoScriptCompileProject_ImportExtern_Isolation_" + Guid.NewGuid().ToString("N"));
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
