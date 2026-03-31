using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class TryBareCatch_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		private static object? RunMain(string code, out Compiler compiler)
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			return interpretter.RunMethodAsObject(null, "main", new List<object>());
		}

		[TestMethod]
		public void BareCatch_CatchesThrownRuntimeError()
		{
			string code = @"
function main() : string
{
	try
	{
		String s = null;
		return s.GetStringValue();
	}
	catch
	{
		return ""caught"";
	}
}
";

			object? result = RunMain(code, out Compiler compiler);
			Assert.AreEqual(0, compiler.Diagnostics.Count, string.Join("\n", compiler.Diagnostics.Select(x => x.ToString())));
			Assert.AreEqual("caught", result as string);
		}

		[TestMethod]
		public void BareCatch_WithJsonObjectAndNullCoalescing_CompilesAndRuns()
		{
			string code = @"
reference BasicUtilities BasicUtilities;
import BasicUtilities BasicUtilities.JsonObject JsonObject;

function main() : string
{
	try
	{
		JsonObject root = new JsonObject(""{\""data\"":{}}"");
		JsonObject data = root.GetJsonObjectOrDefault(""data"");
		return data.GetStringOrNull(""id"") ?? """";
	}
	catch
	{
		return ""fallback"";
	}
}
";

			object? result = RunMain(code, out Compiler compiler);
			Assert.AreEqual(0, compiler.Diagnostics.Count, string.Join("\n", compiler.Diagnostics.Select(x => x.ToString())));
			Assert.AreEqual(string.Empty, result as string);
		}
	}
}
