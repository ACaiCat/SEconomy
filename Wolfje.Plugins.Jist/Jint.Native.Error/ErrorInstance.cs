using Jint.Native.Object;
using Jint.Runtime;

namespace Jint.Native.Error
{
	public class ErrorInstance : ObjectInstance
	{
		public override string Class => "Error";

		public ErrorInstance(Engine engine, string name)
			: base(engine)
		{
			FastAddProperty("name", name, writable: true, enumerable: false, configurable: true);
		}

		public override string ToString()
		{
			return base.Engine.Error.PrototypeObject.ToString(this, Arguments.Empty).ToObject().ToString();
		}
	}
}
