using System.Diagnostics;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class CompileProjectResetBoundaryCrashRepro_Tests
	{
		[TestMethod]
		public void CompileTwice_WithInitializerResetBetweenPasses_Succeeds()
		{
			string tempDir = Path.Combine(Path.GetTempPath(), "ProtoScript_ResetBoundaryCrash_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			try
			{
				WriteReproFiles(tempDir);
				ChildRunResult run = RunCompileResetCompileInChild(Path.Combine(tempDir, "Project.pts"));

				Assert.AreEqual(
					0,
					run.ExitCode,
					"Expected child process to compile successfully across reset boundary.\nSTDOUT:\n"
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

prototype X
{
	function Execute() : string
	{
		return "ok";
	}
}
""";

			const string projectFile = "include \"Repro.pts\";";

			System.IO.File.WriteAllText(Path.Combine(dir, "Repro.pts"), singleFile);
			System.IO.File.WriteAllText(Path.Combine(dir, "Project.pts"), projectFile);
		}

		private static ChildRunResult RunCompileResetCompileInChild(string projectPath)
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
				"[Ontology.Initializer]::ResetCache()",
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
			Task<string> stdOutTask = process.StandardOutput.ReadToEndAsync();
			Task<string> stdErrTask = process.StandardError.ReadToEndAsync();
			bool exited = process.WaitForExit(60000);
			if (!exited)
			{
				try { process.Kill(true); } catch { }
				string timedOutStdOut = stdOutTask.GetAwaiter().GetResult();
				string timedOutStdErr = stdErrTask.GetAwaiter().GetResult();
				return new ChildRunResult(-1, timedOutStdOut, "Timed out after 60 seconds.\n" + timedOutStdErr);
			}

			string stdout = stdOutTask.GetAwaiter().GetResult();
			string stderr = stdErrTask.GetAwaiter().GetResult();
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
