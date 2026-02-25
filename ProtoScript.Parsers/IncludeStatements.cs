using BasicUtilities;
using System.Text;

namespace ProtoScript.Parsers
{
	public class IncludeStatements
	{
		private static readonly HashSet<string> AllowedPathOperators = new HashSet<string>(StringComparer.Ordinal)
		{
			".",
			"/",
			"\\",
			":",
			"*",
			"?",
			"-"
		};

		static public ProtoScript.IncludeStatement Parse(string strStatement)
		{
			Tokenizer tok = new Tokenizer(strStatement);
			return Parse(tok);
		}

		public static ProtoScript.IncludeStatement Parse(Tokenizer tok)
		{
			ProtoScript.IncludeStatement result = new ProtoScript.IncludeStatement();

			tok.movePastWhitespace();
			result.Info.StartStatement(tok.getCursor());
			result.Info.File = Files.CurrentFile;

			tok.MustBeNext("include");

			if (tok.CouldBeNext("recursive"))
			{
				result.Recursive = true;
			}

			result.FileName = ParsePathLiteral(tok, "include");

			tok.MustBeNext(";");

			result.Info.StopStatement(tok.getCursor());

			return result;
		}

		public static bool TryParseImportAsInclude(Tokenizer tok, out ProtoScript.IncludeStatement includeStatement)
		{
			includeStatement = null;
			int saveCursor = tok.getCursor();
			try
			{
				tok.movePastWhitespace();
				int startCursor = tok.getCursor();
				tok.MustBeNext("import");

				string fileName = ParsePathLiteral(tok, "import");
				tok.MustBeNext(";");

				if (!LooksLikeProtoScriptPath(fileName))
				{
					tok.setCursor(saveCursor);
					return false;
				}

				includeStatement = new ProtoScript.IncludeStatement();
				includeStatement.Info.File = Files.CurrentFile;
				includeStatement.Info.StartStatement(startCursor);
				includeStatement.FileName = fileName;
				includeStatement.Info.StopStatement(tok.getCursor());
				return true;
			}
			catch (ProtoScriptTokenizingException)
			{
				tok.setCursor(saveCursor);
				return false;
			}
		}

		public static string ParsePathLiteral(Tokenizer tok, string statementKeyword)
		{
			string token = tok.peekNextToken();
			if (IsQuotedPathLiteral(token))
			{
				return NormalizeQuotedPathLiteral(tok.getNextToken());
			}

			return ParseUnquotedPathLiteral(tok, statementKeyword);
		}

		private static string ParseUnquotedPathLiteral(Tokenizer tok, string statementKeyword)
		{
			int startCursor = -1;
			int stopCursor = -1;
			StringBuilder strPath = new StringBuilder();

			while (tok.hasMoreTokens())
			{
				string token = tok.peekNextToken();
				if (token == ";")
				{
					break;
				}

				if (!IsAllowedUnquotedPathToken(tok, token))
				{
					throw BuildPathLiteralException(
						tok,
						statementKeyword,
						"path literal",
						$"{FormatStatementName(statementKeyword)} path contains unsupported token '{token}'. Use quotes for complex paths.");
				}

				string consumed = tok.getNextToken();
				int consumedStop = tok.getCursor();
				int consumedStart = consumedStop - consumed.Length;
				if (startCursor < 0)
				{
					startCursor = consumedStart;
				}

				strPath.Append(consumed);
				stopCursor = consumedStop;
			}

			if (strPath.Length == 0)
			{
				throw BuildPathLiteralException(
					tok,
					statementKeyword,
					"path literal",
					$"{FormatStatementName(statementKeyword)} path is missing. Example: {statementKeyword} \"Path/File.pts\";");
			}

			if (ContainsWhitespace(tok.getString(), startCursor, stopCursor))
			{
				throw BuildPathLiteralException(
					tok,
					statementKeyword,
					"path literal",
					$"Unquoted {statementKeyword} path cannot contain whitespace. Quote the path instead.");
			}

			return strPath.ToString();
		}

		private static bool IsAllowedUnquotedPathToken(Tokenizer tok, string token)
		{
			if (string.IsNullOrEmpty(token))
			{
				return false;
			}

			if (tok.IsOperator(token))
			{
				return AllowedPathOperators.Contains(token);
			}

			if (token.Length == 1 && tok.isSymbol(token[0]))
			{
				return false;
			}

			return true;
		}

		private static bool IsQuotedPathLiteral(string token)
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

		private static string NormalizeQuotedPathLiteral(string token)
		{
			if (token.StartsWith("@\"") && token.EndsWith("\""))
			{
				return StringUtil.BetweenQuotes(token).Replace("\"\"", "\"");
			}

			return StringUtil.BetweenQuotes(token);
		}

		private static bool LooksLikeProtoScriptPath(string path)
		{
			return path.EndsWith(".pts", StringComparison.OrdinalIgnoreCase)
				|| path.Contains("/")
				|| path.Contains("\\")
				|| path.Contains(":");
		}

		private static bool ContainsWhitespace(string source, int start, int stop)
		{
			if (string.IsNullOrEmpty(source) || stop <= start || start < 0)
			{
				return false;
			}

			int max = Math.Min(stop, source.Length);
			for (int i = start; i < max; i++)
			{
				if (char.IsWhiteSpace(source[i]))
				{
					return true;
				}
			}

			return false;
		}

		private static ProtoScriptParsingException BuildPathLiteralException(Tokenizer tok, string statementKeyword, string expected, string explanation)
		{
			return new ProtoScriptParsingException(tok.getString(), tok.getCursor(), expected, explanation);
		}

		private static string FormatStatementName(string statementKeyword)
		{
			if (string.IsNullOrEmpty(statementKeyword))
			{
				return "Statement";
			}

			return char.ToUpperInvariant(statementKeyword[0]) + statementKeyword.Substring(1);
		}
	}
}
