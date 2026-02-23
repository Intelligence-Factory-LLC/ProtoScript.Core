using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class TernaryOperator_Tests
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

		// Purpose: Validate ternary returns true branch value when condition is true.
		[TestMethod]
		public void Ternary_BasicTrueBranch_ReturnsTrueExpression()
		{
			string code = @"
function main() : int
{
	return true ? 1 : 2;
}
";

			object? result = RunMain(code);
			Assert.IsTrue(result is int);
			Assert.AreEqual(1, (int)result);
		}

		// Purpose: Validate ternary returns false branch value when condition is false.
		[TestMethod]
		public void Ternary_BasicFalseBranch_ReturnsFalseExpression()
		{
			string code = @"
function main() : int
{
	return false ? 1 : 2;
}
";

			object? result = RunMain(code);
			Assert.IsTrue(result is int);
			Assert.AreEqual(2, (int)result);
		}

		// Purpose: Verify nested ternary expressions associate to the right.
		[TestMethod]
		public void Ternary_Nested_RightAssociative_BehavesCorrectly()
		{
			string code = @"
function main() : int
{
	return false ? 1 : true ? 2 : 3;
}
";

			object? result = RunMain(code);
			Assert.IsTrue(result is int);
			Assert.AreEqual(2, (int)result);
		}

		// Purpose: Verify relational expressions are evaluated before ternary branch selection.
		[TestMethod]
		public void Ternary_Precedence_WithComparison_BehavesCorrectly()
		{
			string code = @"
function main() : int
{
	return 1 < 2 ? 10 : 20;
}
";

			object? result = RunMain(code);
			Assert.IsTrue(result is int);
			Assert.AreEqual(10, (int)result);
		}

		// Purpose: Ensure the non-selected ternary branch is not evaluated.
		[TestMethod]
		public void Ternary_ShortCircuit_DoesNotEvaluateUnselectedBranch()
		{
			string code = @"
function crash() : int
{
	String s = null;
	return s.GetStringValue().Length;
}

function main() : int
{
	return true ? 7 : crash();
}
";

			object? result = RunMain(code);
			Assert.IsTrue(result is int);
			Assert.AreEqual(7, (int)result);
		}
	}
}
