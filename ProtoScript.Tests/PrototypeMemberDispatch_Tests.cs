using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class PrototypeMemberDispatch_Tests
	{
		[TestMethod]
		public void ParsePrototype_WithFieldMethodAndInitializerShortForm_ParsesAllMembers()
		{
			const string code = @"
prototype Demo
{
	string Name = ""demo"";
	int Count;

	string Execute()
	{
		return Name;
	}

	Name = ""updated"";
}";

			ProtoScript.File file = Files.ParseFileContents(code);
			ProtoScript.PrototypeDefinition prototype = file.PrototypeDefinitions.Single();

			Assert.AreEqual("Demo", prototype.PrototypeName.TypeName);
			Assert.AreEqual(2, prototype.Fields.Count);
			Assert.AreEqual(1, prototype.Methods.Count);
			Assert.AreEqual("Execute", prototype.Methods[0].FunctionName);
			Assert.AreEqual(1, prototype.Initializers.Count);
			Assert.AreEqual(1, prototype.Initializers[0].Statements.Count);
		}

		[TestMethod]
		public void ParsePrototype_WithCSharpStyleMethodDeclaration_ConvertsToFunction()
		{
			const string code = @"
prototype Demo
{
	string Echo(string value)
	{
		return value;
	}
}";

			ProtoScript.File file = Files.ParseFileContents(code);
			ProtoScript.PrototypeDefinition prototype = file.PrototypeDefinitions.Single();

			Assert.AreEqual(1, prototype.Methods.Count);
			Assert.AreEqual("Echo", prototype.Methods[0].FunctionName);
			Assert.AreEqual("string", prototype.Methods[0].ReturnType.TypeName);
			Assert.AreEqual(1, prototype.Methods[0].Parameters.Count);
		}
	}
}
