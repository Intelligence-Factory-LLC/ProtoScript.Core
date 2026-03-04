using ProtoScript.Extensions;
using WebAppUtilities;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class WorkbenchJsonSerializationAnnotations_Tests
	{
		[TestMethod]
		public void Workbench_Methods_UseFullSerialization()
		{
			AssertMethodUsesFullSerialization(nameof(ProtoScriptWorkbench.CompileCode), new System.Type[] { typeof(string) });
			AssertMethodUsesFullSerialization(nameof(ProtoScriptWorkbench.CompileCodeWithProject), new System.Type[] { typeof(string), typeof(string) });
			AssertMethodUsesFullSerialization(nameof(ProtoScriptWorkbench.InterpretImmediate), new System.Type[] { typeof(string), typeof(string), typeof(ProtoScriptWorkbench.TaggingSettings) });
			AssertMethodUsesFullSerialization(nameof(ProtoScriptWorkbench.TagImmediate), new System.Type[] { typeof(string), typeof(string), typeof(ProtoScriptWorkbench.TaggingSettings) });
		}

		private static void AssertMethodUsesFullSerialization(string methodName, System.Type[] parameters)
		{
			System.Reflection.MethodInfo? method = typeof(ProtoScriptWorkbench).GetMethod(methodName, parameters);
			Assert.IsNotNull(method, $"Method not found: {methodName}");

			JsonWsSerializeAttribute? attribute = method!.GetCustomAttributes(typeof(JsonWsSerializeAttribute), false)
				.Cast<JsonWsSerializeAttribute>()
				.FirstOrDefault();

			Assert.IsNotNull(attribute, $"Expected [JsonWsSerialize] on method: {methodName}");
			Assert.AreEqual(JsonWs.SerializeResultsOptions.Full, attribute!.Mode, $"Expected Full serialization mode for method: {methodName}");
		}
	}
}
