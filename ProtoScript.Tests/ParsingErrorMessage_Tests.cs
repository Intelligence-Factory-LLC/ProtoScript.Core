using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class ParsingErrorMessage_Tests
	{
		[TestMethod]
		public void ParseFile_InstanceLikeTopLevelDeclaration_EmitsHelpfulMessage()
		{
			const string code = @"
Project#SelfReflection
	EntityName = ""self reflection""
";

			ProtoScriptTokenizingException err = Assert.ThrowsException<ProtoScriptTokenizingException>(
				() => Files.ParseFileContents(code));

			Assert.IsFalse(string.IsNullOrWhiteSpace(err.Explanation), "Expected non-empty parse explanation.");
			Assert.AreNotEqual(", ", err.Explanation, "Parser should not emit a placeholder ', ' explanation.");
			Assert.IsTrue(
				err.Explanation.Contains("prototype Name : BaseType", StringComparison.Ordinal),
				"Expected guidance for missing prototype declaration. Actual: " + err.Explanation);
		}
	}
}
