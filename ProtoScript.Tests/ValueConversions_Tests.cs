using Ontology.Simulation;
using ProtoScript.Interpretter;
using ProtoScript.Interpretter.RuntimeInfo;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class ValueConversions_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		private static object? RunGlobalFunction(string code, string methodName)
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			return interpretter.RunMethodAsObject(null, methodName, new List<object>());
		}

		// Purpose: Ensure NativeValuePrototype primitives are converted to CLR primitives.
		[TestMethod]
		public void GetAs_NativeValuePrototype_Primitives()
		{
			NativeValuePrototype stringValue = StringWrapper.ToPrototype("abc");
			NativeValuePrototype intValue = IntWrapper.ToPrototype(7);
			NativeValuePrototype boolValue = BoolWrapper.ToPrototype(true);
			NativeValuePrototype doubleValue = DoubleWrapper.ToPrototype(2.5);

			Assert.AreEqual("abc", ValueConversions.GetAs(stringValue, typeof(string)));
			Assert.AreEqual(7, ValueConversions.GetAs(intValue, typeof(int)));
			Assert.AreEqual(true, ValueConversions.GetAs(boolValue, typeof(bool)));
			Assert.AreEqual(2.5, ValueConversions.GetAs(doubleValue, typeof(double)));
		}

		// Purpose: Ensure ValueRuntimeInfo values are unwrapped before conversion.
		[TestMethod]
		public void GetAs_ValueRuntimeInfo_UnwrapsAndConverts()
		{
			ValueRuntimeInfo info = new ValueRuntimeInfo();
			info.Type = new TypeInfo(typeof(StringWrapper));
			info.Value = StringWrapper.ToPrototype("runtime");

			object? result = ValueConversions.GetAs(info, typeof(string));
			Assert.AreEqual("runtime", result);
		}

		// Purpose: Verify primitive CLR values are converted into prototype-backed values.
		[TestMethod]
		public void ToPrototype_ConvertsClrPrimitives()
		{
			Prototype? str = ValueConversions.ToPrototype("abc");
			Prototype? i = ValueConversions.ToPrototype(42);
			Prototype? b = ValueConversions.ToPrototype(true);
			Prototype? d = ValueConversions.ToPrototype(12.75);

			Assert.IsNotNull(str);
			Assert.IsNotNull(i);
			Assert.IsNotNull(b);
			Assert.IsNotNull(d);

			Assert.AreEqual("abc", StringWrapper.ToString(str));
			Assert.AreEqual(42, IntWrapper.ToInteger(i));
			Assert.AreEqual(true, BoolWrapper.ToBoolean(b));
			Assert.AreEqual(12.75, DoubleWrapper.ToDouble(d));
		}

		// Purpose: Verify centralized assignment conversion handles both directions for wrapper/primitive values.
		[TestMethod]
		public void TryMakeAssignable_PrimitiveWrapperRoundTrip()
		{
			object? converted;
			bool okPrimitiveToWrapper = ValueConversions.TryMakeAssignable("hello", new TypeInfo(typeof(StringWrapper)), out converted);
			Assert.IsTrue(okPrimitiveToWrapper);
			Assert.IsTrue(converted is StringWrapper);

			bool okWrapperToPrimitive = ValueConversions.TryMakeAssignable(new IntWrapper(9), new TypeInfo(typeof(int)), out converted);
			Assert.IsTrue(okWrapperToPrimitive);
			Assert.AreEqual(9, converted);
		}

		// Purpose: Confirm type-compatibility rules still support wrapper-to-primitive and primitive-to-wrapper checks.
		[TestMethod]
		public void IsAssignableFrom_WrapperPrimitiveMappings()
		{
			Assert.IsTrue(ValueConversions.IsAssignableFrom(new TypeInfo(typeof(StringWrapper)), new TypeInfo(typeof(string))));
			Assert.IsTrue(ValueConversions.IsAssignableFrom(new TypeInfo(typeof(string)), new TypeInfo(typeof(StringWrapper))));
			Assert.IsTrue(ValueConversions.IsAssignableFrom(new TypeInfo(typeof(IntWrapper)), new TypeInfo(typeof(int))));
			Assert.IsTrue(ValueConversions.IsAssignableFrom(new TypeInfo(typeof(int)), new TypeInfo(typeof(IntWrapper))));
		}

		// Purpose: Validate string concatenation with a '#'-qualified prototype instance property.
		[TestMethod]
		public void Concatenation_InstanceProperty_ReturnsExpectedCommand()
		{
			string code = @"
prototype GitHubOrg
{
	String OrgName = ""Intelligence_Factory_LLC"";
}

prototype GitHubOrg#Intelligence_Factory_LLC : GitHubOrg
{
}

function BuildCommand() : string
{
	return ""repo list "" + GitHubOrg#Intelligence_Factory_LLC.OrgName + "" --limit 100"";
}
";
			object? result = RunGlobalFunction(code, "BuildCommand");
			Assert.AreEqual("repo list Intelligence_Factory_LLC --limit 100", result);
		}

		// Purpose: Validate string concatenation with child-property resolution from a '#'-qualified instance.
		[TestMethod]
		public void Concatenation_ChildProperty_ReturnsExpectedCommand()
		{
			string code = @"
prototype GitHubOrgChild
{
	String OrgName = ""Intelligence_Factory_LLC"";
}

prototype GitHubOrg
{
	GitHubOrgChild Child = new GitHubOrgChild();
}

prototype GitHubOrg#Intelligence_Factory_LLC : GitHubOrg
{
}

function BuildCommand() : string
{
	return ""repo list "" + GitHubOrg#Intelligence_Factory_LLC.Child.OrgName + "" --limit 100"";
}
";
			object? result = RunGlobalFunction(code, "BuildCommand");
			Assert.AreEqual("repo list Intelligence_Factory_LLC --limit 100", result);
		}

		// Purpose: Guard against method cache regressions in chained string member access.
		[TestMethod]
		public void MethodCallOnStringWrapper_ChainedMemberAccess_IsStable()
		{
			string code = @"
function main() : int
{
	String str = ""123"";
	return str.GetStringValue().Length;
}
";
			object? result = RunGlobalFunction(code, "main");
			Assert.IsTrue(result is int);
			Assert.AreEqual(3, (int)result);
		}

		// Purpose: Ensure string concatenation auto-converts int operands in JSON argument builders.
		[TestMethod]
		public void Concatenation_StringAndInt_BuildsJson()
		{
			string code =
"prototype CreatorSiteEndpoint\r\n" +
"{\r\n" +
"	String RootUrl = new String();\r\n" +
"}\r\n" +
"\r\n" +
"function Execute(CreatorSiteEndpoint endpoint, int siteID, string token) : string\r\n" +
"{\r\n" +
"	string argsJson = \"{\\\"SiteID\\\":\" + siteID + \"}\";\r\n" +
"	return argsJson;\r\n" +
"}\r\n" +
"\r\n" +
"function main() : string\r\n" +
"{\r\n" +
"	CreatorSiteEndpoint endpoint = new CreatorSiteEndpoint();\r\n" +
"	return Execute(endpoint, 42, \"\");\r\n" +
"}\r\n";

			object? result = RunGlobalFunction(code, "main");
			Assert.AreEqual("{\"SiteID\":42}", result);
		}
	}
}
