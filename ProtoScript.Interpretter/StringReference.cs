using Ontology;
using Ontology.BaseTypes;
using Ontology.Simulation;

namespace ProtoScript.Interpretter
{
	// Opaque handle for large string values that crosses the C#/ProtoScript boundary by prototype name.
	public sealed class StringReference
	{
		public string PrototypeName { get; }

		public StringReference(string prototypeName)
		{
			if (string.IsNullOrWhiteSpace(prototypeName))
				throw new ArgumentException("prototypeName cannot be null or whitespace.", nameof(prototypeName));

			PrototypeName = prototypeName;
		}

		public static StringReference FromString(string value)
		{
			Prototype prototype = StringWrapper.ToPrototype(value ?? string.Empty);
			return new StringReference(prototype.PrototypeName);
		}

		public static bool TryFromPrototype(Prototype? prototype, out StringReference? reference)
		{
			reference = null;
			if (prototype == null || !Prototypes.TypeOf(prototype, System_String.Prototype))
				return false;

			reference = new StringReference(prototype.PrototypeName);
			return true;
		}

		public bool TryResolvePrototype(out Prototype? prototype)
		{
			prototype = null;

			Prototype? temporary = TemporaryPrototypes.GetTemporaryPrototypeOrNull(PrototypeName);
			if (temporary != null && Prototypes.TypeOf(temporary, System_String.Prototype))
			{
				prototype = temporary;
				return true;
			}

			Prototype? global = Prototypes.GetPrototypeByPrototypeName(PrototypeName);
			if (global != null && Prototypes.TypeOf(global, System_String.Prototype))
			{
				prototype = global;
				return true;
			}

			return false;
		}

		public bool TryResolveString(out string? value)
		{
			value = null;
			if (!TryResolvePrototype(out Prototype? prototype) || prototype == null)
				return false;

			value = StringWrapper.ToString(prototype);
			return true;
		}

		public override string ToString()
		{
			return PrototypeName;
		}
	}
}
