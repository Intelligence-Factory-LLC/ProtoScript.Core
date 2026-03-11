using ProtoScript.Extensions;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class WorkbenchBestEffortDefault_Tests
	{
		[TestInitialize]
		public void Setup()
		{
			Initializer.Initialize();
			ProtoScriptWorkbench.Reset();
		}

		[TestMethod]
		public void CompileCodeWithProject_DefaultsToBestEffortAndSkipsBrokenFile()
		{
			string tempDir = CreateTempDirectory();
			bool allowPrecompiledOriginal = Settings.AllowPrecompiled;
			Settings.AllowPrecompiled = true;
			try
			{
				System.IO.File.WriteAllText(Path.Combine(tempDir, "Project.pts"),
@"include ""Broken.pts"";
include ""Healthy.pts"";");

				System.IO.File.WriteAllText(Path.Combine(tempDir, "Broken.pts.json"), "not-valid-precompiled-json");
				System.IO.File.WriteAllText(Path.Combine(tempDir, "Healthy.pts"),
@"prototype HealthySkill
{
	function Execute() : string
	{
		return ""ok"";
	}
}");

				List<Diagnostic> diagnostics = ProtoScriptWorkbench.CompileCodeWithProject(string.Empty, Path.Combine(tempDir, "Project.pts"));

				Assert.IsTrue(
					diagnostics.Any(x => (x.Message ?? string.Empty).Contains("Best-effort: skipped file", StringComparison.OrdinalIgnoreCase)),
					"Expected workbench compile to skip broken file in default best-effort mode.");
			}
			finally
			{
				Settings.AllowPrecompiled = allowPrecompiledOriginal;
				DeleteDirectory(tempDir);
				ProtoScriptWorkbench.Reset();
			}
		}

		private static string CreateTempDirectory()
		{
			string path = Path.Combine(Path.GetTempPath(), "ProtoScriptWorkbenchBestEffort_" + Guid.NewGuid().ToString("N"));
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
