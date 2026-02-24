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
		public void ParseProject_ReturnsHelpfulDiagnostic_ForUnquotedIncludePath()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				string projectPath = Path.Combine(tempDir, "Project.pts");
				System.IO.File.WriteAllText(projectPath, "include Critic/CriticOps.pts;");

				ProtoScriptValidationService service = new ProtoScriptValidationService();
				ProtoScriptValidationResponse response = service.ParseProject(new ParseProjectRequest
				{
					ProjectPath = projectPath
				});

				Assert.AreEqual(ProtoScriptValidationExitCodes.ValidationFailed, response.ExitCode);
				Assert.IsTrue(response.Diagnostics.Any(x =>
					x.Category == "parse"
					&& x.Code == "PS1001"
					&& x.Message.Contains("quoted string literal", StringComparison.OrdinalIgnoreCase)));
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
