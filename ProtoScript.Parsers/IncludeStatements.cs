using BasicUtilities;

namespace ProtoScript.Parsers
{
	public class IncludeStatements
	{
		static public ProtoScript.IncludeStatement Parse(string strStatement)
		{
			Tokenizer tok = new Tokenizer(strStatement);
			return Parse(tok);
		}

		public static ProtoScript.IncludeStatement Parse(Tokenizer tok)
		{
			ProtoScript.IncludeStatement result = new ProtoScript.IncludeStatement();

			tok.movePastWhitespace(); result.Info.StartStatement(tok.getCursor());
			result.Info.File = Files.CurrentFile;

			tok.MustBeNext("include");

			if (tok.CouldBeNext("recursive"))
			{
				result.Recursive = true;
			}

			string strPathToken = tok.peekNextToken();
			if (!IsIncludePathStringLiteral(strPathToken))
			{
				throw new ProtoScriptParsingException(
					tok.getString(),
					tok.getCursor(),
					"string literal",
					"Include path must be a quoted string literal. Example: include \"Critic/CriticOps.pts\";");
			}

			result.FileName = tok.getNextToken();

			tok.MustBeNext(";");

			if (result.FileName.StartsWith("@\"") && result.FileName.EndsWith("\""))
			{
				result.FileName = StringUtil.BetweenQuotes(result.FileName).Replace("\"\"", "\"");
			}
			else if (result.FileName.StartsWith("\"") && result.FileName.EndsWith("\""))
			{
				result.FileName = StringUtil.BetweenQuotes(result.FileName);
			}



			result.Info.StopStatement(tok.getCursor());

			return result;
		}

		private static bool IsIncludePathStringLiteral(string token)
		{
			if (string.IsNullOrEmpty(token))
			{
				return false;
			}

			if (token.StartsWith("@\"") && token.EndsWith("\"") && token.Length >= 3)
			{
				return true;
			}

			return token.StartsWith("\"") && token.EndsWith("\"") && token.Length >= 2;
		}
	}
}
