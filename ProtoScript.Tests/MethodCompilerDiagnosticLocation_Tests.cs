using ProtoScript.Interpretter;
using ProtoScript.Interpretter.Compiling;
using ProtoScript.Interpretter.RuntimeInfo;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class MethodCompilerDiagnosticLocation_Tests
	{
		[TestMethod]
		public void Compile_MethodResolutionWithMissingMethodInfo_FallsBackToReceiverLocation()
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();

			ProtoScript.MethodEvaluation methodEval = new ProtoScript.MethodEvaluation
			{
				MethodName = "GetAwaiter"
			};

			ProtoScript.Interpretter.Compiled.Expression receiver = new ProtoScript.Interpretter.Compiled.Literal
			{
				Value = "value",
				InferredType = new TypeInfo(typeof(string)),
				Info = new StatementParsingInfo { File = @"C:\temp\AwaitError.pts", StartingOffset = 10, Length = 5 }
			};

			MethodCompiler.CompileMethodEvaluationInternal(methodEval, receiver, compiler);

			CompilerDiagnostic? diagnostic = compiler.Diagnostics.FirstOrDefault(x =>
				x.Diagnostic.Message.Contains("GetAwaiter", StringComparison.OrdinalIgnoreCase)
				|| x.Diagnostic.Message.Contains("Cannot find compatible method", StringComparison.OrdinalIgnoreCase));

			Assert.IsNotNull(diagnostic, "Expected a method resolution diagnostic.");
			Assert.IsNotNull(diagnostic.Expression);
			Assert.AreEqual(@"C:\temp\AwaitError.pts", diagnostic.Expression.Info.File);
		}
	}
}
