using ProtoScript.Extensions;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class WorkbenchImmediateAnnotationExecution_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void InterpretImmediate_RunsProjectAnnotations_BeforeExpression()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				string projectPath = Path.Combine(tempDir, "Project.pts");
				System.IO.File.WriteAllText(projectPath, "include \"Imports.pts\";\ninclude \"Actions.pts\";");

				System.IO.File.WriteAllText(
					Path.Combine(tempDir, "Imports.pts"),
@"reference Ontology Ontology;
reference Ontology.Simulation Ontology.Simulation;
import Ontology Ontology.Prototype Prototype;
import Ontology.Simulation Ontology.Simulation.StringWrapper String;");

				System.IO.File.WriteAllText(
					Path.Combine(tempDir, "Actions.pts"),
@"prototype OpsAction
{
	String Description = new String();
}

prototype SemanticProgram
{
	function InfinitivePhrase(OpsAction action, string infinitive) : void
	{
		action.Description = infinitive;
	}
}

[SemanticProgram.InfinitivePhrase(""to read recent gmail emails"")]
prototype ToReadRecentGoogleWorkspaceEmails : OpsAction
{
}");

				ProtoScriptWorkbench.TaggingSettings settings = new ProtoScriptWorkbench.TaggingSettings()
				{
					Project = projectPath
				};

				ProtoScriptWorkbench.TagImmediateResult result =
					ProtoScriptWorkbench.InterpretImmediate(projectPath, "ToReadRecentGoogleWorkspaceEmails.Description", settings);

				Assert.IsTrue(string.IsNullOrWhiteSpace(result.Error), "Unexpected immediate error: " + result.Error);
				Assert.AreEqual("to read recent gmail emails", result.Result);
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		private static string CreateTempDirectory()
		{
			string path = Path.Combine(Path.GetTempPath(), "ProtoScriptWorkbenchAnno_" + Guid.NewGuid().ToString("N"));
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
