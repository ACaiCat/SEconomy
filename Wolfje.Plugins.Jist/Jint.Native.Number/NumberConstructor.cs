using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime;

namespace Jint.Native.Number
{
	public sealed class NumberConstructor : FunctionInstance, IConstructor
	{
		public NumberPrototype PrototypeObject { get; private set; }

		public NumberConstructor(Engine engine)
			: base(engine, null, null, strict: false)
		{
		}

		public static NumberConstructor CreateNumberConstructor(Engine engine)
		{
			NumberConstructor numberConstructor = new NumberConstructor(engine);
			numberConstructor.Extensible = true;
			numberConstructor.Prototype = engine.Function.PrototypeObject;
			numberConstructor.PrototypeObject = NumberPrototype.CreatePrototypeObject(engine, numberConstructor);
			numberConstructor.FastAddProperty("length", 1.0, writable: false, enumerable: false, configurable: false);
			numberConstructor.FastAddProperty("prototype", numberConstructor.PrototypeObject, writable: false, enumerable: false, configurable: false);
			return numberConstructor;
		}

		public void Configure()
		{
			FastAddProperty("MAX_VALUE", double.MaxValue, writable: false, enumerable: false, configurable: false);
			FastAddProperty("MIN_VALUE", double.Epsilon, writable: false, enumerable: false, configurable: false);
			FastAddProperty("NaN", double.NaN, writable: false, enumerable: false, configurable: false);
			FastAddProperty("NEGATIVE_INFINITY", double.NegativeInfinity, writable: false, enumerable: false, configurable: false);
			FastAddProperty("POSITIVE_INFINITY", double.PositiveInfinity, writable: false, enumerable: false, configurable: false);
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			if (arguments.Length == 0)
			{
				return 0.0;
			}
			return TypeConverter.ToNumber(arguments[0]);
		}

		public ObjectInstance Construct(JsValue[] arguments)
		{
			return Construct((arguments.Length != 0) ? TypeConverter.ToNumber(arguments[0]) : 0.0);
		}

		public NumberInstance Construct(double value)
		{
			NumberInstance numberInstance = new NumberInstance(base.Engine);
			numberInstance.Prototype = PrototypeObject;
			numberInstance.PrimitiveValue = value;
			numberInstance.Extensible = true;
			return numberInstance;
		}
	}
}
