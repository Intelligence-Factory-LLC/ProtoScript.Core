using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class OpsAgentProjectLayoutRegression_Tests
	{
		[TestInitialize]
		public void Setup()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void CompileProject_WithOpsAgentStyleIncludes_DoesNotThrowUnexpected()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				WriteProjectFiles(
					tempDir,
					projectContents:
@"include Imports.pts;
include Skill.pts;",
					importsContents:
@"reference Ontology Ontology;
reference Ontology.Simulation Ontology.Simulation;
reference ProtoScript.Interpretter ProtoScript.Interpretter;
reference BasicUtilities BasicUtilities;
reference Ontology.Agents Ontology.Agents;

import Ontology.Simulation OntologySimulation;
import Ontology.Agents OntologyAgents;

extern IOpsAgentRuntimeHost _opsAgent;",
					skillContents:
@"prototype MetaActionSkill extends OpsAction {
  string Execute() {
    return ""ok"";
  }
}");

				Compiler compiler = new Compiler();
				compiler.Initialize();

				try
				{
					compiler.CompileProject(Path.Combine(tempDir, "Project.pts"));
				}
				catch (ProtoScriptCompilerException ex)
				{
					Assert.IsFalse(ex.Explanation.Contains("Unexpected", StringComparison.OrdinalIgnoreCase), ex.Explanation);
					throw;
				}
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		[TestMethod]
		public void CompileProject_WithTruncatedImport_ReportsActionableParsingError()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				WriteProjectFiles(
					tempDir,
					projectContents:
@"include Imports.pts;
include Skill.pts;",
					importsContents:
@"reference Ontology.Simulation Ontology.Simulation;
import Ontology.Simulation On",
					skillContents:
@"prototype MetaActionSkill {
  function Execute() : string {
    return ""ok"";
  }
}");

				Compiler compiler = new Compiler();
				compiler.Initialize();

				ProtoScriptParsingException ex = Assert.ThrowsException<ProtoScriptParsingException>(() =>
					compiler.CompileProject(Path.Combine(tempDir, "Project.pts")));

				Assert.IsTrue(
					(ex.Explanation ?? string.Empty).Contains("Import statements", StringComparison.OrdinalIgnoreCase),
					$"Expected actionable import explanation, but got: {ex.Explanation ?? "<null>"}");
				Assert.AreEqual("import alias", ex.Expected);
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
			string path = Path.Combine(Path.GetTempPath(), "ProtoScriptOpsAgentProject_" + Guid.NewGuid().ToString("N"));
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
