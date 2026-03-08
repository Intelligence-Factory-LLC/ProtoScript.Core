using ProtoScript.Interpretter;
using ProtoScript.Interpretter.Compiling;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class PrototypeCompilerDiagnostics_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void DefinePrototypes_MissingResolvedPrototype_WrapsWithPrototypeContext()
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();

			ProtoScript.File file = Files.ParseFileContents("prototype Missing;");

			ProtoScriptCompilerException ex = Assert.ThrowsException<ProtoScriptCompilerException>(() =>
			{
				PrototypeCompiler.DefinePrototypes(file, compiler);
			});

			Assert.IsTrue(
				ex.Explanation.Contains("Failed to define prototype 'Missing'", StringComparison.Ordinal),
				"Expected prototype-specific context in explanation, got: " + ex.Explanation);
		}

	}
}
