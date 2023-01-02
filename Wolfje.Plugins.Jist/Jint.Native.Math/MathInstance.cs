using System;
using Jint.Native.Number;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Interop;

namespace Jint.Native.Math
{
	public sealed class MathInstance : ObjectInstance
	{
		private static Random _random = new Random();

		public override string Class => "Math";

		private MathInstance(Engine engine)
			: base(engine)
		{
		}

		public static MathInstance CreateMathObject(Engine engine)
		{
			MathInstance mathInstance = new MathInstance(engine);
			mathInstance.Extensible = true;
			mathInstance.Prototype = engine.Object.PrototypeObject;
			return mathInstance;
		}

		public void Configure()
		{
			FastAddProperty("abs", new ClrFunctionInstance(base.Engine, Abs), writable: true, enumerable: false, configurable: true);
			FastAddProperty("acos", new ClrFunctionInstance(base.Engine, Acos), writable: true, enumerable: false, configurable: true);
			FastAddProperty("asin", new ClrFunctionInstance(base.Engine, Asin), writable: true, enumerable: false, configurable: true);
			FastAddProperty("atan", new ClrFunctionInstance(base.Engine, Atan), writable: true, enumerable: false, configurable: true);
			FastAddProperty("atan2", new ClrFunctionInstance(base.Engine, Atan2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("ceil", new ClrFunctionInstance(base.Engine, Ceil), writable: true, enumerable: false, configurable: true);
			FastAddProperty("cos", new ClrFunctionInstance(base.Engine, Cos), writable: true, enumerable: false, configurable: true);
			FastAddProperty("exp", new ClrFunctionInstance(base.Engine, Exp), writable: true, enumerable: false, configurable: true);
			FastAddProperty("floor", new ClrFunctionInstance(base.Engine, Floor), writable: true, enumerable: false, configurable: true);
			FastAddProperty("log", new ClrFunctionInstance(base.Engine, Log), writable: true, enumerable: false, configurable: true);
			FastAddProperty("max", new ClrFunctionInstance(base.Engine, Max, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("min", new ClrFunctionInstance(base.Engine, Min, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("pow", new ClrFunctionInstance(base.Engine, Pow, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("random", new ClrFunctionInstance(base.Engine, Random), writable: true, enumerable: false, configurable: true);
			FastAddProperty("round", new ClrFunctionInstance(base.Engine, Round), writable: true, enumerable: false, configurable: true);
			FastAddProperty("sin", new ClrFunctionInstance(base.Engine, Sin), writable: true, enumerable: false, configurable: true);
			FastAddProperty("sqrt", new ClrFunctionInstance(base.Engine, Sqrt), writable: true, enumerable: false, configurable: true);
			FastAddProperty("tan", new ClrFunctionInstance(base.Engine, Tan), writable: true, enumerable: false, configurable: true);
			FastAddProperty("E", System.Math.E, writable: false, enumerable: false, configurable: false);
			FastAddProperty("LN10", System.Math.Log(10.0), writable: false, enumerable: false, configurable: false);
			FastAddProperty("LN2", System.Math.Log(2.0), writable: false, enumerable: false, configurable: false);
			FastAddProperty("LOG2E", System.Math.Log(System.Math.E, 2.0), writable: false, enumerable: false, configurable: false);
			FastAddProperty("LOG10E", System.Math.Log(System.Math.E, 10.0), writable: false, enumerable: false, configurable: false);
			FastAddProperty("PI", System.Math.PI, writable: false, enumerable: false, configurable: false);
			FastAddProperty("SQRT1_2", System.Math.Sqrt(0.5), writable: false, enumerable: false, configurable: false);
			FastAddProperty("SQRT2", System.Math.Sqrt(2.0), writable: false, enumerable: false, configurable: false);
		}

		private static JsValue Abs(JsValue thisObject, JsValue[] arguments)
		{
			double value = TypeConverter.ToNumber(arguments.At(0));
			return System.Math.Abs(value);
		}

		private static JsValue Acos(JsValue thisObject, JsValue[] arguments)
		{
			double d = TypeConverter.ToNumber(arguments.At(0));
			return System.Math.Acos(d);
		}

		private static JsValue Asin(JsValue thisObject, JsValue[] arguments)
		{
			double d = TypeConverter.ToNumber(arguments.At(0));
			return System.Math.Asin(d);
		}

		private static JsValue Atan(JsValue thisObject, JsValue[] arguments)
		{
			double d = TypeConverter.ToNumber(arguments.At(0));
			return System.Math.Atan(d);
		}

		private static JsValue Atan2(JsValue thisObject, JsValue[] arguments)
		{
			double num = TypeConverter.ToNumber(arguments.At(0));
			double num2 = TypeConverter.ToNumber(arguments.At(1));
			if (double.IsNaN(num2) || double.IsNaN(num))
			{
				return double.NaN;
			}
			if (num > 0.0 && num2.Equals(0.0))
			{
				return System.Math.PI / 2.0;
			}
			if (NumberInstance.IsPositiveZero(num))
			{
				if (num2 > 0.0)
				{
					return 0.0;
				}
				if (NumberInstance.IsPositiveZero(num2))
				{
					return 0.0;
				}
				if (NumberInstance.IsNegativeZero(num2))
				{
					return System.Math.PI;
				}
				if (num2 < 0.0)
				{
					return System.Math.PI;
				}
			}
			if (NumberInstance.IsNegativeZero(num))
			{
				if (num2 > 0.0)
				{
					return 0.0;
				}
				if (NumberInstance.IsPositiveZero(num2))
				{
					return 0.0;
				}
				if (NumberInstance.IsNegativeZero(num2))
				{
					return -System.Math.PI;
				}
				if (num2 < 0.0)
				{
					return -System.Math.PI;
				}
			}
			if (num < 0.0 && num2.Equals(0.0))
			{
				return -System.Math.PI / 2.0;
			}
			if (num > 0.0 && !double.IsInfinity(num))
			{
				if (double.IsPositiveInfinity(num2))
				{
					return 0.0;
				}
				if (double.IsNegativeInfinity(num2))
				{
					return System.Math.PI;
				}
			}
			if (num < 0.0 && !double.IsInfinity(num))
			{
				if (double.IsPositiveInfinity(num2))
				{
					return 0.0;
				}
				if (double.IsNegativeInfinity(num2))
				{
					return -System.Math.PI;
				}
			}
			if (double.IsPositiveInfinity(num) && !double.IsInfinity(num2))
			{
				return System.Math.PI / 2.0;
			}
			if (double.IsNegativeInfinity(num) && !double.IsInfinity(num2))
			{
				return -System.Math.PI / 2.0;
			}
			if (double.IsPositiveInfinity(num) && double.IsPositiveInfinity(num2))
			{
				return System.Math.PI / 4.0;
			}
			if (double.IsPositiveInfinity(num) && double.IsNegativeInfinity(num2))
			{
				return System.Math.PI * 3.0 / 4.0;
			}
			if (double.IsNegativeInfinity(num) && double.IsPositiveInfinity(num2))
			{
				return -System.Math.PI / 4.0;
			}
			if (double.IsNegativeInfinity(num) && double.IsNegativeInfinity(num2))
			{
				return System.Math.PI * -3.0 / 4.0;
			}
			return System.Math.Atan2(num, num2);
		}

		private static JsValue Ceil(JsValue thisObject, JsValue[] arguments)
		{
			double a = TypeConverter.ToNumber(arguments.At(0));
			return System.Math.Ceiling(a);
		}

		private static JsValue Cos(JsValue thisObject, JsValue[] arguments)
		{
			double d = TypeConverter.ToNumber(arguments.At(0));
			return System.Math.Cos(d);
		}

		private static JsValue Exp(JsValue thisObject, JsValue[] arguments)
		{
			double d = TypeConverter.ToNumber(arguments.At(0));
			return System.Math.Exp(d);
		}

		private static JsValue Floor(JsValue thisObject, JsValue[] arguments)
		{
			double d = TypeConverter.ToNumber(arguments.At(0));
			return System.Math.Floor(d);
		}

		private static JsValue Log(JsValue thisObject, JsValue[] arguments)
		{
			double d = TypeConverter.ToNumber(arguments.At(0));
			return System.Math.Log(d);
		}

		private static JsValue Max(JsValue thisObject, JsValue[] arguments)
		{
			if (arguments.Length == 0)
			{
				return double.NegativeInfinity;
			}
			double num = TypeConverter.ToNumber(arguments.At(0));
			for (int i = 0; i < arguments.Length; i++)
			{
				num = System.Math.Max(num, TypeConverter.ToNumber(arguments[i]));
			}
			return num;
		}

		private static JsValue Min(JsValue thisObject, JsValue[] arguments)
		{
			if (arguments.Length == 0)
			{
				return double.PositiveInfinity;
			}
			double num = TypeConverter.ToNumber(arguments.At(0));
			for (int i = 0; i < arguments.Length; i++)
			{
				num = System.Math.Min(num, TypeConverter.ToNumber(arguments[i]));
			}
			return num;
		}

		private static JsValue Pow(JsValue thisObject, JsValue[] arguments)
		{
			double num = TypeConverter.ToNumber(arguments.At(0));
			double num2 = TypeConverter.ToNumber(arguments.At(1));
			if (double.IsNaN(num2))
			{
				return double.NaN;
			}
			if (num2.Equals(0.0))
			{
				return 1.0;
			}
			if (double.IsNaN(num) && !num2.Equals(0.0))
			{
				return double.NaN;
			}
			if (System.Math.Abs(num) > 1.0)
			{
				if (double.IsPositiveInfinity(num2))
				{
					return double.PositiveInfinity;
				}
				if (double.IsNegativeInfinity(num2))
				{
					return 0.0;
				}
			}
			if (System.Math.Abs(num).Equals(1.0) && double.IsInfinity(num2))
			{
				return double.NaN;
			}
			if (System.Math.Abs(num) < 1.0)
			{
				if (double.IsPositiveInfinity(num2))
				{
					return 0.0;
				}
				if (double.IsNegativeInfinity(num2))
				{
					return double.PositiveInfinity;
				}
			}
			if (double.IsPositiveInfinity(num))
			{
				if (num2 > 0.0)
				{
					return double.PositiveInfinity;
				}
				if (num2 < 0.0)
				{
					return 0.0;
				}
			}
			if (double.IsNegativeInfinity(num))
			{
				if (num2 > 0.0)
				{
					if (System.Math.Abs(num2 % 2.0).Equals(1.0))
					{
						return double.NegativeInfinity;
					}
					return double.PositiveInfinity;
				}
				if (num2 < 0.0)
				{
					System.Math.Abs(num2 % 2.0).Equals(1.0);
					return 0.0;
				}
			}
			if (NumberInstance.IsPositiveZero(num))
			{
				if (num2 > 0.0)
				{
					return 0.0;
				}
				if (num2 < 0.0)
				{
					return double.PositiveInfinity;
				}
			}
			if (NumberInstance.IsNegativeZero(num))
			{
				if (num2 > 0.0)
				{
					System.Math.Abs(num2 % 2.0).Equals(1.0);
					return 0.0;
				}
				if (num2 < 0.0)
				{
					if (System.Math.Abs(num2 % 2.0).Equals(1.0))
					{
						return double.NegativeInfinity;
					}
					return double.PositiveInfinity;
				}
			}
			if (num < 0.0 && !double.IsInfinity(num) && !double.IsInfinity(num2) && !num2.Equals((int)num2))
			{
				return double.NaN;
			}
			return System.Math.Pow(num, num2);
		}

		private static JsValue Random(JsValue thisObject, JsValue[] arguments)
		{
			return _random.NextDouble();
		}

		private static JsValue Round(JsValue thisObject, JsValue[] arguments)
		{
			double num = TypeConverter.ToNumber(arguments.At(0));
			double num2 = System.Math.Round(num);
			if (num2.Equals(num - 0.5))
			{
				return num2 + 1.0;
			}
			return num2;
		}

		private static JsValue Sin(JsValue thisObject, JsValue[] arguments)
		{
			double a = TypeConverter.ToNumber(arguments.At(0));
			return System.Math.Sin(a);
		}

		private static JsValue Sqrt(JsValue thisObject, JsValue[] arguments)
		{
			double d = TypeConverter.ToNumber(arguments.At(0));
			return System.Math.Sqrt(d);
		}

		private static JsValue Tan(JsValue thisObject, JsValue[] arguments)
		{
			double a = TypeConverter.ToNumber(arguments.At(0));
			return System.Math.Tan(a);
		}
	}
}
