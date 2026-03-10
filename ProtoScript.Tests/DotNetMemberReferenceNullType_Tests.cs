using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class DotNetMemberReferenceNullType_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void Compile_PrototypeDotUnknownMember_DoesNotThrowNullReference()
		{
			const string code = @"
prototype MetabaseSkillAction;
prototype ToEnsureRootTraxVdbApplication : MetabaseSkillAction
{
	function Execute() : string
	{
		ToEnsureRootTraxVdbApplication app = new ToEnsureRootTraxVdbApplication();
		return app.ApplicationName;
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
				Assert.IsFalse(
					ex.Explanation.Contains("NullReferenceException", StringComparison.OrdinalIgnoreCase),
					"Unexpected NullReferenceException: " + ex.Explanation);
				throw;
			}
		}
	}
}
