using Ontology;
using Ontology.BaseTypes;
using Ontology.Simulation;
using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class NativeInterpretterEnumerableReturn_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void ImmediatePrototypeEnumerableMethods_ReturnCollectionPrototype()
		{
			(NativeInterpretter interpretter, Compiler compiler) setup = BuildInterpreter(@"
prototype CoreAction : ProtoScriptAction;
prototype CoreSkill : CoreAction;
");

			Prototype? descendants = EvaluateExpressionAsPrototype(setup.interpretter, setup.compiler, "CoreSkill.GetDescendants()");
			Prototype? allDescendants = EvaluateExpressionAsPrototype(setup.interpretter, setup.compiler, "CoreSkill.GetAllDescendants()");

			Assert.IsNotNull(descendants);
			Assert.IsNotNull(allDescendants);
			Assert.IsInstanceOfType(descendants, typeof(Collection));
			Assert.IsInstanceOfType(allDescendants, typeof(Collection));
		}

		[TestMethod]
		public void ImmediatePrimitiveEnumerableMethods_ReturnNativeValuePrototype()
		{
			(NativeInterpretter interpretter, Compiler compiler) setup = BuildInterpreter(@"
prototype CoreAction : ProtoScriptAction;
prototype CoreSkill : CoreAction;
");

			Prototype? typeOfs = EvaluateExpressionAsPrototype(setup.interpretter, setup.compiler, "CoreSkill.GetTypeOfs()");

			Assert.IsNotNull(typeOfs);
			Assert.IsInstanceOfType(typeOfs, typeof(NativeValuePrototype));
		}

		private static (NativeInterpretter interpretter, Compiler compiler) BuildInterpreter(string code)
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);

			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File fileCompiled = compiler.Compile(file);

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(fileCompiled);

			return (interpretter, compiler);
		}

		private static Prototype? EvaluateExpressionAsPrototype(NativeInterpretter interpretter, Compiler compiler, string expression)
		{
			ProtoScript.Expression immediate = ProtoScript.Parsers.Expressions.Parse(expression);
			ProtoScript.Interpretter.Compiled.Expression compiledImmediate = compiler.Compile(immediate);
			object? result = interpretter.Evaluate(compiledImmediate);
			return interpretter.GetOrConvertToPrototype(result);
		}
	}
}
