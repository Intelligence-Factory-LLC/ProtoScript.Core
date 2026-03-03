using System.Diagnostics;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class NativeValuePrototypeCrashRepro_Tests
	{
		[TestMethod]
		public void ToPrototype_DebugString_DoesNotCrashChildProcess()
		{
			string testBinDir = AppContext.BaseDirectory.TrimEnd('\\');
			string script = string.Join("; ",
				"$ErrorActionPreference='Stop'",
				$"Get-ChildItem '{EscapeForSingleQuotedPowerShell(testBinDir)}\\*.dll' | ForEach-Object {{ try {{ [System.Reflection.Assembly]::LoadFrom($_.FullName) | Out-Null }} catch {{}} }}",
				"[Ontology.Initializer]::Initialize()",
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
			bool exited = process.WaitForExit(15000);
			if (!exited)
			{
				try { process.Kill(true); } catch { }
				Assert.Fail("Child process timed out while invoking NativeValuePrototypes.ToPrototype(\"Debug\").");
			}

			string stdout = process.StandardOutput.ReadToEnd();
			string stderr = process.StandardError.ReadToEnd();

			Assert.AreEqual(
				0,
				process.ExitCode,
				"Child process crashed while invoking NativeValuePrototypes.ToPrototype(\"Debug\").\nSTDOUT:\n"
				+ stdout + "\nSTDERR:\n" + stderr);
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
	}
}
