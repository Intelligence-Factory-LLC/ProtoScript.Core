using System.Diagnostics;
using ProtoScript.Extensions;

namespace ProtoScript.CLI.Validation
{
	public class ProtoScriptValidationService
	{
		public ProtoScriptValidationResponse ParseProject(ParseProjectRequest request)
		{
			DateTime requestedAtUtc = DateTime.UtcNow;
			Stopwatch stopwatch = Stopwatch.StartNew();
			ProtoScriptValidationResponse response = CreateResponse("parse-project", requestedAtUtc);

			try
			{
				if (string.IsNullOrWhiteSpace(request.ProjectPath))
				{
					AddInvalidArgumentsDiagnostic(response, "Missing project path.");
					return CompleteResponse(response, stopwatch);
				}

				string projectPath = Path.GetFullPath(request.ProjectPath);
				List<string> files = ProtoScriptWorkbench.LoadProject(projectPath);

				response.Project = new ProtoScriptValidationProjectInfo
				{
					ProjectFile = projectPath
				};
				response.Summary.FileCount = files.Count;
			}
			catch (UnauthorizedAccessException err)
			{
				AddIoDiagnostic(response, err.Message, request.ProjectPath, "PS8002");
			}
			catch (IOException err)
			{
				AddIoDiagnostic(response, err.Message, request.ProjectPath, "PS8003");
			}
			catch (Exception err)
			{
				AddDiagnostic(response, "parse", "PS1001", err.Message, request.ProjectPath, null, null, err);
			}

			return CompleteResponse(response, stopwatch);
		}

		public ProtoScriptValidationResponse CompileProject(CompileProjectRequest request)
		{
			DateTime requestedAtUtc = DateTime.UtcNow;
			Stopwatch stopwatch = Stopwatch.StartNew();
			ProtoScriptValidationResponse response = CreateResponse("compile-project", requestedAtUtc);

			try
			{
				if (string.IsNullOrWhiteSpace(request.ProjectPath))
				{
					AddInvalidArgumentsDiagnostic(response, "Missing project path.");
					return CompleteResponse(response, stopwatch);
				}

				string projectPath = Path.GetFullPath(request.ProjectPath);
				List<Diagnostic> diagnostics = ProtoScriptWorkbench.CompileCodeWithProject(string.Empty, projectPath);

				foreach (Diagnostic diagnostic in diagnostics)
				{
					string category = string.Equals(diagnostic.Type, "Parsing", StringComparison.OrdinalIgnoreCase)
						? "parse"
						: "compile";
					string code = category == "parse" ? "PS1001" : "PS2001";
					AddDiagnostic(
						response,
						category,
						code,
						diagnostic.Message ?? "Compilation error.",
						diagnostic.Info?.File ?? projectPath,
						diagnostic.Info?.StartingOffset,
						diagnostic.Info == null ? null : Math.Max(1, diagnostic.Info.Length),
						null);
				}

				response.Project = new ProtoScriptValidationProjectInfo
				{
					ProjectFile = projectPath
				};
				response.Summary.FileCount = TryGetProjectFileCount(projectPath);
			}
			catch (UnauthorizedAccessException err)
			{
				AddIoDiagnostic(response, err.Message, request.ProjectPath, "PS8002");
			}
			catch (IOException err)
			{
				AddIoDiagnostic(response, err.Message, request.ProjectPath, "PS8003");
			}
			catch (Exception err)
			{
				AddInternalDiagnostic(response, err);
			}

			return CompleteResponse(response, stopwatch);
		}

