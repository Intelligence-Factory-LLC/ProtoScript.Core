using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class ReferenceStatementTests
	{
		// Purpose: Ensure reference statements infer the alias when only an assembly name is provided.
		[TestMethod]
		public void ParseReferenceWithImplicitName()
		{
			ProtoScript.ReferenceStatement statement = ReferenceStatements.Parse("reference CSharp.Extensions;;");
			Assert.AreEqual("CSharp.Extensions", statement.AssemblyName);
			Assert.AreEqual("CSharp.Extensions", statement.Reference);
		}
	}
}
