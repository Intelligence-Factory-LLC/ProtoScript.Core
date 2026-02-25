using ProtoScript.CLI.Validation;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class ProtoScriptCliValidationIncludeTests
	{
		[TestInitialize]
		public void Setup()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void ParseProject_ReturnsHelpfulDiagnostic_ForWhitespaceInUnquotedIncludePath()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				string projectPath = Path.Combine(tempDir, "Project.pts");
				System.IO.File.WriteAllText(projectPath, "include Path With Space/File.pts;");

				ProtoScriptValidationService service = new ProtoScriptValidationService();
				ProtoScriptValidationResponse response = service.ParseProject(new ParseProjectRequest
				{
					ProjectPath = projectPath
				});

				Assert.AreEqual(ProtoScriptValidationExitCodes.ValidationFailed, response.ExitCode);
				Assert.IsTrue(response.Diagnostics.Any(x =>
					x.Category == "parse"
					&& x.Code == "PS1001"
					&& x.Message.Contains("cannot contain whitespace", StringComparison.OrdinalIgnoreCase)));
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		[TestMethod]
		public void ParseProject_SupportsImportPathAlias_WithForwardSlashPath()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				string projectPath = Path.Combine(tempDir, "Project.pts");
				string importedFile = Path.Combine(tempDir, "Sub", "File.pts");
				Directory.CreateDirectory(Path.GetDirectoryName(importedFile)!);
				System.IO.File.WriteAllText(projectPath, "import Sub/File.pts;");
				System.IO.File.WriteAllText(importedFile, "prototype Alpha;");

				ProtoScriptValidationService service = new ProtoScriptValidationService();
				ProtoScriptValidationResponse response = service.ParseProject(new ParseProjectRequest
				{
					ProjectPath = projectPath
				});

				Assert.AreEqual(ProtoScriptValidationExitCodes.Success, response.ExitCode);
				Assert.IsTrue(response.Summary.FileCount >= 2);
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		private static string CreateTempDirectory()
		{
			string path = Path.Combine(Path.GetTempPath(), "ProtoScriptCliValidation_" + Guid.NewGuid().ToString("N"));
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
