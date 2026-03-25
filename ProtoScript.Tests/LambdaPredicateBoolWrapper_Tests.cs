using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class LambdaPredicateBoolWrapper_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void WherePredicate_BooleanReturnFromHelper_DoesNotThrowCastAndFilters()
		{
			const string code = @"
prototype Item
{
	Boolean Flag;
}

function IsFlagged(Item item) : Boolean
{
	return item.Flag;
}

function Filter() : Collection
{
	Collection items = new Collection();

	Item a = new Item();
	a.Flag = true;
	items.Add(a);

	Item b = new Item();
	b.Flag = false;
	items.Add(b);

	return items.Where(x => IsFlagged((x as Item)));
}
";

			ProtoScript.File file = Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);

			object? result = interpretter.RunMethodAsObject(null, "Filter", new List<object>());
			Assert.IsNotNull(result);
			Assert.IsInstanceOfType<Collection>(result);

			Collection filtered = (Collection)result;
			Assert.AreEqual(1, filtered.Count);
		}
	}
}
