using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ontology;
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

		private sealed class MutableDto
		{
			public string Prompt { get; set; } = string.Empty;
			public string Size { get; set; } = string.Empty;
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

		private static object? RunCompiledFunction(Compiler compiler, ProtoScript.File file, string methodName, List<object> parameters)
		{
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			Assert.AreEqual(0, compiler.Diagnostics.Count, string.Join(Environment.NewLine, compiler.Diagnostics.Select(x => x.Diagnostic.Message)));

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			return interpretter.RunMethodAsObject(null, methodName, parameters);
		}

		private static object? RunCompiledPrototypeFunction(Compiler compiler, ProtoScript.File file, string prototypeName, string methodName, List<object> parameters)
		{
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			Assert.AreEqual(0, compiler.Diagnostics.Count, string.Join(Environment.NewLine, compiler.Diagnostics.Select(x => x.Diagnostic.Message)));

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			Prototype prototype = Prototypes.GetPrototypeByPrototypeName(prototypeName);
			return interpretter.RunMethodAsObject(prototype, methodName, parameters);
		}

		[TestMethod]
		public void RunNewObject_DotNetPropertyAssignment_StringParameter_RoundTripsValue()
		{
			Compiler compiler = CreateCompilerWithType("MutableDto", typeof(MutableDto));
			ProtoScript.File file = Files.ParseFileContents(@"
function Build(string prompt) : string
{
	MutableDto request = new MutableDto();
	request.Prompt = prompt;
	return request.Prompt;
}");

			object? result = RunCompiledFunction(compiler, file, "Build", new List<object> { "hello image" });

			Assert.AreEqual("hello image", result);
		}

		[TestMethod]
		public void RunNewObject_DotNetPropertyAssignment_StringLiteral_RoundTripsValue()
		{
			Compiler compiler = CreateCompilerWithType("MutableDto", typeof(MutableDto));
			ProtoScript.File file = Files.ParseFileContents(@"
function Build() : string
{
	MutableDto request = new MutableDto();
	request.Prompt = ""literal image"";
	return request.Prompt;
}");

			object? result = RunCompiledFunction(compiler, file, "Build", new List<object>());

			Assert.AreEqual("literal image", result);
		}

		[TestMethod]
		public void RunNewObject_ImportedDotNetPropertyAssignment_StringParameter_RoundTripsValue()
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.File file = Files.ParseFileContents($@"
reference {typeof(ImportedMutableDto).Assembly.GetName().Name} {typeof(ImportedMutableDto).Namespace};
import {typeof(ImportedMutableDto).Assembly.GetName().Name} {typeof(ImportedMutableDto).FullName} MutableDto;

function Build(string prompt) : string
{{
	MutableDto request = new MutableDto();
	request.Prompt = prompt;
	return request.Prompt;
}}");

			object? result = RunCompiledFunction(compiler, file, "Build", new List<object> { "hello imported image" });

			Assert.AreEqual("hello imported image", result);
		}

		[TestMethod]
		public void RunNewObject_ImportedDotNetPropertyAssignment_InPrototypeMethod_RoundTripsValue()
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.File file = Files.ParseFileContents($@"
reference {typeof(ImportedMutableDto).Assembly.GetName().Name} {typeof(ImportedMutableDto).Namespace};
import {typeof(ImportedMutableDto).Assembly.GetName().Name} {typeof(ImportedMutableDto).FullName} MutableDto;

prototype BaseObject;

prototype DtoService : BaseObject
{{
	function Build(string prompt) : string
	{{
		MutableDto request = new MutableDto();
		request.Prompt = prompt;
		return request.Prompt;
	}}
}}

prototype DtoAction : BaseObject
{{
	function Execute(string prompt) : string
	{{
		return DtoService.Build(prompt);
	}}
}}");

			object? result = RunCompiledPrototypeFunction(compiler, file, "DtoAction", "Execute", new List<object> { "hello prototype image" });

			Assert.AreEqual("hello prototype image", result);
		}


		[TestMethod]
		public void RunNewObject_InvalidDotNetPropertyExpressionStatement_ThrowsCompilerDiagnosticBeforeRuntime()
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.File file = Files.ParseFileContents($@"
reference {typeof(ImportedMutableDto).Assembly.GetName().Name} {typeof(ImportedMutableDto).Namespace};
import {typeof(ImportedMutableDto).Assembly.GetName().Name} {typeof(ImportedMutableDto).FullName} MutableDto;

function Build(string prompt) : string
{{
	MutableDto request = new MutableDto();
	request.DoesNotExist = prompt;
	return request.Prompt;
}}");

			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			Assert.IsTrue(
				compiler.Diagnostics.Any(x => x.Diagnostic.Message.Contains("Could not find property DoesNotExist", StringComparison.Ordinal)),
				string.Join(Environment.NewLine, compiler.Diagnostics.Select(x => x.Diagnostic.Message)));

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			object? result = interpretter.RunMethodAsObject(null, "Build", new List<object> { "hello invalid image" });

			Assert.AreEqual(string.Empty, result);
		}

		private static Compiler CreateCompilerWithType(string typeAlias, System.Type type)
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Symbols.InsertSymbol(typeAlias, new ProtoScript.Interpretter.RuntimeInfo.DotNetTypeInfo(type));
			return compiler;
		}
	}

	public sealed class ImportedMutableDto
	{
		public string Prompt { get; set; } = string.Empty;
		public string Size { get; set; } = string.Empty;
	}
}


