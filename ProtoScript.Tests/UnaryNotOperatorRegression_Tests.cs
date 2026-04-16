using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class UnaryNotOperatorRegression_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void CompileAndRun_DoubleMethodNegation_WithLogicalAnd_ReturnsExpected()
		{
			const string code = @"
function IsLabel(string text) : bool
{
	return text.StartsWith(""[label:"");
}

function IsTimelineLabel(string text) : bool
{
	return text.StartsWith(""[timeline-label:"");
}

function main() : bool
{
	string trimmedInstruction = ""normal text"";
	if (!IsLabel(trimmedInstruction)
		&& !IsTimelineLabel(trimmedInstruction))
	{
		return true;
	}

	return false;
}
";

			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.File file = Files.ParseFileContents(code);
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);

			Assert.AreEqual(
				0,
				compiler.Diagnostics.Count,
				string.Join("\n", compiler.Diagnostics.Select(d => d.Diagnostic?.Message ?? "(null)")));

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			object? result = interpretter.RunMethodAsObject(null, "main", new List<object>());

			Assert.AreEqual(true, result);
		}

		[TestMethod]
		public void CompileAndRun_DoubleMemberCallNegation_WithLogicalAnd_ReturnsExpected()
		{
			const string code = @"
function main() : bool
{
	string trimmedInstruction = ""[timeline-label:abc"";
	if (!trimmedInstruction.StartsWith(""[label:"")
		&& !trimmedInstruction.StartsWith(""[timeline-label:""))
	{
		return true;
	}

	return false;
}
";

			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.File file = Files.ParseFileContents(code);
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);

			Assert.AreEqual(
				0,
				compiler.Diagnostics.Count,
				string.Join("\n", compiler.Diagnostics.Select(d => d.Diagnostic?.Message ?? "(null)")));

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			object? result = interpretter.RunMethodAsObject(null, "main", new List<object>());

			Assert.AreEqual(false, result);
		}
	}
}
