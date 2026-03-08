using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class StringConcatenationBool_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void Compile_StringConcatenation_WithBoolInChain_DoesNotThrow()
		{
			const string code = @"
prototype OpsAction;
prototype SampleAction : OpsAction
{
	function Execute(bool dryRun = true) : string
	{
		string scriptPath = ""C:\\temp\\x.ps1"";
		return
			""Script: "" + scriptPath + ""\n"" +
			""DryRun: "" + dryRun + ""\n"" +
			""Done"";
	}
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();

			try
			{
				compiler.Compile(Files.ParseFileContents(code));
			}
			catch (ProtoScriptCompilerException ex)
			{
				Assert.Fail("Compilation should succeed for bool concatenation. Explanation: " + ex.Explanation);
			}
		}
	}
}
