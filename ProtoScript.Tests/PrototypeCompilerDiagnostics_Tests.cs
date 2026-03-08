using ProtoScript.Interpretter;
using ProtoScript.Interpretter.Compiling;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class PrototypeCompilerDiagnostics_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void DefinePrototypes_MissingResolvedPrototype_WrapsWithPrototypeContext()
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();

			ProtoScript.File file = Files.ParseFileContents("prototype Missing;");

			ProtoScriptCompilerException ex = Assert.ThrowsException<ProtoScriptCompilerException>(() =>
			{
				PrototypeCompiler.DefinePrototypes(file, compiler);
			});

			Assert.IsTrue(
				ex.Explanation.Contains("Failed to define prototype 'Missing'", StringComparison.Ordinal),
				"Expected prototype-specific context in explanation, got: " + ex.Explanation);
		}

		[TestMethod]
		public void Compile_SessionManagementAnnotationSnippet_DoesNotThrowDefinePrototypesNullReference()
		{
			const string code = @"
prototype SessionManagementSkillAction : OpsAction
{
	Description = @""Base action type for Session Management skill tools."";
}

[SemanticEntity(""session management skill"")]
prototype SessionManagementSkill : SkillEntity, CoreEntity
{
	Description = ""Session management and maintenance actions for agent session directories and metadata."";
	ActionRoot = SessionManagementSkillAction;
}

[SemanticProgram.InfinitivePhrase(""to remove guid named buffaly sessions"")]
[SemanticProgram.InfinitivePhrase(""to delete guid named opsagent sessions"")]
prototype ToRemoveGuidNamedBuffalySessions : SessionManagementSkillAction
{
	function Execute(bool dryRun = true) : string
	{
		return ""ok"";
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
					"Unexpected NullReferenceException surfaced from DefinePrototypes: " + ex.Explanation);
				throw;
			}
		}

	}
}
