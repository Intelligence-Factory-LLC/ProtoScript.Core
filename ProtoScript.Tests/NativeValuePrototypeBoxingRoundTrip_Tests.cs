using Ontology.BaseTypes;
using Ontology.Simulation;
using ProtoScript.Interpretter;
using BasicUtilities;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class NativeValuePrototypeBoxingRoundTrip_Tests
	{
		private sealed class Payload
		{
			public string Id { get; set; } = string.Empty;
			public int Distance { get; set; }
		}

		private sealed class PayloadHost
		{
			public System.Type? LastSeenType;

			public Payload Create(string id)
			{
				return new Payload
				{
					Id = id,
					Distance = id.Length
				};
			}

			public string ConsumePayload(Payload value)
			{
				LastSeenType = value?.GetType();
				return value.Id + ":" + value.Distance;
			}

			public Dictionary<string, int> CreateMap()
			{
				return new Dictionary<string, int>
				{
					["alpha"] = 7
				};
			}

			public string ConsumeMap(Dictionary<string, int> value)
			{
				LastSeenType = value?.GetType();
				return "alpha:" + value["alpha"];
			}

			public JsonObject CreateJson()
			{
				JsonObject json = new JsonObject();
				json["kind"] = "agent";
				return json;
			}

			public string ConsumeJson(JsonObject value)
			{
				LastSeenType = value?.GetType();
				return value["kind"].ToString();
			}
		}

		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void ReturnPrototype_BoxesClrObject_AndTypedMethodUnboxesIt()
		{
			const string code = @"
extern PayloadHost host;

function BuildBoxed() : Prototype
{
	return host.Create(""A1"");
}

function UseTyped(Prototype value) : string
{
	return host.ConsumePayload(value);
}

function main() : string
{
	Prototype boxed = BuildBoxed();
	return UseTyped(boxed);
}";

			(Compiler compiler, NativeInterpretter interpretter) = BuildInterpreter(code);
			PayloadHost host = new PayloadHost();
			interpretter.InsertGlobalObject("host", host);

			Prototype? boxed = interpretter.RunMethodAsPrototype(null, "BuildBoxed", new List<object>());
			Assert.IsInstanceOfType(boxed, typeof(NativeValuePrototype));

			object? result = interpretter.RunMethodAsObject(null, "main", new List<object>());
			Assert.AreEqual("A1:2", result);
			Assert.AreEqual(typeof(Payload), host.LastSeenType);
		}

		[TestMethod]
		public void TypedUnboxing_WorksForDirectCallExpression()
		{
			const string code = @"
extern PayloadHost host;

function BuildBoxed() : Prototype
{
	return host.Create(""B22"");
}

function main() : string
{
	return host.ConsumePayload(BuildBoxed());
}";

			(Compiler compiler, NativeInterpretter interpretter) = BuildInterpreter(code);
			PayloadHost host = new PayloadHost();
			interpretter.InsertGlobalObject("host", host);

			object? result = interpretter.RunMethodAsObject(null, "main", new List<object>());
			Assert.AreEqual("B22:3", result);
			Assert.AreEqual(typeof(Payload), host.LastSeenType);
		}

		[TestMethod]
		public void TypedUnboxing_WorksForEnumerableClrObject()
		{
			const string code = @"
extern PayloadHost host;

function BuildMapBoxed() : Prototype
{
	return host.CreateMap();
}

function main() : string
{
	Prototype boxed = BuildMapBoxed();
	return host.ConsumeMap(boxed);
}";

			(Compiler compiler, NativeInterpretter interpretter) = BuildInterpreter(code);
			PayloadHost host = new PayloadHost();
			interpretter.InsertGlobalObject("host", host);

			object? result = interpretter.RunMethodAsObject(null, "main", new List<object>());
			Assert.AreEqual("alpha:7", result);
			Assert.AreEqual(typeof(Dictionary<string, int>), host.LastSeenType);
		}

		[TestMethod]
		public void TypedUnboxing_WorksForJsonObject()
		{
			const string code = @"
extern PayloadHost host;

function BuildJsonBoxed() : Prototype
{
	return host.CreateJson();
}

function main() : string
{
	Prototype boxed = BuildJsonBoxed();
	return host.ConsumeJson(boxed);
}";

			(Compiler compiler, NativeInterpretter interpretter) = BuildInterpreter(code);
			PayloadHost host = new PayloadHost();
			interpretter.InsertGlobalObject("host", host);

			object? result = interpretter.RunMethodAsObject(null, "main", new List<object>());
			Assert.AreEqual("agent", result);
			Assert.AreEqual(typeof(JsonObject), host.LastSeenType);
		}

		private static (Compiler compiler, NativeInterpretter interpretter) BuildInterpreter(string code)
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Symbols.InsertSymbol("Payload", new ProtoScript.Interpretter.RuntimeInfo.DotNetTypeInfo(typeof(Payload)));
			compiler.Symbols.InsertSymbol("PayloadHost", new ProtoScript.Interpretter.RuntimeInfo.DotNetTypeInfo(typeof(PayloadHost)));

			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			Assert.AreEqual(0, compiler.Diagnostics.Count);

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			return (compiler, interpretter);
		}
	}
}
