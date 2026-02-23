using ProtoScript.CLI.Validation;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class ProtoScriptCliValidationServiceTests
	{
		[TestInitialize]
		public void Setup()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void ParseProject_ReturnsSuccess_ForValidProject()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				string projectPath = Path.Combine(tempDir, "Project.pts");
				string fileA = Path.Combine(tempDir, "A.pts");
				System.IO.File.WriteAllText(projectPath, "include \"A.pts\";");
				System.IO.File.WriteAllText(fileA, "prototype Alpha;");

				ProtoScriptValidationService service = new ProtoScriptValidationService();
				ProtoScriptValidationResponse response = service.ParseProject(new ParseProjectRequest
				{
					ProjectPath = projectPath
				});

				Assert.AreEqual(ProtoScriptValidationExitCodes.Success, response.ExitCode);
				Assert.AreEqual("parse-project", response.Command);
				Assert.IsTrue(response.Summary.FileCount >= 1);
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		[TestMethod]
		public void ParseProject_ReturnsValidationFailed_ForInvalidProject()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				string projectPath = Path.Combine(tempDir, "Project.pts");
				System.IO.File.WriteAllText(projectPath,
@"function Broken() : void
{
	return;");

				ProtoScriptValidationService service = new ProtoScriptValidationService();
				ProtoScriptValidationResponse response = service.ParseProject(new ParseProjectRequest
				{
					ProjectPath = projectPath
				});

				Assert.AreEqual(ProtoScriptValidationExitCodes.ValidationFailed, response.ExitCode);
				Assert.IsTrue(response.Diagnostics.Count > 0);
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		[TestMethod]
		public void CompileProject_ReturnsCompileDiagnostic_ForUnknownType()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				string projectPath = Path.Combine(tempDir, "Project.pts");
				string fileA = Path.Combine(tempDir, "CompileError.pts");

				System.IO.File.WriteAllText(projectPath, "include \"CompileError.pts\";");
				System.IO.File.WriteAllText(fileA,
@"function TestBad() : void
{
	UnknownType x;
}");

				ProtoScriptValidationService service = new ProtoScriptValidationService();
				ProtoScriptValidationResponse response = service.CompileProject(new CompileProjectRequest
				{
					ProjectPath = projectPath
				});

				Assert.AreEqual(ProtoScriptValidationExitCodes.ValidationFailed, response.ExitCode);
				Assert.IsTrue(response.Diagnostics.Any(x => x.Category == "compile" || x.Category == "parse"));
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		[TestMethod]
		public void InterpretProject_RunsExpressionSuccessfully()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				string projectPath = Path.Combine(tempDir, "Project.pts");
				System.IO.File.WriteAllText(projectPath,
@"function Ping() : Integer
{
	return 1;
}");

				ProtoScriptValidationService service = new ProtoScriptValidationService();
				ProtoScriptValidationResponse response = service.InterpretProject(new InterpretProjectRequest
				{
					ProjectPath = projectPath,
					Expression = "Ping()"
				});

				Assert.AreEqual(ProtoScriptValidationExitCodes.Success, response.ExitCode);
				Assert.AreEqual(0, response.Summary.RuntimeErrorCount);
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		[TestMethod]
		public void InterpretProject_ReturnsRuntimeDiagnostic_WhenExecutionFails()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				string projectPath = Path.Combine(tempDir, "Project.pts");
				System.IO.File.WriteAllText(projectPath,
@"function Crash() : Object
{
	Collection c = new Collection();
	return c[4];
}");

				ProtoScriptValidationService service = new ProtoScriptValidationService();
				ProtoScriptValidationResponse response = service.InterpretProject(new InterpretProjectRequest
				{
					ProjectPath = projectPath,
					Expression = "Crash()"
				});

				Assert.AreEqual(ProtoScriptValidationExitCodes.ValidationFailed, response.ExitCode);
				Assert.IsTrue(response.Summary.RuntimeErrorCount > 0);
				Assert.IsTrue(response.Diagnostics.Any(x => x.Category == "runtime"));
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
