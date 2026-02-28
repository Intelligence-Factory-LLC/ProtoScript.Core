using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class NewObjectExpression_Tests
	{
		private sealed class AmbiguousCtorTarget
		{
			public AmbiguousCtorTarget(string? left, object? right) { }
			public AmbiguousCtorTarget(object? left, string? right) { }
		}

		private sealed class SingleCtorTarget
		{
			public SingleCtorTarget(string? token, object? options) { }
		}

		[TestInitialize]
		public void InitializeRuntime()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void CompileNewObject_NullArguments_AmbiguousConstructor_EmitsDiagnostic()
		{
			Compiler compiler = CreateCompilerWithType("AmbiguousCtorTarget", typeof(AmbiguousCtorTarget));
			ProtoScript.File file = Files.ParseFileContents(@"
function Build() : void
{
	AmbiguousCtorTarget client = new AmbiguousCtorTarget(null, null);
}");

			compiler.Compile(file);

			CompilerDiagnostic? diagnostic = compiler.Diagnostics
				.FirstOrDefault(x => x.Diagnostic.Message.Contains("Cannot resolve constructor for AmbiguousCtorTarget(null, null).", StringComparison.Ordinal));
			Assert.IsNotNull(diagnostic);
			Assert.IsNotNull(diagnostic.Expression);
			Assert.IsNotNull(diagnostic.Expression.Info);
		}

		[TestMethod]
		public void CompileNewObject_NullArguments_SingleConstructor_Compiles()
		{
			Compiler compiler = CreateCompilerWithType("SingleCtorTarget", typeof(SingleCtorTarget));
			ProtoScript.File file = Files.ParseFileContents(@"
function Build() : void
{
	SingleCtorTarget client = new SingleCtorTarget(null, null);
}");

			compiler.Compile(file);

			Assert.AreEqual(0, compiler.Diagnostics.Count);
		}

		private static Compiler CreateCompilerWithType(string typeAlias, System.Type type)
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Symbols.InsertSymbol(typeAlias, new ProtoScript.Interpretter.RuntimeInfo.DotNetTypeInfo(type));
			return compiler;
		}
	}
}
