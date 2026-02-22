namespace ProtoScript.CLI.Validation
{
	public static class ProtoScriptValidationExitCodes
	{
		public const int Success = 0;
		public const int ValidationFailed = 1;
		public const int InvalidArguments = 2;
		public const int IoFailure = 3;
		public const int Timeout = 4;
		public const int InternalError = 5;
	}

	public class ParseProjectRequest
	{
		public string ProjectPath { get; set; } = string.Empty;
	}

	public class CompileProjectRequest
	{
		public string ProjectPath { get; set; } = string.Empty;
	}

	public class InterpretProjectRequest
	{
		public string ProjectPath { get; set; } = string.Empty;
		public string Expression { get; set; } = "0";
	}

	public class ProtoScriptValidationResponse
	{
		public string SchemaVersion { get; set; } = "1.0";
		public string Command { get; set; } = string.Empty;
		public DateTime RequestedAtUtc { get; set; }
		public long DurationMs { get; set; }
		public bool Success { get; set; }
		public int ExitCode { get; set; } = ProtoScriptValidationExitCodes.Success;
		public ProtoScriptValidationProjectInfo? Project { get; set; }
		public ProtoScriptValidationSummary Summary { get; set; } = new ProtoScriptValidationSummary();
		public List<ProtoScriptValidationDiagnostic> Diagnostics { get; set; } = new List<ProtoScriptValidationDiagnostic>();
	}

	public class ProtoScriptValidationProjectInfo
	{
		public string? ProjectFile { get; set; }
	}

	public class ProtoScriptValidationSummary
	{
		public int FileCount { get; set; }
		public int ErrorCount { get; set; }
		public int RuntimeErrorCount { get; set; }
	}

	public class ProtoScriptValidationDiagnostic
	{
		public string Severity { get; set; } = "error";
		public string Category { get; set; } = "parse";
		public string Code { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
		public string? File { get; set; }
		public int? Cursor { get; set; }
		public int? Length { get; set; }
		public string? ExceptionType { get; set; }
	}
}
