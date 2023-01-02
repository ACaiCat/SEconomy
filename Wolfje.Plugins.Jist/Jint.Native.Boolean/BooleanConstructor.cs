using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime;

namespace Jint.Native.Boolean
{
	public sealed class BooleanConstructor : FunctionInstance, IConstructor
	{
		public BooleanPrototype PrototypeObject { get; private set; }

		private BooleanConstructor(Engine engine)
			: base(engine, null, null, strict: false)
		{
		}

		public static BooleanConstructor CreateBooleanConstructor(Engine engine)
		{
			BooleanConstructor booleanConstructor = new BooleanConstructor(engine);
			booleanConstructor.Extensible = true;
			booleanConstructor.Prototype = engine.Function.PrototypeObject;
			booleanConstructor.PrototypeObject = BooleanPrototype.CreatePrototypeObject(engine, booleanConstructor);
			booleanConstructor.FastAddProperty("length", 1.0, writable: false, enumerable: false, configurable: false);
			booleanConstructor.FastAddProperty("prototype", booleanConstructor.PrototypeObject, writable: false, enumerable: false, configurable: false);
			return booleanConstructor;
		}

		public void Configure()
		{
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			if (arguments.Length == 0)
			{
				return false;
			}
			return TypeConverter.ToBoolean(arguments[0]);
		}

		public ObjectInstance Construct(JsValue[] arguments)
		{
			return Construct(TypeConverter.ToBoolean(arguments.At(0)));
		}

		public BooleanInstance Construct(bool value)
		{
			BooleanInstance booleanInstance = new BooleanInstance(base.Engine);
			booleanInstance.Prototype = PrototypeObject;
			booleanInstance.PrimitiveValue = value;
			booleanInstance.Extensible = true;
			return booleanInstance;
		}
	}
}
