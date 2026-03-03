using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class NegativeIntegerLiteral_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void Compile_NegativeIntegerLiteralInReturnExpression_Succeeds()
		{
			const string code = @"
function main() : int
{
	return -1;
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Compile(Files.ParseFileContents(code));
		}

		[TestMethod]
		public void Compile_NegativeIntegerLiteralInPrototypeFieldInitializer_Succeeds()
		{
			const string code = @"
prototype BaseObject;

prototype TabMemory : BaseObject
{
	Int TabIndex = -1;
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Compile(Files.ParseFileContents(code));
		}
	}
}
