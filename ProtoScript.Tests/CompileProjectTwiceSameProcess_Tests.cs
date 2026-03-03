using System.Diagnostics;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class CompileProjectTwiceSameProcess_Tests
	{
		[TestMethod]
		public void CompileSingleFileProjectTwiceInSameProcess_DoesNotCrash()
		{
			string tempDir = Path.Combine(Path.GetTempPath(), "ProtoScript_DoubleCompile_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			try
			{
				WriteReproFiles(tempDir);
				ChildRunResult run = RunCompileTwiceInChild(Path.Combine(tempDir, "Project.pts"));

				Assert.AreEqual(
					0,
					run.ExitCode,
					"Child process failed while compiling same project twice.\nSTDOUT:\n"
					+ run.StdOut + "\nSTDERR:\n" + run.StdErr);
			}
			finally
			{
				if (Directory.Exists(tempDir))
					Directory.Delete(tempDir, true);
			}
		}

		private static void WriteReproFiles(string dir)
		{
			const string singleFile = """
reference Ontology Ontology;
import Ontology Ontology.Prototype Prototype;

prototype BaseObject;

prototype SemanticEntityBase : BaseObject
{
}

prototype LearnedAction
{
}

prototype SemanticProgram
{
	function InfinitivePhrase(Prototype prototype, string infinitive) : void
	{
		// no-op for repro
	}

	function Directive(Prototype prototype, string directive) : void
	{
		// no-op for repro
	}
}

function SemanticEntity(Prototype prototype, string semanticEntity) : void
{
	// no-op for repro
}

prototype ProtoScriptAction : LearnedAction
{
	String InfinitivePhrase = new String();
	String Description = new String();
}

prototype OpsAction : ProtoScriptAction;

prototype ToExecuteCommandLineOperation : OpsAction
{
	function Execute(string command, string commandLineArguments) : string
	{
		return "ok";
	}
}
""";

			const string projectFile = "include \"SingleFileCrash.pts\";";

			System.IO.File.WriteAllText(Path.Combine(dir, "SingleFileCrash.pts"), singleFile);
			System.IO.File.WriteAllText(Path.Combine(dir, "Project.pts"), projectFile);
		}

		private static ChildRunResult RunCompileTwiceInChild(string projectPath)
		{
			string testBinDir = AppContext.BaseDirectory.TrimEnd('\\');
			string script = string.Join("; ",
				"$ErrorActionPreference='Stop'",
				$"$project='{EscapeForSingleQuotedPowerShell(projectPath)}'",
				$"Get-ChildItem '{EscapeForSingleQuotedPowerShell(testBinDir)}\\*.dll' | ForEach-Object {{ try {{ [System.Reflection.Assembly]::LoadFrom($_.FullName) | Out-Null }} catch {{}} }}",
				"[Ontology.Initializer]::Initialize()",
				"$c1 = New-Object ProtoScript.Interpretter.Compiler",
				"$c1.Initialize()",
				"$null = $c1.CompileProject($project)",
				"$c2 = New-Object ProtoScript.Interpretter.Compiler",
				"$c2.Initialize()",
				"$null = $c2.CompileProject($project)",
				"Write-Output 'ok'");

			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = GetPowerShellExecutable(),
				Arguments = "-NoProfile -NonInteractive -Command \"" + script.Replace("\"", "\\\"") + "\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			using Process process = Process.Start(psi)!;
			bool exited = process.WaitForExit(30000);
			if (!exited)
			{
				try { process.Kill(true); } catch { }
				return new ChildRunResult(-1, string.Empty, "Timed out after 30 seconds.");
			}

			string stdout = process.StandardOutput.ReadToEnd();
			string stderr = process.StandardError.ReadToEnd();
			return new ChildRunResult(process.ExitCode, stdout, stderr);
		}

		private static string EscapeForSingleQuotedPowerShell(string input)
		{
			return input.Replace("'", "''");
		}

		private static string GetPowerShellExecutable()
		{
			string[] candidates =
			{
				"pwsh",
				"pwsh.exe",
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"),
				"powershell",
				"powershell.exe"
			};

			foreach (string candidate in candidates)
			{
				if (Path.IsPathRooted(candidate))
				{
					if (System.IO.File.Exists(candidate))
						return candidate;
				}
				else if (IsOnPath(candidate))
				{
					return candidate;
				}
			}

			return "pwsh";
		}

		private static bool IsOnPath(string executableName)
		{
			string? path = Environment.GetEnvironmentVariable("PATH");
			if (string.IsNullOrWhiteSpace(path))
				return false;

			foreach (string segment in path.Split(Path.PathSeparator))
			{
				if (string.IsNullOrWhiteSpace(segment))
					continue;

				string fullPath = Path.Combine(segment, executableName);
				if (System.IO.File.Exists(fullPath))
					return true;
			}

			return false;
		}

		private readonly record struct ChildRunResult(int ExitCode, string StdOut, string StdErr);
	}
}
