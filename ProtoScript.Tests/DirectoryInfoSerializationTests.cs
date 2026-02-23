using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class DirectoryInfoSerializationTests
	{
		[TestInitialize]
		public void TestInitialize()
		{
			Initializer.Initialize();
		}

		// Purpose: Ensure DirectoryInfo values are not converted into recursive graph structures.
		[TestMethod]
		public void Serialize_DirectoryInfo_DoesNotRecurse()
		{
			System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo("__nonexistent_for_serialization_test__");
			NativeValuePrototype? prototype = NativeValuePrototypes.ToPrototype(directoryInfo);

			Assert.IsNull(prototype);
		}
	}
}
