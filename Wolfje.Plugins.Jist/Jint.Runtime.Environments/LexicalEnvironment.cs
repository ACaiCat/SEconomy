using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.References;

namespace Jint.Runtime.Environments
{
	public sealed class LexicalEnvironment
	{
		private readonly EnvironmentRecord _record;

		private readonly LexicalEnvironment _outer;

		public EnvironmentRecord Record => _record;

		public LexicalEnvironment Outer => _outer;

		public LexicalEnvironment(EnvironmentRecord record, LexicalEnvironment outer)
		{
			_record = record;
			_outer = outer;
		}

		public static Reference GetIdentifierReference(LexicalEnvironment lex, string name, bool strict)
		{
			if (lex == null)
			{
				return new Reference(Undefined.Instance, name, strict);
			}
			if (lex.Record.HasBinding(name))
			{
				return new Reference(lex.Record, name, strict);
			}
			if (lex.Outer == null)
			{
				return new Reference(Undefined.Instance, name, strict);
			}
			return GetIdentifierReference(lex.Outer, name, strict);
		}

		public static LexicalEnvironment NewDeclarativeEnvironment(Engine engine, LexicalEnvironment outer = null)
		{
			return new LexicalEnvironment(new DeclarativeEnvironmentRecord(engine), outer);
		}

		public static LexicalEnvironment NewObjectEnvironment(Engine engine, ObjectInstance objectInstance, LexicalEnvironment outer, bool provideThis)
		{
			return new LexicalEnvironment(new ObjectEnvironmentRecord(engine, objectInstance, provideThis), outer);
		}
	}
}
