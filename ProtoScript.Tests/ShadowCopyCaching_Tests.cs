using ProtoScript.Interpretter;
using System.Reflection;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class ShadowCopyCaching_Tests
	{
		[TestMethod]
		public void PrepareShadowCopyDirectory_UsesSingleDirectoryPerSourceFolder()
		{
			string tempDir = Path.Combine(Path.GetTempPath(), "ProtoScript_ShadowDirReuse_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			try
			{
				string firstDll = Path.Combine(tempDir, "First.dll");
				string secondDll = Path.Combine(tempDir, "Second.dll");
				System.IO.File.WriteAllText(firstDll, "first");
				System.IO.File.WriteAllText(secondDll, "second");

				string firstShadowDir = InvokePrepareShadowCopyDirectory(firstDll);
				string secondShadowDir = InvokePrepareShadowCopyDirectory(secondDll);

				Assert.AreEqual(firstShadowDir, secondShadowDir);
				Assert.IsTrue(System.IO.File.Exists(Path.Combine(firstShadowDir, "First.dll")));
				Assert.IsTrue(System.IO.File.Exists(Path.Combine(firstShadowDir, "Second.dll")));
			}
			finally
			{
				if (Directory.Exists(tempDir))
					Directory.Delete(tempDir, true);
			}
		}

		[TestMethod]
		public void PrepareShadowCopyDirectory_RefreshesChangedSourceDll()
		{
			string tempDir = Path.Combine(Path.GetTempPath(), "ProtoScript_ShadowRefresh_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			try
			{
				string sourceDll = Path.Combine(tempDir, "HotSwap.dll");
				System.IO.File.WriteAllText(sourceDll, "version-1");

				string shadowDir = InvokePrepareShadowCopyDirectory(sourceDll);
				string shadowDll = Path.Combine(shadowDir, "HotSwap.dll");
				Assert.IsTrue(System.IO.File.Exists(shadowDll));
				string firstShadowContent = System.IO.File.ReadAllText(shadowDll);
				Assert.AreEqual("version-1", firstShadowContent);

				System.Threading.Thread.Sleep(1200);
				System.IO.File.WriteAllText(sourceDll, "version-2-with-more-bytes");

				InvokePrepareShadowCopyDirectory(sourceDll);
				string secondShadowContent = System.IO.File.ReadAllText(shadowDll);
				Assert.AreEqual("version-2-with-more-bytes", secondShadowContent);
			}
			finally
			{
				if (Directory.Exists(tempDir))
					Directory.Delete(tempDir, true);
			}
		}

		private static string InvokePrepareShadowCopyDirectory(string sourceAssemblyPath)
		{
			MethodInfo? method = typeof(Compiler).GetMethod(
				"PrepareShadowCopyDirectory",
				BindingFlags.NonPublic | BindingFlags.Static);
			Assert.IsNotNull(method);
			object? result = method!.Invoke(null, new object[] { sourceAssemblyPath });
			Assert.IsNotNull(result);
			return (string)result!;
		}
	}
}
