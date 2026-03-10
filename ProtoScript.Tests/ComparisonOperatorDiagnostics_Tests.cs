using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class ComparisonOperatorDiagnostics_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void Compile_NullComparedToInt_EmitsTypedOperandDiagnostic_NotNullReference()
		{
			const string code = @"
prototype CompareSkillAction
{
	function Execute() : bool
	{
		return null > 0;
	}
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();

			compiler.Compile(Files.ParseFileContents(code));

			Assert.IsTrue(
				compiler.Diagnostics.Any(x =>
					(x.Diagnostic?.Message ?? string.Empty)
						.Contains("requires typed operands", StringComparison.OrdinalIgnoreCase)),
				"Expected typed operand diagnostic, got: " +
				string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic?.Message)));
			Assert.IsFalse(
				compiler.Diagnostics.Any(x =>
					(x.Diagnostic?.Message ?? string.Empty)
						.Contains("NullReferenceException", StringComparison.OrdinalIgnoreCase)),
				"Unexpected NullReferenceException diagnostic: " +
				string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic?.Message)));
		}
	}
}
