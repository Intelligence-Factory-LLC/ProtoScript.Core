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
		public void ParseIncludeStatement_WithUnquotedForwardSlashPath_Succeeds()
		{
			ProtoScript.IncludeStatement statement = IncludeStatements.Parse("include Path/File.pts;");
			Assert.AreEqual("Path/File.pts", statement.FileName);
		}

		[TestMethod]
		public void ParseIncludeStatement_WithUnquotedBackslashPath_Succeeds()
		{
			ProtoScript.IncludeStatement statement = IncludeStatements.Parse("include Path\\File.pts;");
			Assert.AreEqual("Path\\File.pts", statement.FileName);
		}

		[TestMethod]
		public void ParseIncludeStatement_WithWhitespaceInUnquotedPath_ThrowsHelpfulError()
		{
			ProtoScriptParsingException err = Assert.ThrowsException<ProtoScriptParsingException>(() =>
				IncludeStatements.Parse("include Path With Space/File.pts;"));

			Assert.AreEqual("path literal", err.Expected);
			Assert.IsTrue(err.Explanation?.Contains("cannot contain whitespace", StringComparison.OrdinalIgnoreCase) ?? false);
		}

		[TestMethod]
		public void ParseFile_WithImportPathAlias_ThrowsHelpfulError()
		{
			ProtoScriptParsingException err = Assert.ThrowsException<ProtoScriptParsingException>(() =>
				ProtoScript.Parsers.Files.ParseFileContents("import Path/File.pts;"));

			Assert.AreEqual("assembly alias", err.Expected);
			Assert.IsTrue(err.Explanation?.Contains("cannot target files", StringComparison.OrdinalIgnoreCase) ?? false);
			Assert.IsTrue(err.Explanation?.Contains("Use include", StringComparison.OrdinalIgnoreCase) ?? false);
		}

		[TestMethod]
		public void ParseFile_WithImportPathAlias_BackslashPath_ThrowsHelpfulError()
		{
			ProtoScriptParsingException err = Assert.ThrowsException<ProtoScriptParsingException>(() =>
				ProtoScript.Parsers.Files.ParseFileContents("import Path\\File.pts;"));

			Assert.AreEqual("assembly alias", err.Expected);
			Assert.IsTrue(err.Explanation?.Contains("cannot target files", StringComparison.OrdinalIgnoreCase) ?? false);
			Assert.IsTrue(err.Explanation?.Contains("Use include", StringComparison.OrdinalIgnoreCase) ?? false);
		}

		[TestMethod]
		public void ParseFile_WithImportPathAlias_FileNameOnly_ThrowsHelpfulError()
		{
			ProtoScriptParsingException err = Assert.ThrowsException<ProtoScriptParsingException>(() =>
				ProtoScript.Parsers.Files.ParseFileContents("import File.pts;"));

			Assert.AreEqual("assembly alias", err.Expected);
			Assert.IsTrue(err.Explanation?.Contains("cannot target files", StringComparison.OrdinalIgnoreCase) ?? false);
			Assert.IsTrue(err.Explanation?.Contains("Use include", StringComparison.OrdinalIgnoreCase) ?? false);
		}

		[TestMethod]
		public void ParseFile_WithLegacyAssemblyImport_StaysImport()
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents("import Ontology Ontology.Collection Collection;");
			Assert.AreEqual(0, file.Includes.Count);
			Assert.AreEqual(1, file.Imports.Count);
			Assert.AreEqual("Ontology", file.Imports[0].Reference);
		}

		[TestMethod]
		public void TryParseImportAsInclude_LegacyAssemblyImport_ReturnsFalseAndRestoresCursor()
		{
			Tokenizer tok = new Tokenizer("import Ontology Ontology.Collection Collection;");
			int startCursor = tok.getCursor();

			bool parsed = IncludeStatements.TryParseImportAsInclude(tok, out ProtoScript.IncludeStatement includeStatement);

			Assert.IsFalse(parsed);
			Assert.IsNull(includeStatement);
			Assert.AreEqual(startCursor, tok.getCursor());
		}

		[TestMethod]
		public void TryParseImportAsInclude_PathImport_ReturnsIncludeWithoutExceptions()
		{
			Tokenizer tok = new Tokenizer("import Path/File.pts;");

			bool parsed = IncludeStatements.TryParseImportAsInclude(tok, out ProtoScript.IncludeStatement includeStatement);

			Assert.IsTrue(parsed);
			Assert.IsNotNull(includeStatement);
			Assert.AreEqual("Path/File.pts", includeStatement.FileName);
		}
	}
}
