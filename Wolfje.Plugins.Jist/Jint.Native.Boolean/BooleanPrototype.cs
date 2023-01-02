using Jint.Runtime;
using Jint.Runtime.Interop;

namespace Jint.Native.Boolean
{
	public sealed class BooleanPrototype : BooleanInstance
	{
		private BooleanPrototype(Engine engine)
			: base(engine)
		{
		}

		public static BooleanPrototype CreatePrototypeObject(Engine engine, BooleanConstructor booleanConstructor)
		{
			BooleanPrototype booleanPrototype = new BooleanPrototype(engine);
			booleanPrototype.Prototype = engine.Object.PrototypeObject;
			booleanPrototype.PrimitiveValue = false;
			booleanPrototype.Extensible = true;
			booleanPrototype.FastAddProperty("constructor", booleanConstructor, writable: true, enumerable: false, configurable: true);
			return booleanPrototype;
		}

		public void Configure()
		{
			FastAddProperty("toString", new ClrFunctionInstance(base.Engine, ToBooleanString), writable: true, enumerable: false, configurable: true);
			FastAddProperty("valueOf", new ClrFunctionInstance(base.Engine, ValueOf), writable: true, enumerable: false, configurable: true);
		}

		private JsValue ValueOf(JsValue thisObj, JsValue[] arguments)
		{
			JsValue result = thisObj;
			if (result.IsBoolean())
			{
				return result;
			}
			BooleanInstance booleanInstance = result.TryCast<BooleanInstance>();
			if (booleanInstance != null)
			{
				return booleanInstance.PrimitiveValue;
			}
			throw new JavaScriptException(base.Engine.TypeError);
		}

		private JsValue ToBooleanString(JsValue thisObj, JsValue[] arguments)
		{
			return ValueOf(thisObj, Arguments.Empty).AsBoolean() ? "true" : "false";
		}
	}
}
