using Ontology.Simulation;
using ProtoScript.Interpretter;
using System.Reflection;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class ReflectionUtilTests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		private static class OverloadTarget
		{
			public static string Echo(int v) => "int";
			public static string Echo(string v) => "string";
			public static string Echo(object v) => "object";

			public static string Add(int a, int b) => "int-int";
			public static string Add(object a, object b) => "obj-obj";
		}

		// Purpose: Confirm overload resolution prefers exact string overload over object overload.
		[TestMethod]
		public void GetMethod_PicksStringOverObject()
		{
			MethodInfo? info = ReflectionUtil.GetMethod(
				typeof(OverloadTarget),
				"Echo",
				new List<System.Type> { typeof(string) });
			Assert.IsNotNull(info);
			Assert.AreEqual(typeof(string), info.GetParameters()[0].ParameterType);
		}

		// Purpose: Confirm wrapped integer types resolve to the integer overload.
		[TestMethod]
		public void GetMethod_PicksIntOverObjectWhenUsingWrapper()
		{
			MethodInfo? info = ReflectionUtil.GetMethod(
				typeof(OverloadTarget),
				"Echo",
				new List<System.Type> { typeof(IntWrapper) });
			Assert.IsNotNull(info);
			Assert.AreEqual(typeof(int), info.GetParameters()[0].ParameterType);
		}

		// Purpose: Confirm overload resolution handles multi-parameter matches correctly.
		[TestMethod]
		public void GetMethod_HandlesMultipleParameters()
		{
			MethodInfo? info = ReflectionUtil.GetMethod(
				typeof(OverloadTarget),
				"Add",
				new List<System.Type> { typeof(IntWrapper), typeof(IntWrapper) });
			Assert.IsNotNull(info);
			ParameterInfo[] p = info.GetParameters();
			Assert.AreEqual(typeof(int), p[0].ParameterType);
			Assert.AreEqual(typeof(int), p[1].ParameterType);
		}

		// Purpose: Confirm overload resolution supports params methods for single char arguments.
		[TestMethod]
		public void GetMethod_ResolvesStringTrimEndWithChar()
		{
			MethodInfo? info = ReflectionUtil.GetMethod(
				typeof(string),
				"TrimEnd",
				new List<System.Type> { typeof(char) });
			Assert.IsNotNull(info);
			ParameterInfo[] p = info.GetParameters();
			Assert.AreEqual(1, p.Length);
			Assert.IsTrue(p[0].ParameterType == typeof(char) || p[0].ParameterType == typeof(char[]));
		}
	}
}
