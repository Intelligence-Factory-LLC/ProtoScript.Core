using ProtoScript.Interpretter;
using ProtoScript.Interpretter.RuntimeInfo;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class CompileProjectBestEffortMode_Tests
	{
		[TestInitialize]
		public void Setup()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void CompileProject_BestEffort_SkipsFileThatThrowsAndCompilesHealthyFile()
		{
			string tempDir = CreateTempDirectory();
			bool allowPrecompiledOriginal = Settings.AllowPrecompiled;
			Settings.AllowPrecompiled = true;
			try
			{
				System.IO.File.WriteAllText(Path.Combine(tempDir, "Project.pts"),
@"include ""Broken.pts"";
include ""Healthy.pts"";");

				// Force precompiled load path for Broken.pts and make it throw during Precompiled stage.
				System.IO.File.WriteAllText(Path.Combine(tempDir, "Broken.pts.json"), "not-valid-precompiled-json");

				System.IO.File.WriteAllText(Path.Combine(tempDir, "Healthy.pts"),
@"prototype HealthySkill
{
	function Execute() : string
	{
		return ""ok"";
	}
}");

				Compiler compiler = new Compiler();
				compiler.Initialize();
				compiler.ProjectCompilationMode = Compiler.CompilationMode.BestEffort;

				compiler.CompileProject(Path.Combine(tempDir, "Project.pts"));

				Assert.IsTrue(
					compiler.DisabledFiles.Any(x => x.EndsWith("Broken.pts", StringComparison.OrdinalIgnoreCase)),
					"Expected Broken.pts to be disabled.");

				Assert.IsTrue(
					compiler.Diagnostics.Any(x => (x.Diagnostic?.Message ?? string.Empty).Contains("Best-effort: skipped file", StringComparison.OrdinalIgnoreCase)),
					"Expected best-effort skip diagnostic.");

				TypeInfo? healthyType = compiler.Symbols.GetGlobalScope().GetSymbol("HealthySkill") as TypeInfo;
				Assert.IsNotNull(healthyType, "Expected healthy file to compile and register symbols.");

				TypeInfo? brokenType = compiler.Symbols.GetGlobalScope().GetSymbol("BrokenSkill") as TypeInfo;
				Assert.IsNull(brokenType, "Expected broken file symbols to be skipped.");
			}
			finally
			{
				Settings.AllowPrecompiled = allowPrecompiledOriginal;
				DeleteDirectory(tempDir);
			}
		}

		private static string CreateTempDirectory()
		{
			string path = Path.Combine(Path.GetTempPath(), "ProtoScriptBestEffort_" + Guid.NewGuid().ToString("N"));
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
