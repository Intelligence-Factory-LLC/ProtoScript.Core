using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoScript.Interpretter;
using ProtoScript.Parsers;
using System.Reflection;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class ReferenceStatementTests
	{
		// Purpose: Ensure reference statements infer the alias when only an assembly name is provided.
		[TestMethod]
		public void ParseReferenceWithImplicitName()
		{
			ProtoScript.ReferenceStatement statement = ReferenceStatements.Parse("reference CSharp.Extensions;");
			Assert.AreEqual("CSharp.Extensions", statement.AssemblyName);
			Assert.AreEqual("CSharp.Extensions", statement.Reference);
			Assert.IsFalse(statement.IsFileReference);
		}

		[TestMethod]
		public void ParseReferencePathWithImplicitAlias()
		{
			ProtoScript.ReferenceStatement statement = ReferenceStatements.Parse("reference \"Skills/TwitterXApi/lib/Buffaly.XApi.dll\";");
			Assert.IsTrue(statement.IsFileReference);
			Assert.AreEqual("Skills/TwitterXApi/lib/Buffaly.XApi.dll", statement.AssemblyName);
			Assert.AreEqual("Buffaly.XApi", statement.Reference);
		}

		[TestMethod]
		public void CompileReferencePath_MissingDll_FailsFast()
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.ReferenceStatement statement = ReferenceStatements.Parse("reference \"missing/missing-lib.dll\";");

			compiler.Compile(statement);

			Assert.IsTrue(compiler.Diagnostics.Any(x => x.Diagnostic.Message.Contains("Reference DLL not found", StringComparison.OrdinalIgnoreCase)));
		}

		[TestMethod]
		public void CompileReferencePath_LoadsAssembly_AndImportBindsType()
		{
			string parsersAssemblyPath = typeof(Files).Assembly.Location.Replace("\\", "/");
			string code = $@"
reference ""{parsersAssemblyPath}"" ParsersAsm;
import ParsersAsm ProtoScript.Parsers.Files FilesParser;
";
			ProtoScript.File file = Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Compile(file);

			Assert.AreEqual(0, compiler.Diagnostics.Count);
			Assert.IsTrue(compiler.References.ContainsKey("ParsersAsm"));
			Assert.IsNotNull(compiler.Symbols.GetTypeInfo("FilesParser"));
		}

		[TestMethod]
		public void CompileReferencePath_IsCachedByFullPathAcrossAliases()
		{
			string parsersAssemblyPath = typeof(Files).Assembly.Location.Replace("\\", "/");
			string code = $@"
reference ""{parsersAssemblyPath}"" AliasA;
reference ""{parsersAssemblyPath}"" AliasB;
";
			ProtoScript.File file = Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Compile(file);

			Assert.AreEqual(0, compiler.Diagnostics.Count);
			Assert.IsTrue(compiler.References.TryGetValue("AliasA", out object? aliasA));
			Assert.IsTrue(compiler.References.TryGetValue("AliasB", out object? aliasB));
			Assert.AreSame(aliasA as Assembly, aliasB as Assembly);
		}

		[TestMethod]
		public void CompileReferencePath_ResolvesRelativeToSourceFile()
		{
			string parsersAssemblyFullPath = typeof(Files).Assembly.Location;
			string assemblyFileName = Path.GetFileName(parsersAssemblyFullPath);
			string sourceDir = Path.GetDirectoryName(parsersAssemblyFullPath)!;

			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.ReferenceStatement statement = ReferenceStatements.Parse($"reference \"{assemblyFileName}\" ParsersAsm;");
			statement.Info.File = Path.Combine(sourceDir, "TestFile.pts");

			compiler.Compile(statement);

			Assert.AreEqual(0, compiler.Diagnostics.Count);
			Assert.IsTrue(compiler.References.ContainsKey("ParsersAsm"));
			Assert.AreEqual(Path.GetFullPath(parsersAssemblyFullPath), statement.ResolvedAssemblyPath);
		}

		[TestMethod]
		public void CompileReferencePath_UsesAlreadyLoadedAssemblyByIdentity()
		{
			string sourceAssemblyPath = typeof(Files).Assembly.Location;
			string tempDir = Path.Combine(Path.GetTempPath(), "ProtoScriptRefIdentity_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			try
			{
				string copiedAssemblyPath = Path.Combine(tempDir, Path.GetFileName(sourceAssemblyPath));
				System.IO.File.Copy(sourceAssemblyPath, copiedAssemblyPath, true);

				string code = $@"
reference ""{copiedAssemblyPath.Replace("\\", "/")}"" CopiedAsm;
import CopiedAsm ProtoScript.Parsers.Files FilesParser;
";
				ProtoScript.File file = Files.ParseFileContents(code);
				Compiler compiler = new Compiler();
				compiler.Initialize();
				compiler.Compile(file);

				Assert.AreEqual(0, compiler.Diagnostics.Count);
				Assert.IsTrue(compiler.References.TryGetValue("CopiedAsm", out object? loadedObj));
				Assert.AreSame(typeof(Files).Assembly, loadedObj as Assembly);
			}
			finally
			{
				if (Directory.Exists(tempDir))
					Directory.Delete(tempDir, true);
			}
		}

		[TestMethod]
		public void GetReferenceAssemblyInfos_IncludesVersionAndModifiedDate_ForFileReference()
		{
			string parsersAssemblyPath = typeof(Files).Assembly.Location.Replace("\\", "/");
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.ReferenceStatement statement = ReferenceStatements.Parse($"reference \"{parsersAssemblyPath}\" ParsersAsm;");

			compiler.Compile(statement);

			ReferenceAssemblyInfo? info = compiler
				.GetReferenceAssemblyInfos()
				.FirstOrDefault(x => x.Alias == "ParsersAsm");

			Assert.IsNotNull(info);
			Assert.IsTrue(info!.LoadSucceeded);
			Assert.IsFalse(string.IsNullOrWhiteSpace(info.AssemblyVersion));
			Assert.IsTrue(info.LastWriteUtc.HasValue);
			Assert.AreEqual(Path.GetFullPath(typeof(Files).Assembly.Location), Path.GetFullPath(info.LoadedLocation!));
		}

		[TestMethod]
		public void GetReferenceAssemblyReport_IsDeterministicAndContainsAlias()
		{
			string parsersAssemblyPath = typeof(Files).Assembly.Location.Replace("\\", "/");
			string code = $@"
reference ""{parsersAssemblyPath}"" ZAlias;
reference ""{parsersAssemblyPath}"" AAlias;
";
			ProtoScript.File file = Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Compile(file);

			string report = compiler.GetReferenceAssemblyReport();

			Assert.IsTrue(report.Contains("alias=AAlias", StringComparison.Ordinal));
			Assert.IsTrue(report.Contains("alias=ZAlias", StringComparison.Ordinal));
			Assert.IsTrue(report.IndexOf("alias=AAlias", StringComparison.Ordinal) < report.IndexOf("alias=ZAlias", StringComparison.Ordinal));
		}
	}
}
