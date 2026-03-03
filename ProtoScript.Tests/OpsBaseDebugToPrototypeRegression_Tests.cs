using System.Diagnostics;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class OpsBaseDebugToPrototypeRegression_Tests
	{
		private const string DefaultProjectPath =
			@"C:\Users\Administrator\AppData\Local\Temp\1\BuffalyBranchWorkspaceTests\a9212abb7ba54988b86b7be0d5142490\OpsBase\Project.pts";

		[TestMethod]
		public void CompileOpsBaseProject_ThenToPrototypeDebug_DoesNotCrash_OrReportsFirstFailingInclude()
		{
			string projectPath = ResolveProjectPath();
			if (!System.IO.File.Exists(projectPath))
				Assert.Inconclusive("OpsBase fixture project not found: " + projectPath);

			ChildRunResult fullRun = RunCompileAndToPrototypeInChild(projectPath);
			if (fullRun.ExitCode == 0)
				return;

			string isolation = IsolateFailingInclude(projectPath);
			Assert.Fail(
				"OpsBase compile + ToPrototype(\"Debug\") failed.\n"
				+ "Project: " + projectPath + "\n"
				+ "ExitCode: " + fullRun.ExitCode + "\n"
				+ "STDOUT:\n" + fullRun.StdOut + "\n"
				+ "STDERR:\n" + fullRun.StdErr + "\n"
				+ "Isolation:\n" + isolation);
		}

		private static string ResolveProjectPath()
		{
			string? fromEnv = Environment.GetEnvironmentVariable("PROTOSCRIPT_OPSBASE_PROJECT_PATH");
			return string.IsNullOrWhiteSpace(fromEnv) ? DefaultProjectPath : fromEnv;
		}

		private static string IsolateFailingInclude(string projectPath)
		{
			string projectDir = Path.GetDirectoryName(projectPath)!;
			string[] lines = System.IO.File.ReadAllLines(projectPath);
			List<string> includeLines = lines
				.Where(static l => l.TrimStart().StartsWith("include ", StringComparison.OrdinalIgnoreCase))
				.ToList();

			if (includeLines.Count == 0)
				return "No include lines found in project file.";

			for (int i = 1; i <= includeLines.Count; i++)
			{
				string tempProject = Path.Combine(projectDir, $"__codex_isolation_{i}.pts");
				try
				{
					System.IO.File.WriteAllText(tempProject, string.Join(Environment.NewLine, includeLines.Take(i)) + Environment.NewLine);
					ChildRunResult run = RunCompileAndToPrototypeInChild(tempProject);
					if (run.ExitCode != 0)
					{
						return "First failing include prefix size: " + i + "\n"
							+ "Include line: " + includeLines[i - 1] + "\n"
							+ "Temp project: " + tempProject + "\n"
							+ "ExitCode: " + run.ExitCode + "\n"
							+ "STDOUT:\n" + run.StdOut + "\n"
							+ "STDERR:\n" + run.StdErr;
					}
				}
				finally
				{
					if (System.IO.File.Exists(tempProject))
						System.IO.File.Delete(tempProject);
				}
			}

			return "No failing include prefix found. Failure may require full include graph or runtime state.";
		}

		private static ChildRunResult RunCompileAndToPrototypeInChild(string projectPath)
		{
			string testBinDir = AppContext.BaseDirectory.TrimEnd('\\');
			string script = string.Join("; ",
				"$ErrorActionPreference='Stop'",
				$"$project='{EscapeForSingleQuotedPowerShell(projectPath)}'",
				$"Get-ChildItem '{EscapeForSingleQuotedPowerShell(testBinDir)}\\*.dll' | ForEach-Object {{ try {{ [System.Reflection.Assembly]::LoadFrom($_.FullName) | Out-Null }} catch {{}} }}",
				"[Ontology.Initializer]::Initialize()",
				"$c = New-Object ProtoScript.Interpretter.Compiler",
				"$c.Initialize()",
				"$null = $c.CompileProject($project)",
				"[Ontology.NativeValuePrototypes]::ToPrototype('Debug') | Out-Null",
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
			bool exited = process.WaitForExit(60000);
			if (!exited)
			{
				try { process.Kill(true); } catch { }
				return new ChildRunResult(-1, string.Empty, "Timed out after 60 seconds.");
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
				else
				{
					if (IsOnPath(candidate))
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

			string[] segments = path.Split(Path.PathSeparator);
			foreach (string segment in segments)
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
