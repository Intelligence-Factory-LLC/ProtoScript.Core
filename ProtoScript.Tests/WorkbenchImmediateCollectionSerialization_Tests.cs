using ProtoScript.Extensions;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class WorkbenchImmediateCollectionSerialization_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void InterpretImmediate_GetDescendants_SerializesCollectionChildren()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				string projectPath = Path.Combine(tempDir, "Project.pts");
				System.IO.File.WriteAllText(
					projectPath,
@"include ""Skill.pts"";");

				System.IO.File.WriteAllText(
					Path.Combine(tempDir, "Skill.pts"),
@"prototype Root;
prototype ChildA : Root;
prototype ChildB : Root;");

				ProtoScriptWorkbench.TaggingSettings settings = new ProtoScriptWorkbench.TaggingSettings()
				{
					Project = projectPath
				};

				ProtoScriptWorkbench.TagImmediateResult result =
					ProtoScriptWorkbench.InterpretImmediate(projectPath, "Root.GetDescendants()", settings);

				Assert.IsTrue(string.IsNullOrWhiteSpace(result.Error), "Unexpected immediate error: " + result.Error);
				Assert.IsFalse(string.IsNullOrWhiteSpace(result.Result), "Expected a serialized immediate result.");
				Assert.IsTrue(result.Result!.Contains("ChildA"), "Serialized result should contain ChildA.");
				Assert.IsTrue(result.Result!.Contains("ChildB"), "Serialized result should contain ChildB.");
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		private static string CreateTempDirectory()
		{
			string path = Path.Combine(Path.GetTempPath(), "ProtoScriptWorkbenchImmediate_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(path);
			return path;
		}

		private static void DeleteDirectory(string path)
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}
		}
	}
}
