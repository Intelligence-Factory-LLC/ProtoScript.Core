using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class DotNetParamsMethod_Tests
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

		// Purpose: Ensure params methods on .NET types resolve for a single char literal argument.
		[TestMethod]
		public void TrimEnd_WithSingleCharLiteral_CompilesAndRuns()
		{
			string code =
"function main() : string\r\n" +
"{\r\n" +
"	string value = @\"C:\\Temp\\Folder/\";\r\n" +
"	return value.TrimEnd('/');\r\n" +
"}\r\n";

			object? result = RunGlobalFunction(code, "main");
			string? strResult = ValueConversions.GetAs(result, typeof(string)) as string;
			Assert.AreEqual("C:\\Temp\\Folder", strResult);
		}

		// Purpose: Ensure params methods can consume multiple char literal arguments.
		[TestMethod]
		public void TrimEnd_WithMultipleCharLiterals_CompilesAndRuns()
		{
			string code =
"function main() : string\r\n" +
"{\r\n" +
"	string value = @\"C:\\Temp\\Folder\\\\\";\r\n" +
"	return value.TrimEnd('/', '\\\\');\r\n" +
"}\r\n";

			object? result = RunGlobalFunction(code, "main");
			string? strResult = ValueConversions.GetAs(result, typeof(string)) as string;
			Assert.AreEqual("C:\\Temp\\Folder", strResult);
		}

		// Purpose: Ensure StringWrapper receivers can bind to native .NET string methods.
		[TestMethod]
		public void TrimEnd_OnStringWrapper_CompilesAndRuns()
		{
			string code =
"function main() : string\r\n" +
"{\r\n" +
"	String value = \"C:\\\\Temp\\\\Folder/\";\r\n" +
"	return value.TrimEnd('/');\r\n" +
"}\r\n";

			object? result = RunGlobalFunction(code, "main");
			string? strResult = ValueConversions.GetAs(result, typeof(string)) as string;
			Assert.AreEqual("C:\\Temp\\Folder", strResult);
		}

		// Purpose: Ensure child-property String fields can call native .NET string methods.
		[TestMethod]
		public void TrimEnd_OnChildPropertyStringField_CompilesAndRuns()
		{
			string code =
"prototype Endpoint\r\n" +
"{\r\n" +
"	String RootUrl = new String();\r\n" +
"}\r\n" +
"\r\n" +
"partial prototype Endpoint#Local : Endpoint\r\n" +
"{\r\n" +
"	RootUrl = \"https://localhost/\";\r\n" +
"}\r\n" +
"\r\n" +
"function main() : string\r\n" +
"{\r\n" +
"	Endpoint endpoint = Endpoint#Local;\r\n" +
"	return endpoint.RootUrl.TrimEnd('/');\r\n" +
"}\r\n";

			object? result = RunGlobalFunction(code, "main");
			string? strResult = ValueConversions.GetAs(result, typeof(string)) as string;
			Assert.AreEqual("https://localhost", strResult);
		}
	}
}
