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
		public void ParseFile_WithImportPathAlias_AddsInclude()
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents("import Path/File.pts;");
			Assert.AreEqual(1, file.Includes.Count);
			Assert.AreEqual(0, file.Imports.Count);
			Assert.AreEqual("Path/File.pts", file.Includes[0].FileName);
		}

		[TestMethod]
		public void ParseFile_WithImportPathAlias_BackslashPath_AddsInclude()
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents("import Path\\File.pts;");
			Assert.AreEqual(1, file.Includes.Count);
			Assert.AreEqual(0, file.Imports.Count);
			Assert.AreEqual("Path\\File.pts", file.Includes[0].FileName);
		}

		[TestMethod]
		public void ParseFile_WithImportPathAlias_FileNameOnly_AddsInclude()
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents("import File.pts;");
			Assert.AreEqual(1, file.Includes.Count);
			Assert.AreEqual(0, file.Imports.Count);
			Assert.AreEqual("File.pts", file.Includes[0].FileName);
		}

		[TestMethod]
		public void ParseFile_WithLegacyAssemblyImport_StaysImport()
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents("import Ontology Ontology.Collection Collection;");
			Assert.AreEqual(0, file.Includes.Count);
			Assert.AreEqual(1, file.Imports.Count);
			Assert.AreEqual("Ontology", file.Imports[0].Reference);
		}
	}
}
