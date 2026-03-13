using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class StringReferenceBoundary_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		private static NativeInterpretter BuildInterpreter(string code)
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			return interpretter;
		}

		// Purpose: Returning StringRef should cross the boundary as an opaque handle instead of raw string payload.
		[TestMethod]
		public void ReturnTypeStringRef_ReturnsOpaqueHandle()
		{
			string code = @"
function ReturnRef() : StringRef
{
	return ""Large payload for handle"";
}
";
			NativeInterpretter interpretter = BuildInterpreter(code);
			object? result = interpretter.RunMethodAsObject(null, "ReturnRef", new List<object>());

			Assert.IsNotNull(result);
			Assert.IsInstanceOfType<StringReference>(result);
			StringReference handle = (StringReference)result!;
			Assert.IsTrue(handle.PrototypeName.StartsWith("System.String[ref:", StringComparison.Ordinal));
			Assert.IsTrue(handle.PrototypeName.EndsWith("]", StringComparison.Ordinal));
			Assert.IsFalse(string.IsNullOrWhiteSpace(handle.PrototypeName));
			Assert.IsTrue(handle.TryResolveString(out string? resolvedValue));
			Assert.AreEqual("Large payload for handle", resolvedValue);
		}

		// Purpose: Handle text must not leak secret payload fragments.
		[TestMethod]
		public void ReturnTypeStringRef_DoesNotLeakSecretInHandle()
		{
			string secret = "sk_live_super_secret_value_123";
			string code = @"
function ReturnRef(string value) : StringRef
{
	return value;
}
";
			NativeInterpretter interpretter = BuildInterpreter(code);
			object? result = interpretter.RunMethodAsObject(null, "ReturnRef", new List<object> { secret });

			Assert.IsNotNull(result);
			Assert.IsInstanceOfType<StringReference>(result);
			StringReference handle = (StringReference)result!;
			Assert.IsTrue(handle.PrototypeName.StartsWith("System.String[ref:", StringComparison.Ordinal));
			Assert.IsFalse(handle.PrototypeName.Contains(secret, StringComparison.Ordinal));
			Assert.IsTrue(handle.TryResolveString(out string? resolvedValue));
			Assert.AreEqual(secret, resolvedValue);
		}

		// Purpose: Large payloads should return bounded opaque handles and still resolve.
		[TestMethod]
		public void ReturnTypeStringRef_LargePayload_ReturnsBoundedOpaqueHandle()
		{
			string payload = new string('x', 1024 * 1024);
			string code = @"
function ReturnRef(string value) : StringRef
{
	return value;
}
";
			NativeInterpretter interpretter = BuildInterpreter(code);
			object? result = interpretter.RunMethodAsObject(null, "ReturnRef", new List<object> { payload });

			Assert.IsNotNull(result);
			Assert.IsInstanceOfType<StringReference>(result);
			StringReference handle = (StringReference)result!;
			Assert.IsTrue(handle.PrototypeName.StartsWith("System.String[ref:", StringComparison.Ordinal));
			Assert.IsTrue(handle.PrototypeName.Length < 96);
			Assert.IsTrue(handle.TryResolveString(out string? resolvedValue));
			Assert.AreEqual(payload, resolvedValue);
		}

		// Purpose: Passing StringRef into a function expecting string should auto-resolve to text.
		[TestMethod]
		public void StringParameter_AcceptsStringRefHandle()
		{
			string code = @"
function ReturnRef() : StringRef
{
	return ""Resolve me automatically"";
}

function Echo(string value) : string
{
	return value;
}
";
			NativeInterpretter interpretter = BuildInterpreter(code);
			object? handle = interpretter.RunMethodAsObject(null, "ReturnRef", new List<object>());
			object? result = interpretter.RunMethodAsObject(null, "Echo", new List<object> { handle! });

			Assert.AreEqual("Resolve me automatically", result);
		}

		// Purpose: Passing StringRef into a function expecting String wrapper should auto-resolve.
		[TestMethod]
		public void StringWrapperParameter_AcceptsStringRefHandle()
		{
			string code = @"
function ReturnRef() : StringRef
{
	return ""Wrapper resolution"";
}

function EchoWrapper(String value) : string
{
	return value.GetStringValue();
}
";
			NativeInterpretter interpretter = BuildInterpreter(code);
			object? handle = interpretter.RunMethodAsObject(null, "ReturnRef", new List<object>());
			object? result = interpretter.RunMethodAsObject(null, "EchoWrapper", new List<object> { handle! });

			Assert.AreEqual("Wrapper resolution", result);
		}

		// Purpose: Methods declared to return string should continue returning raw string values.
		[TestMethod]
		public void ReturnTypeString_RemainsRawString()
		{
			string code = @"
function ReturnText() : string
{
	return ""Raw text"";
}
";
			NativeInterpretter interpretter = BuildInterpreter(code);
			object? result = interpretter.RunMethodAsObject(null, "ReturnText", new List<object>());

			Assert.IsInstanceOfType<string>(result);
			Assert.AreEqual("Raw text", result);
		}
	}
}
