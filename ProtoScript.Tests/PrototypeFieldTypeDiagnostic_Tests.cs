using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class PrototypeFieldTypeDiagnostic_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void Compile_PrototypeFieldUsingStringType_ReportsGuidanceInsteadOfThrowing()
		{
			string code = @"
prototype BadFieldType
{
	string Description = ""test"";
}";

			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();

			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);

			Assert.IsNotNull(compiled);
			Assert.IsTrue(
				compiler.Diagnostics.Any(x => (x.Diagnostic?.Message ?? string.Empty).Contains("use 'String' instead of 'string'", StringComparison.OrdinalIgnoreCase)),
				"Expected a diagnostic guiding prototype fields to use 'String' instead of 'string'.");
		}
	}
}
