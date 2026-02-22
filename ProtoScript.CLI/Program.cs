using System.Text.Json;
using System.Text.Json.Serialization;
using ProtoScript.CLI.Validation;

namespace ProtoScript.CLI
{
	internal static class Program
	{
		private static readonly HashSet<string> HelpOptionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"-h",
			"--help",
			"help"
		};

		private static int Main(string[] args)
		{
			if (args.Length == 0 || HelpOptionNames.Contains(args[0]))
			{
				Console.WriteLine(GetUsageText());
				return ProtoScriptValidationExitCodes.Success;
			}

			string command = args[0];
			ProtoScriptValidationService service = new ProtoScriptValidationService();
			ProtoScriptValidationResponse response;

			try
			{
				response = ExecuteCommand(service, command, args);
			}
			catch (Exception err)
			{
				response = CreateErrorResponse(command, err);
			}

			WriteResponse(response);
			return response.ExitCode;
		}

		private static ProtoScriptValidationResponse ExecuteCommand(ProtoScriptValidationService service, string command, string[] args)
		{
			switch (command.ToLowerInvariant())
			{
				case "parse-project":
					EnsureArgumentCount(command, args, 2, 2);
					return service.ParseProject(new ParseProjectRequest
					{
						ProjectPath = args[1]
					});

				case "compile-project":
					EnsureArgumentCount(command, args, 2, 2);
					return service.CompileProject(new CompileProjectRequest
					{
						ProjectPath = args[1]
					});

				case "interpret-project":
					EnsureArgumentCount(command, args, 2, 3);
					return service.InterpretProject(new InterpretProjectRequest
					{
						ProjectPath = args[1],
						Expression = args.Length >= 3 ? args[2] : "0"
					});

				default:
					throw new ArgumentException("Unknown command: " + command);
			}
		}

		private static void EnsureArgumentCount(string command, string[] args, int minCount, int maxCount)
		{
			if (args.Length < minCount || args.Length > maxCount)
			{
				throw new ArgumentException("Invalid arguments for " + command + ". See usage.");
			}
		}

		private static ProtoScriptValidationResponse CreateErrorResponse(string command, Exception err)
		{
			ProtoScriptValidationResponse response = new ProtoScriptValidationResponse
			{
				Command = string.IsNullOrWhiteSpace(command) ? "unknown" : command,
				RequestedAtUtc = DateTime.UtcNow,
				Success = false
			};

			string category = "config";
			string code = "PS9002";
			int exitCode = ProtoScriptValidationExitCodes.InternalError;

			if (err is ArgumentException)
			{
				category = "config";
				code = "PS9001";
				exitCode = ProtoScriptValidationExitCodes.InvalidArguments;
			}
			else if (err is IOException || err is UnauthorizedAccessException)
			{
				category = "io";
				code = "PS8002";
				exitCode = ProtoScriptValidationExitCodes.IoFailure;
			}

			response.ExitCode = exitCode;
			response.Diagnostics.Add(new ProtoScriptValidationDiagnostic
			{
				Severity = "error",
				Category = category,
				Code = code,
				Message = err.Message,
				ExceptionType = err.GetType().FullName
			});
			response.Summary.ErrorCount = 1;

			return response;
		}

		private static void WriteResponse(ProtoScriptValidationResponse response)
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				WriteIndented = true,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};
			string json = JsonSerializer.Serialize(response, options);
			Console.WriteLine(json);
		}

		private static string GetUsageText()
		{
			string[] lines = new[]
			{
				"ProtoScript CLI",
				"",
				"Usage:",
				"  protoscript-cli parse-project <projectPath>",
				"  protoscript-cli compile-project <projectPath>",
				"  protoscript-cli interpret-project <projectPath> [expression]",
				"",
				"Examples:",
				"  protoscript-cli parse-project \"C:\\dev\\Project\\Project.pts\"",
				"  protoscript-cli compile-project \"C:\\dev\\Project\\Project.pts\"",
				"  protoscript-cli interpret-project \"C:\\dev\\Project\\Project.pts\" \"Ping()\""
			};

			return string.Join(Environment.NewLine, lines);
		}
	}
}
