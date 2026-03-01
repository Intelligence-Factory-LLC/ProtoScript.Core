using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class PrototypeCompilerRegression_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void Compile_InvalidPrototypeShortInitializer_DoesNotThrowAndAddsDiagnostic()
		{
			string code = @"
prototype BrokenInitializer
{
	init
	{
		DoSomething();
	}
}";

			ProtoScript.File file = Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();

			compiler.Compile(file);

			Assert.IsTrue(compiler.Diagnostics.Any(x =>
				x.Diagnostic.Message.Contains("Initializer should be an assignment statement", StringComparison.OrdinalIgnoreCase)));
		}
	}
}
