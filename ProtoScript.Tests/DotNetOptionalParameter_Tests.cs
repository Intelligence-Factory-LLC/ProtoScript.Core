using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class DotNetOptionalParameter_Tests
	{
		private static class OptionalTarget
		{
			public static string Format(string value, string suffix = "!")
			{
				return value + suffix;
			}

			public static string JoinWith(string head, string separator = ",", params string[] tail)
			{
				if (tail == null || tail.Length == 0)
					return head;

				return head + separator + string.Join(separator, tail);
			}

			public static string WithNullable(int? maxResults = null, bool? includeSpamTrash = null)
			{
				string left = maxResults.HasValue ? maxResults.Value.ToString() : "null";
				string right = includeSpamTrash.HasValue ? includeSpamTrash.Value.ToString().ToLowerInvariant() : "null";
				return left + "|" + right;
			}
		}

		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		private static object? RunGlobalFunction(string code, string methodName, out Compiler compiler)
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			compiler = new Compiler();
			compiler.Initialize();
			compiler.Symbols.InsertSymbol("OptionalTarget", new ProtoScript.Interpretter.RuntimeInfo.DotNetTypeInfo(typeof(OptionalTarget)));
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			return interpretter.RunMethodAsObject(null, methodName, new List<object>());
		}

		[TestMethod]
		public void OptionalArgument_Omitted_UsesDefaultValue()
		{
			string code =
"function main() : string\r\n" +
"{\r\n" +
"	return OptionalTarget.Format(\"hi\");\r\n" +
"}\r\n";

			object? result = RunGlobalFunction(code, "main", out Compiler compiler);
			Assert.AreEqual(0, compiler.Diagnostics.Count);
			Assert.AreEqual("hi!", result);
		}

		[TestMethod]
		public void OptionalWithParams_OmittedOptionalAndParams_UsesDefaults()
		{
			string code =
"function main() : string\r\n" +
"{\r\n" +
"	return OptionalTarget.JoinWith(\"root\");\r\n" +
"}\r\n";

			object? result = RunGlobalFunction(code, "main", out Compiler compiler);
			Assert.AreEqual(0, compiler.Diagnostics.Count);
			Assert.AreEqual("root", result);
		}

		[TestMethod]
		public void OptionalNullableParameters_AcceptPrimitiveArguments()
		{
			string code =
"function main() : string\r\n" +
"{\r\n" +
"	return OptionalTarget.WithNullable(5, false);\r\n" +
"}\r\n";

			object? result = RunGlobalFunction(code, "main", out Compiler compiler);
			Assert.AreEqual(0, compiler.Diagnostics.Count);
			Assert.AreEqual("5|false", result);
		}

		[TestMethod]
		public void OptionalArgument_MissingRequiredParameter_StillReportsDiagnostic()
		{
			string code =
"function main() : string\r\n" +
"{\r\n" +
"	return OptionalTarget.Format();\r\n" +
"}\r\n";

			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Symbols.InsertSymbol("OptionalTarget", new ProtoScript.Interpretter.RuntimeInfo.DotNetTypeInfo(typeof(OptionalTarget)));
			compiler.Compile(file);
			Assert.IsTrue(
				compiler.Diagnostics.Any(x => x.Diagnostic.Message.Contains("parameters don't match", StringComparison.OrdinalIgnoreCase)
					|| x.Diagnostic.Message.Contains("Cannot find compatible method", StringComparison.OrdinalIgnoreCase)),
				"Expected method compatibility diagnostic for missing required parameter.");
		}
	}
}
