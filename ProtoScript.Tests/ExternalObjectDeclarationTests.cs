using Ontology.Simulation;
using ProtoScript;
using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class ExternalObjectDeclarationTests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void ParseVariableDeclaration_ExternFlag_IsSet()
		{
			VariableDeclaration declaration = VariableDeclarations.Parse("extern String ext;");
			Assert.IsTrue(declaration.IsExternal);
			Assert.AreEqual("String", declaration.Type.TypeName);
			Assert.AreEqual("ext", declaration.VariableName);
		}

		[TestMethod]
		public void ParseFile_ExternPrototype_RemainsPrototypeDefinition()
		{
			ProtoScript.File file = Files.ParseFileContents("extern prototype ExternalThing;");
			Assert.AreEqual(1, file.PrototypeDefinitions.Count);
			Assert.AreEqual("ExternalThing", file.PrototypeDefinitions[0].PrototypeName.TypeName);
		}

		[TestMethod]
		public void Compile_ExternObject_MethodCall_IsTypeChecked()
		{
			string code = @"
extern String ext;
function main() : string
{
	return ext.GetStringValue();
}";

			ProtoScript.File file = Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Compile(file);

			Assert.AreEqual(0, compiler.Diagnostics.Count);
		}

		[TestMethod]
		public void Compile_ExternObject_InvalidMethodSignature_AddsDiagnostic()
		{
			string code = @"
extern String ext;
function main() : void
{
	ext.GetStringValue(1);
}";

			ProtoScript.File file = Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Compile(file);

			Assert.IsTrue(compiler.Diagnostics.Any(x =>
				x.Diagnostic.Message.Contains("parameters don't match", StringComparison.OrdinalIgnoreCase)
				|| x.Diagnostic.Message.Contains("Cannot find compatible method", StringComparison.OrdinalIgnoreCase)));
		}

		[TestMethod]
		public void RuntimeInjection_BindsToExternDeclarationSlot()
		{
			string code = @"
extern String ext;
function main() : string
{
	return ext.GetStringValue();
}";

			ProtoScript.File file = Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			NativeInterpretter interpretter = new NativeInterpretter(compiler);

			interpretter.InsertGlobalObject("ext", new StringWrapper("injected"));
			interpretter.Evaluate(compiled);

			object? result = interpretter.RunMethodAsObject(null, "main", new List<object>());
			Assert.AreEqual("injected", result);
		}
	}
}
