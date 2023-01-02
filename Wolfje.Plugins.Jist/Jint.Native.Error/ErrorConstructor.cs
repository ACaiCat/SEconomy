using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime;

namespace Jint.Native.Error
{
	public class ErrorConstructor : FunctionInstance, IConstructor
	{
		private string _name;

		public ErrorPrototype PrototypeObject { get; private set; }

		public ErrorConstructor(Engine engine)
			: base(engine, null, null, strict: false)
		{
		}

		public static ErrorConstructor CreateErrorConstructor(Engine engine, string name)
		{
			ErrorConstructor errorConstructor = new ErrorConstructor(engine);
			errorConstructor.Extensible = true;
			errorConstructor._name = name;
			errorConstructor.Prototype = engine.Function.PrototypeObject;
			errorConstructor.PrototypeObject = ErrorPrototype.CreatePrototypeObject(engine, errorConstructor, name);
			errorConstructor.FastAddProperty("length", 1.0, writable: false, enumerable: false, configurable: false);
			errorConstructor.FastAddProperty("prototype", errorConstructor.PrototypeObject, writable: false, enumerable: false, configurable: false);
			return errorConstructor;
		}

		public void Configure()
		{
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			return Construct(arguments);
		}

		public ObjectInstance Construct(JsValue[] arguments)
		{
			ErrorInstance errorInstance = new ErrorInstance(base.Engine, _name);
			errorInstance.Prototype = PrototypeObject;
			errorInstance.Extensible = true;
			if (arguments.At(0) != Undefined.Instance)
			{
				errorInstance.Put("message", TypeConverter.ToString(arguments.At(0)), throwOnError: false);
			}
			return errorInstance;
		}
	}
}
