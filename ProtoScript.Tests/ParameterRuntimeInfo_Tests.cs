using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoScript.Interpretter.RuntimeInfo;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class ParameterRuntimeInfo_Tests
	{
		// Purpose: Ensure cloning preserves parameter identity metadata.
		[TestMethod]
		public void Clone_RetainsParameterName()
		{
			ParameterRuntimeInfo param = new ParameterRuntimeInfo();
			param.ParameterName = "foo";
			ParameterRuntimeInfo clone = (ParameterRuntimeInfo)param.Clone();
			Assert.AreEqual(param.ParameterName, clone.ParameterName);
		}
	}
}