		public ProtoScriptValidationResponse InterpretProject(InterpretProjectRequest request)
		{
			DateTime requestedAtUtc = DateTime.UtcNow;
			Stopwatch stopwatch = Stopwatch.StartNew();
			ProtoScriptValidationResponse response = CreateResponse("interpret-project", requestedAtUtc);

			try
			{
				if (string.IsNullOrWhiteSpace(request.ProjectPath))
				{
					AddInvalidArgumentsDiagnostic(response, "Missing project path.");
					return CompleteResponse(response, stopwatch);
				}

				string projectPath = Path.GetFullPath(request.ProjectPath);
				string expression = string.IsNullOrWhiteSpace(request.Expression) ? "0" : request.Expression;

				ProtoScriptWorkbench.TaggingSettings settings = new ProtoScriptWorkbench.TaggingSettings
				{
					Project = projectPath,
					SessionKey = projectPath
				};
				ProtoScriptWorkbench.TagImmediateResult result = ProtoScriptWorkbench.InterpretImmediate(projectPath, expression, settings);

				if (!string.IsNullOrWhiteSpace(result.Error))
				{
					AddDiagnostic(
						response,
						"runtime",
						"PS3001",
						result.Error,
						result.ErrorStatement?.File ?? projectPath,
						result.ErrorStatement?.StartingOffset,
						result.ErrorStatement == null ? null : Math.Max(1, result.ErrorStatement.Length),
						null);
				}

				response.Project = new ProtoScriptValidationProjectInfo
				{
					ProjectFile = projectPath
				};
				response.Summary.FileCount = TryGetProjectFileCount(projectPath);
			}
			catch (UnauthorizedAccessException err)
			{
				AddIoDiagnostic(response, err.Message, request.ProjectPath, "PS8002");
			}
			catch (IOException err)
			{
				AddIoDiagnostic(response, err.Message, request.ProjectPath, "PS8003");
			}
			catch (Exception err)
			{
				AddInternalDiagnostic(response, err);
			}

			return CompleteResponse(response, stopwatch);
		}

		private static int TryGetProjectFileCount(string projectPath)
		{
			try
			{
				List<string> files = ProtoScriptWorkbench.LoadProject(projectPath);
				return files.Count;
			}
			catch
			{
				return 0;
			}
		}

		private static ProtoScriptValidationResponse CreateResponse(string command, DateTime requestedAtUtc)
		{
			ProtoScriptValidationResponse response = new ProtoScriptValidationResponse
			{
				Command = command,
				RequestedAtUtc = requestedAtUtc
			};
			return response;
		}

		private static void AddDiagnostic(
			ProtoScriptValidationResponse response,
			string category,
			string code,
			string message,
			string? file,
			int? cursor,
			int? length,
			Exception? exception)
		{
			ProtoScriptValidationDiagnostic diagnostic = new ProtoScriptValidationDiagnostic
			{
				Severity = "error",
				Category = category,
				Code = code,
				Message = message,
				File = file,
				Cursor = cursor,
				Length = length,
				ExceptionType = exception?.GetType().FullName
			};
			response.Diagnostics.Add(diagnostic);
		}

		private static void AddInvalidArgumentsDiagnostic(ProtoScriptValidationResponse response, string message)
		{
			response.ExitCode = ProtoScriptValidationExitCodes.InvalidArguments;
			AddDiagnostic(response, "config", "PS9001", message, null, null, null, null);
		}

		private static void AddIoDiagnostic(ProtoScriptValidationResponse response, string message, string? file, string code)
		{
			response.ExitCode = ProtoScriptValidationExitCodes.IoFailure;
			AddDiagnostic(response, "io", code, message, file, null, null, null);
		}

		private static void AddInternalDiagnostic(ProtoScriptValidationResponse response, Exception err)
		{
			response.ExitCode = ProtoScriptValidationExitCodes.InternalError;
			AddDiagnostic(response, "config", "PS9002", err.Message, null, null, null, err);
		}

		private static ProtoScriptValidationResponse CompleteResponse(ProtoScriptValidationResponse response, Stopwatch stopwatch)
		{
			response.DurationMs = stopwatch.ElapsedMilliseconds;
			response.Summary.ErrorCount = response.Diagnostics.Count(x => string.Equals(x.Severity, "error", StringComparison.OrdinalIgnoreCase));
			response.Summary.RuntimeErrorCount = response.Diagnostics.Count(x =>
				string.Equals(x.Severity, "error", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.Category, "runtime", StringComparison.OrdinalIgnoreCase));

			if (response.ExitCode == ProtoScriptValidationExitCodes.Success)
			{
				response.ExitCode = response.Summary.ErrorCount > 0
					? ProtoScriptValidationExitCodes.ValidationFailed
					: ProtoScriptValidationExitCodes.Success;
			}

			response.Success = response.ExitCode == ProtoScriptValidationExitCodes.Success;
			return response;
		}
	}
}
