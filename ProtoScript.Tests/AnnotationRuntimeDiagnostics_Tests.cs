using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class AnnotationRuntimeDiagnostics_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		// Purpose: Method-level SemanticProgram annotation mismatches should report actionable parameter/type details.
		[TestMethod]
		public void MethodAnnotation_TypeMismatch_ReportsDetailedRuntimeDiagnostic()
		{
			const string code = @"
prototype BaseObject;

prototype SemanticProgram
{
	function InfinitivePhrase(Prototype prototype, string infinitive) : void
	{
		// no-op
	}
}

prototype TestAction : BaseObject
{
	[SemanticProgram.InfinitivePhrase(""to test annotation mismatch"")]
	function Execute() : string
	{
		return ""ok"";
	}
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.File file = Files.ParseFileContents(code);
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			NativeInterpretter interpretter = new NativeInterpretter(compiler);

			RuntimeException ex = Assert.ThrowsException<RuntimeException>(() =>
			{
				interpretter.Evaluate(compiled);
			});

			Assert.IsTrue(ex.Message.Contains("Cannot assign value for function", StringComparison.OrdinalIgnoreCase), ex.Message);
			Assert.IsTrue(ex.Message.Contains("InfinitivePhrase", StringComparison.OrdinalIgnoreCase), ex.Message);
			Assert.IsTrue(ex.Message.Contains("parameter 'prototype'", StringComparison.OrdinalIgnoreCase), ex.Message);
			Assert.IsTrue(ex.Message.Contains("Expected", StringComparison.OrdinalIgnoreCase), ex.Message);
			Assert.IsTrue(ex.Message.Contains("FunctionRuntimeInfo", StringComparison.OrdinalIgnoreCase), ex.Message);
		}
	}
}
