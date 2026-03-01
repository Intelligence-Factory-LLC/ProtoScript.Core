using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class CompilerExceptionShaping_Tests
	{
		[TestMethod]
		public void CompileFileList_UnexpectedException_IsWrappedWithStageAndFile()
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();

			ProtoScript.File file = new ProtoScript.File
			{
				Info = new FileInfo(@"C:\temp\Broken.pts"),
				RawCode = string.Empty,
				Namespaces = null
			};

			ProtoScriptCompilerException ex = Assert.ThrowsException<ProtoScriptCompilerException>(() => compiler.Compile(file));
			Assert.IsTrue(ex.Explanation.Contains("Compilation failed during DeclareNamespaces", StringComparison.Ordinal));
			Assert.AreEqual(@"C:\temp\Broken.pts", ex.File);
			Assert.AreEqual(@"C:\temp\Broken.pts", ex.Info.File);
		}
	}
}
