using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class IncludeStatementTests
	{
		[TestMethod]
		public void ParseIncludeStatement_WithQuotedPath_Succeeds()
		{
			ProtoScript.IncludeStatement statement = IncludeStatements.Parse("include \"Critic/CriticOps.pts\";");
			Assert.AreEqual("Critic/CriticOps.pts", statement.FileName);
			Assert.IsFalse(statement.Recursive);
		}

		[TestMethod]
		public void ParseIncludeStatement_WithUnquotedPath_ThrowsHelpfulError()
		{
			ProtoScriptParsingException err = Assert.ThrowsException<ProtoScriptParsingException>(() =>
				IncludeStatements.Parse("include Critic/CriticOps.pts;"));

			Assert.AreEqual("string literal", err.Expected);
			Assert.IsTrue(err.Explanation?.Contains("quoted string literal", StringComparison.OrdinalIgnoreCase) ?? false);
		}
	}
}
