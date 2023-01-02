using Jint.Native.Object;
using Jint.Runtime;

namespace Jint.Native.Boolean
{
	public class BooleanInstance : ObjectInstance, IPrimitiveInstance
	{
		public override string Class => "Boolean";

		Types IPrimitiveInstance.Type => Types.Boolean;

		JsValue IPrimitiveInstance.PrimitiveValue => PrimitiveValue;

		public JsValue PrimitiveValue { get; set; }

		public BooleanInstance(Engine engine)
			: base(engine)
		{
		}
	}
}
