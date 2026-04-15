using ProtoScript.Interpretter;
using ProtoScript.Interpretter.RuntimeInfo;
using System.Collections.Generic;
using System.Linq;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class DotNetCollectionInitializerDiagnostics_Tests
	{
		private sealed class StringCollector
		{
			private readonly List<string> _items = new List<string>();

			public void Add(string value)
			{
				_items.Add(value);
			}

			public string Joined => string.Join("|", _items);
		}

		private sealed class NoAddCollector
		{
			public string Value { get; set; } = string.Empty;
		}

		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void DotNetCollectionStyleInitializer_SingleEntry_CompilesAndRuns()
		{
			string code = @"
function main() : string
{
	StringCollector collector = new StringCollector { ""turn.completed"" };
	return collector.Joined;
}
";

			Compiler compiler = CreateCompilerWithType("StringCollector", typeof(StringCollector));
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);

			Assert.AreEqual(
				0,
				compiler.Diagnostics.Count,
				string.Join("\n", compiler.Diagnostics.Select(d => d.Diagnostic?.Message ?? "(null)")));

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			object? result = interpretter.RunMethodAsObject(null, "main", new List<object>());
			Assert.AreEqual("turn.completed", result);
		}

		[TestMethod]
		public void DotNetCollectionStyleInitializer_MissingAdd_FailsAtRuntime()
		{
			string code = @"
function main() : string
{
	NoAddCollector collector = new NoAddCollector { ""turn.completed"" };
	return collector.Value;
}
";

			Compiler compiler = CreateCompilerWithType("NoAddCollector", typeof(NoAddCollector));
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);

			Assert.AreEqual(
				0,
				compiler.Diagnostics.Count,
				string.Join("\n", compiler.Diagnostics.Select(d => d.Diagnostic?.Message ?? "(null)")));

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);

			RuntimeException ex = Assert.ThrowsException<RuntimeException>(() =>
				interpretter.RunMethodAsObject(null, "main", new List<object>()));
			Assert.IsTrue(ex.Message.Contains("Cannot apply collection initializer entry"), ex.Message);
		}

		private static Compiler CreateCompilerWithType(string typeAlias, System.Type type)
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Symbols.InsertSymbol(typeAlias, new DotNetTypeInfo(type));
			return compiler;
		}
	}
}

