using Jint.Native;
using Jint.Runtime.Environments;

namespace Jint.Runtime.References
{
	public class Reference
	{
		private readonly JsValue _baseValue;

		private readonly string _name;

		private readonly bool _strict;

		public Reference(JsValue baseValue, string name, bool strict)
		{
			_baseValue = baseValue;
			_name = name;
			_strict = strict;
		}

		public JsValue GetBase()
		{
			return _baseValue;
		}

		public string GetReferencedName()
		{
			return _name;
		}

		public bool IsStrict()
		{
			return _strict;
		}

		public bool HasPrimitiveBase()
		{
			return _baseValue.IsPrimitive();
		}

		public bool IsUnresolvableReference()
		{
			return _baseValue.IsUndefined();
		}

		public bool IsPropertyReference()
		{
			if (!_baseValue.IsObject() || _baseValue.Is<EnvironmentRecord>())
			{
				return HasPrimitiveBase();
			}
			return true;
		}
	}
}
