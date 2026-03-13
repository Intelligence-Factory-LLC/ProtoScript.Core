using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class MethodEvaluationDiagnostics_Tests
	{
		[TestInitialize]
		public void Setup()
		{
			Initializer.Initialize();
		}

		// Purpose: Calling a non-function symbol should report an explicit compiler diagnostic instead of throwing NullReferenceException.
		[TestMethod]
		public void CompileProject_Strict_NonFunctionSymbolInvocation_ReportsCallableDiagnostic()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				string projectPath = Path.Combine(tempDir, "Project.pts");
				System.IO.File.WriteAllText(projectPath, "include \"Broken.pts\";");
				System.IO.File.WriteAllText(Path.Combine(tempDir, "Broken.pts"),
@"prototype BaseObject;
prototype ToGetBuffalyRuntimeModelAndReasoning : BaseObject
{
	function Execute() : string
	{
		ToGetBuffalyRuntimeModelAndReasoning();
		return ""ok"";
	}
}");

				Compiler compiler = new Compiler();
				compiler.Initialize();
				compiler.CompileProject(projectPath);

				Assert.IsTrue(
					compiler.Diagnostics.Any(x => (x.Diagnostic?.Message ?? string.Empty).Contains("is not a function", StringComparison.OrdinalIgnoreCase)),
					"Expected non-callable symbol diagnostic.");
				Assert.IsFalse(
					compiler.Diagnostics.Any(x => (x.Diagnostic?.Message ?? string.Empty).Contains("Object reference not set", StringComparison.OrdinalIgnoreCase)),
					"Object reference diagnostics should not be emitted for this case.");
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		// Purpose: Best-effort mode should skip the bad file with a useful explanation rather than surfacing a NullReferenceException.
		[TestMethod]
		public void CompileProject_BestEffort_NonFunctionSymbolInvocation_SkipsFileWithActionableReason()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				string projectPath = Path.Combine(tempDir, "Project.pts");
				System.IO.File.WriteAllText(projectPath,
@"include ""Broken.pts"";
include ""Healthy.pts"";");
				System.IO.File.WriteAllText(Path.Combine(tempDir, "Broken.pts"),
@"prototype BaseObject;
prototype ToGetBuffalyRuntimeModelAndReasoning : BaseObject
{
	function Execute() : string
	{
		ToGetBuffalyRuntimeModelAndReasoning();
		return ""ok"";
	}
}");
				System.IO.File.WriteAllText(Path.Combine(tempDir, "Healthy.pts"),
@"prototype HealthySkill : BaseObject
{
	function Execute() : string
	{
		return ""healthy"";
	}
}");

				Compiler compiler = new Compiler();
				compiler.Initialize();
				compiler.ProjectCompilationMode = Compiler.CompilationMode.BestEffort;
				compiler.CompileProject(projectPath);

				Assert.IsTrue(
					compiler.DisabledFiles.Any(x => x.EndsWith("Broken.pts", StringComparison.OrdinalIgnoreCase)),
					"Expected Broken.pts to be skipped.");
				Assert.IsTrue(
					compiler.Diagnostics.Any(x =>
						(x.Diagnostic?.Message ?? string.Empty).Contains("Best-effort: skipped file", StringComparison.OrdinalIgnoreCase)
						&& (x.Diagnostic?.Message ?? string.Empty).Contains("is not a function", StringComparison.OrdinalIgnoreCase)),
					"Expected best-effort skip reason to include callable mismatch details.");
				Assert.IsFalse(
					compiler.Diagnostics.Any(x => (x.Diagnostic?.Message ?? string.Empty).Contains("NullReferenceException", StringComparison.OrdinalIgnoreCase)),
					"Expected no NullReferenceException diagnostics for this scenario.");
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		private static string CreateTempDirectory()
		{
			string path = Path.Combine(Path.GetTempPath(), "ProtoScriptMethodEvalDiag_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(path);
			return path;
		}

		private static void DeleteDirectory(string path)
		{
			if (Directory.Exists(path))
				Directory.Delete(path, true);
		}
	}
}
