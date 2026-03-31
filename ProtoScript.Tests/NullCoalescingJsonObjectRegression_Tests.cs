using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class NullCoalescingJsonObjectRegression_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void JsonObject_GetStringOrNull_WithNullCoalescing_DoesNotThrowAndReturnsFallback()
		{
			string code = @"
reference BasicUtilities BasicUtilities;
import BasicUtilities BasicUtilities.JsonObject JsonObject;

function main() : string
{
	JsonObject root = new JsonObject(""{\""data\"":{}}"");
	JsonObject data = root.GetJsonObjectOrDefault(""data"");
	return data.GetStringOrNull(""id"") ?? """";
}
";

			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			Assert.AreEqual(0, compiler.Diagnostics.Count, string.Join("\n", compiler.Diagnostics.Select(x => x.ToString())));

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			object? result = interpretter.RunMethodAsObject(null, "main", new List<object>());
			Assert.AreEqual(string.Empty, result as string);
		}
	}
}
