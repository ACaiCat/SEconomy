using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Interop;

namespace Jint.Native.Error
{
	public sealed class ErrorPrototype : ErrorInstance
	{
		private ErrorPrototype(Engine engine, string name)
			: base(engine, name)
		{
		}

		public static ErrorPrototype CreatePrototypeObject(Engine engine, ErrorConstructor errorConstructor, string name)
		{
			ErrorPrototype errorPrototype = new ErrorPrototype(engine, name)
			{
				Extensible = true
			};
			errorPrototype.FastAddProperty("constructor", errorConstructor, writable: true, enumerable: false, configurable: true);
			errorPrototype.FastAddProperty("message", "", writable: true, enumerable: false, configurable: true);
			if (name != "Error")
			{
				errorPrototype.Prototype = engine.Error.PrototypeObject;
			}
			else
			{
				errorPrototype.Prototype = engine.Object.PrototypeObject;
			}
			return errorPrototype;
		}

		public void Configure()
		{
			FastAddProperty("toString", new ClrFunctionInstance(base.Engine, ToString), writable: true, enumerable: false, configurable: true);
		}

		public JsValue ToString(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = thisObject.TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			string text = TypeConverter.ToString(objectInstance.Get("name"));
			JsValue jsValue = objectInstance.Get("message");
			string text2 = ((!(jsValue == Undefined.Instance)) ? TypeConverter.ToString(jsValue) : "");
			if (text == "")
			{
				return text2;
			}
			if (text2 == "")
			{
				return text;
			}
			return text + ": " + text2;
		}
	}
}
