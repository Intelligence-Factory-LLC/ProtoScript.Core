using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class StringLiteralEscaping_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		private static object? RunGlobalFunction(string code, string methodName)
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			return interpretter.RunMethodAsObject(null, methodName, new List<object>());
		}

		// Purpose: Verbatim strings should preserve backslashes and decode doubled quotes like C#.
		[TestMethod]
		public void VerbatimString_MatchesCSharpEscapingRules()
		{
			string code =
"function main() : string\r\n" +
"{\r\n" +
"	string path = @\"C:\\Windows\\System32\\cmd.exe\";\r\n" +
"	string args = @\"/c echo \"\"hello world\"\"\";\r\n" +
"	return path + \"|\" + args;\r\n" +
"}\r\n";

			object? result = RunGlobalFunction(code, "main");
			Assert.AreEqual("C:\\Windows\\System32\\cmd.exe|/c echo \"hello world\"", result);
		}

		// Purpose: Escaped string literals and verbatim literals should produce identical values.
		[TestMethod]
		public void EscapedAndVerbatimLiterals_AreEquivalent()
		{
			string code =
"function PathVerbatim() : string\r\n" +
"{\r\n" +
"	return @\"C:\\Windows\\System32\\cmd.exe\";\r\n" +
"}\r\n" +
"\r\n" +
"function PathEscaped() : string\r\n" +
"{\r\n" +
"	return \"C:\\\\Windows\\\\System32\\\\cmd.exe\";\r\n" +
"}\r\n" +
"\r\n" +
"function ArgsVerbatim() : string\r\n" +
"{\r\n" +
"	return @\"/c echo \"\"hello world\"\"\";\r\n" +
"}\r\n" +
"\r\n" +
"function ArgsEscaped() : string\r\n" +
"{\r\n" +
"	return \"/c echo \\\"hello world\\\"\";\r\n" +
"}\r\n";

			object? pathVerbatim = RunGlobalFunction(code, "PathVerbatim");
			object? pathEscaped = RunGlobalFunction(code, "PathEscaped");
			object? argsVerbatim = RunGlobalFunction(code, "ArgsVerbatim");
			object? argsEscaped = RunGlobalFunction(code, "ArgsEscaped");

			string? pathVerbatimString = ValueConversions.GetAs(pathVerbatim, typeof(string)) as string;
			string? pathEscapedString = ValueConversions.GetAs(pathEscaped, typeof(string)) as string;
			string? argsVerbatimString = ValueConversions.GetAs(argsVerbatim, typeof(string)) as string;
			string? argsEscapedString = ValueConversions.GetAs(argsEscaped, typeof(string)) as string;

			Assert.AreEqual("C:\\Windows\\System32\\cmd.exe", pathVerbatimString);
			Assert.AreEqual("C:\\Windows\\System32\\cmd.exe", pathEscapedString);
			Assert.AreEqual(pathEscapedString, pathVerbatimString);

			Assert.AreEqual("/c echo \"hello world\"", argsVerbatimString);
			Assert.AreEqual("/c echo \"hello world\"", argsEscapedString);
			Assert.AreEqual(argsEscapedString, argsVerbatimString);
		}

		// Purpose: Ensure verbatim C#-style literals work when passed to RunCommandLine without extra escaping.
		[TestMethod]
		[Timeout(10000)]
		public void RunCommandLine_WithVerbatimPathAndArgs_DoesNotRequireExtraEscaping()
		{
			string code =
"function main() : string\r\n" +
"{\r\n" +
"	string args = @\"/c echo \"\"hello world\"\"\";\r\n" +
"	string output = SystemOperations.RunCommandLine(@\"C:\\Windows\\System32\\cmd.exe\", args);\r\n" +
"	return args;\r\n" +
"}\r\n";

			object? result = RunGlobalFunction(code, "main");
			string? argsUsed = ValueConversions.GetAs(result, typeof(string)) as string;
			Assert.AreEqual("/c echo \"hello world\"", argsUsed);
		}
	}
}
