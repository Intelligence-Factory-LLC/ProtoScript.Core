using ProtoScript.Interpretter;
using ProtoScript.Interpretter.RuntimeInfo;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class RuntimeBindingHelpers_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void TryResolvePrototypeReference_ExactName_Resolves()
		{
			Compiler compiler = BuildCompiler(@"
prototype BaseAction;
prototype ChildAction : BaseAction;");
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			TypeInfo? expectedType = compiler.Symbols.GetGlobalScope().GetSymbol("BaseAction") as TypeInfo;

			bool ok = interpretter.TryResolvePrototypeReference("ChildAction", expectedType, out Prototype? resolved, out string error);

			Assert.IsTrue(ok, error);
			Assert.IsNotNull(resolved);
			Assert.AreEqual("ChildAction", resolved.PrototypeName);
		}

		[TestMethod]
		public void TryResolvePrototypeReference_UnknownName_Fails()
		{
			Compiler compiler = BuildCompiler("prototype BaseAction;");
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			TypeInfo? expectedType = compiler.Symbols.GetGlobalScope().GetSymbol("BaseAction") as TypeInfo;

			bool ok = interpretter.TryResolvePrototypeReference("MissingAction", expectedType, out Prototype? resolved, out string error);

			Assert.IsFalse(ok);
			Assert.IsNull(resolved);
			Assert.IsTrue(error.Contains("Prototype not found", StringComparison.OrdinalIgnoreCase), error);
		}

		[TestMethod]
		public void TryResolvePrototypeReference_TypeMismatch_Fails()
		{
			Compiler compiler = BuildCompiler(@"
prototype AlphaBase;
prototype BetaBase;
prototype AlphaChild : AlphaBase;");
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			TypeInfo? expectedType = compiler.Symbols.GetGlobalScope().GetSymbol("BetaBase") as TypeInfo;

			bool ok = interpretter.TryResolvePrototypeReference("AlphaChild", expectedType, out Prototype? resolved, out string error);

			Assert.IsFalse(ok);
			Assert.IsNull(resolved);
			Assert.IsTrue(error.Contains("Expected prototype type", StringComparison.OrdinalIgnoreCase), error);
		}

		[TestMethod]
		public void TryBindMethodCall_ResolvesPrototypeParameter_FromStringName()
		{
			Compiler compiler = BuildCompiler(@"
prototype BaseAction;
prototype ChildAction : BaseAction;
function Execute(BaseAction action, int count) : string
{
	return ""ok"";
}");
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			Dictionary<string, object?> rawParameters = new Dictionary<string, object?>
			{
				["action"] = "ChildAction",
				["count"] = 3
			};

			bool ok = interpretter.TryBindMethodCall(
				null,
				"Execute",
				rawParameters,
				out FunctionRuntimeInfo? method,
				out Dictionary<string, object> boundParameters,
				out string error);

			Assert.IsTrue(ok, error);
			Assert.IsNotNull(method);
			Assert.IsTrue(boundParameters["action"] is Prototype);
			Assert.AreEqual("ChildAction", ((Prototype)boundParameters["action"]).PrototypeName);
			Assert.AreEqual(3, boundParameters["count"]);
		}

		[TestMethod]
		public void TryBindMethodCall_MissingParameter_Fails()
		{
			Compiler compiler = BuildCompiler(@"
prototype BaseAction;
function Execute(BaseAction action, int count) : string
{
	return ""ok"";
}");
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			Dictionary<string, object?> rawParameters = new Dictionary<string, object?>
			{
				["action"] = "BaseAction"
			};

			bool ok = interpretter.TryBindMethodCall(
				null,
				"Execute",
				rawParameters,
				out FunctionRuntimeInfo? _,
				out Dictionary<string, object> _,
				out string error);

			Assert.IsFalse(ok);
			Assert.IsTrue(error.Contains("Missing parameter: count", StringComparison.OrdinalIgnoreCase), error);
		}

		[TestMethod]
		public void TryBindMethodCall_PrototypeTypeMismatch_Fails()
		{
			Compiler compiler = BuildCompiler(@"
prototype AlphaBase;
prototype BetaBase;
prototype AlphaChild : AlphaBase;
function Execute(BetaBase action) : string
{
	return ""ok"";
}");
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			Dictionary<string, object?> rawParameters = new Dictionary<string, object?>
			{
				["action"] = "AlphaChild"
			};

			bool ok = interpretter.TryBindMethodCall(
				null,
				"Execute",
				rawParameters,
				out FunctionRuntimeInfo? _,
				out Dictionary<string, object> _,
				out string error);

			Assert.IsFalse(ok);
			Assert.IsTrue(error.Contains("Invalid prototype parameter", StringComparison.OrdinalIgnoreCase), error);
			Assert.IsTrue(error.Contains("Expected prototype type", StringComparison.OrdinalIgnoreCase), error);
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
