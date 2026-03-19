using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class TokenizerHelpers_Tests
	{
		[TestMethod]
		public void IsNext_DoesNotAdvanceCursor()
		{
			Tokenizer tok = new Tokenizer("prototype Demo;");
			int startCursor = tok.getCursor();

			bool isNext = tok.IsNext("prototype");

			Assert.IsTrue(isNext);
			Assert.AreEqual(startCursor, tok.getCursor());
		}

		[TestMethod]
		public void TryConsume_OnMismatch_DoesNotAdvanceCursor()
		{
			Tokenizer tok = new Tokenizer("prototype Demo;");
			int startCursor = tok.getCursor();

			bool consumed = tok.TryConsume("function");

			Assert.IsFalse(consumed);
			Assert.AreEqual(startCursor, tok.getCursor());
		}

		[TestMethod]
		public void CouldBeNext_OnMatch_ConsumesToken()
		{
			Tokenizer tok = new Tokenizer("prototype Demo;");

			bool consumed = tok.CouldBeNext("prototype");
			string next = tok.peekNextToken();

			Assert.IsTrue(consumed);
			Assert.AreEqual("Demo", next);
		}
	}
}
