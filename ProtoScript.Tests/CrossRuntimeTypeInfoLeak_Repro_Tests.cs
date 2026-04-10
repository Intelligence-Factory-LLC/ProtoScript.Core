using Ontology;
using ProtoScript.Interpretter;
using ProtoScript.Interpretter.Compiled;
using ProtoScript.Interpretter.RuntimeInfo;
using ProtoScript.Interpretter.Symbols;
using ProtoScript.Parsers;
using System.Reflection;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class CrossRuntimeTypeInfoLeak_Repro_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void PrototypeTypeInfo_ResolvesFromCurrentRuntimeSymbols_InSameProcess()
		{
			string prototypeName = "CrossRuntimeLeak_" + Guid.NewGuid().ToString("N");
			string methodName = "Ping";

			Compiler compiler1 = BuildCompiler($@"
prototype {prototypeName}
{{
	function {methodName}() : string
	{{
		string tag = ""one"";
		return tag;
	}}
}}");
			NativeInterpretter interpreter1 = new NativeInterpretter(compiler1);

			Compiler compiler2 = BuildCompiler($@"
prototype {prototypeName}
{{
	function {methodName}() : string
	{{
		string tag = ""two"";
		return tag;
	}}
}}");

			Prototype prototype = Prototypes.GetPrototypeByPrototypeName(prototypeName);
			PrototypeTypeInfo typeInfo1 = (PrototypeTypeInfo)compiler1.Symbols.GetTypeInfo(prototypeName)!;
			PrototypeTypeInfo typeInfo2 = (PrototypeTypeInfo)compiler2.Symbols.GetTypeInfo(prototypeName)!;

			MethodInfo getTypeInfoMethod = typeof(NativeInterpretter).GetMethod(
				"GetPrototypeTypeInfo",
				BindingFlags.NonPublic | BindingFlags.Instance)!;

			PrototypeTypeInfo? resolvedTypeInfo = getTypeInfoMethod.Invoke(interpreter1, new object[] { prototype }) as PrototypeTypeInfo;
			Assert.IsNotNull(resolvedTypeInfo);
			Assert.AreSame(typeInfo1, resolvedTypeInfo, "Interpreter1 should resolve TypeInfo from compiler1 symbols.");
			Assert.AreNotSame(typeInfo2, resolvedTypeInfo, "Interpreter1 should not resolve TypeInfo from compiler2 runtime.");

			FunctionRuntimeInfo? resolvedMethod = interpreter1.FindOverriddenMethod(prototype, methodName);
			Assert.IsNotNull(resolvedMethod);
			FunctionRuntimeInfo? compiler1Method = typeInfo1.Scope.GetSymbol(methodName) as FunctionRuntimeInfo;
			FunctionRuntimeInfo? compiler2Method = typeInfo2.Scope.GetSymbol(methodName) as FunctionRuntimeInfo;
			Assert.IsNotNull(compiler1Method);
			Assert.IsNotNull(compiler2Method);
			Assert.AreSame(compiler1Method, resolvedMethod, "Method dispatch should resolve against compiler1 scope.");
			Assert.AreNotSame(compiler2Method, resolvedMethod);

			object? result = interpreter1.RunMethodAsObject(prototype, methodName, new List<object>());
			Assert.AreEqual("one", result, "Interpreter1 should execute compiler1 method body.");
		}

		[TestMethod]
		public void EvaluateGetStack_ForeignScope_ThrowsExplicitRuntimeError()
		{
			Compiler compiler1 = BuildCompiler(@"
function main() : string
{
	return ""ok"";
}");
			NativeInterpretter interpreter1 = new NativeInterpretter(compiler1);

			Compiler compiler2 = BuildCompiler(@"
function main() : string
{
	return ""ok"";
}");
			Scope foreignScope = compiler2.Symbols.GetGlobalScope();
			if (foreignScope.Stack.Count == 0)
			{
				foreignScope.Stack.Add("foreign");
			}

			GetStack getStack = new GetStack
			{
				Index = 0,
				Scope = foreignScope
			};

			try
			{
				interpreter1.Evaluate(getStack);
				Assert.Fail("Expected RuntimeException for foreign GetStack scope.");
			}
			catch (RuntimeException ex)
			{
				Assert.IsTrue(ex.Message.Contains("not owned by current runtime", StringComparison.OrdinalIgnoreCase), ex.Message);
			}
		}

		private static Compiler BuildCompiler(string code)
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Compile(Files.ParseFileContents(code));
			Assert.AreEqual(0, compiler.Diagnostics.Count, string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic.Message)));
			return compiler;
		}
	}
}
