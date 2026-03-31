using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class NullCoalescingOperator_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		private static object? RunMain(string code)
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			return interpretter.RunMethodAsObject(null, "main", new List<object>());
		}

		[TestMethod]
		public void NullCoalescing_LeftNull_ReturnsRight()
		{
			string code = @"
function main() : string
{
	String s = null;
	return s ?? ""fallback"";
}
";
			object? result = RunMain(code);
			Assert.AreEqual("fallback", result as string);
		}

		[TestMethod]
		public void NullCoalescing_LeftNonNull_ReturnsLeft()
		{
			string code = @"
function main() : string
{
	String s = ""value"";
	return s ?? ""fallback"";
}
";
			object? result = RunMain(code);
			Assert.AreEqual("value", result as string);
		}

		[TestMethod]
		public void NullCoalescing_ShortCircuit_DoesNotEvaluateRightWhenLeftNotNull()
		{
			string code = @"
function crash() : string
{
	String s = null;
	return s.GetStringValue();
}

function main() : string
{
	String s = ""safe"";
	return s ?? crash();
}
";
			object? result = RunMain(code);
			Assert.AreEqual("safe", result as string);
		}
	}
}
