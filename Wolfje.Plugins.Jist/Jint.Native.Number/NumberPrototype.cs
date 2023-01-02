using System;
using System.Globalization;
using System.Text;
using Jint.Native.Number.Dtoa;
using Jint.Runtime;
using Jint.Runtime.Interop;

namespace Jint.Native.Number
{
	public sealed class NumberPrototype : NumberInstance
	{
		private const double Ten21 = 1E+21;

		private NumberPrototype(Engine engine)
			: base(engine)
		{
		}

		public static NumberPrototype CreatePrototypeObject(Engine engine, NumberConstructor numberConstructor)
		{
			NumberPrototype numberPrototype = new NumberPrototype(engine);
			numberPrototype.Prototype = engine.Object.PrototypeObject;
			numberPrototype.PrimitiveValue = 0.0;
			numberPrototype.Extensible = true;
			numberPrototype.FastAddProperty("constructor", numberConstructor, writable: true, enumerable: false, configurable: true);
			return numberPrototype;
		}

		public void Configure()
		{
			FastAddProperty("toString", new ClrFunctionInstance(base.Engine, ToNumberString), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toLocaleString", new ClrFunctionInstance(base.Engine, ToLocaleString), writable: true, enumerable: false, configurable: true);
			FastAddProperty("valueOf", new ClrFunctionInstance(base.Engine, ValueOf), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toFixed", new ClrFunctionInstance(base.Engine, ToFixed, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toExponential", new ClrFunctionInstance(base.Engine, ToExponential), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toPrecision", new ClrFunctionInstance(base.Engine, ToPrecision), writable: true, enumerable: false, configurable: true);
		}

		private JsValue ToLocaleString(JsValue thisObject, JsValue[] arguments)
		{
			if (!thisObject.IsNumber() && thisObject.TryCast<NumberInstance>() == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			double num = TypeConverter.ToNumber(thisObject);
			if (double.IsNaN(num))
			{
				return "NaN";
			}
			if (num.Equals(0.0))
			{
				return "0";
			}
			if (num < 0.0)
			{
				return "-" + ToLocaleString(0.0 - num, arguments).ToString();
			}
			if (double.IsPositiveInfinity(num) || num >= double.MaxValue)
			{
				return "Infinity";
			}
			if (double.IsNegativeInfinity(num) || num <= double.MinValue)
			{
				return "-Infinity";
			}
			return num.ToString("n", base.Engine.Options._Culture);
		}

		private JsValue ValueOf(JsValue thisObj, JsValue[] arguments)
		{
			NumberInstance numberInstance = thisObj.TryCast<NumberInstance>();
			if (numberInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			return numberInstance.PrimitiveValue;
		}

		private JsValue ToFixed(JsValue thisObj, JsValue[] arguments)
		{
			int num = (int)TypeConverter.ToInteger(arguments.At(0, 0.0));
			if (num < 0 || num > 20)
			{
				throw new JavaScriptException(base.Engine.RangeError, "fractionDigits argument must be between 0 and 20");
			}
			double num2 = TypeConverter.ToNumber(thisObj);
			if (double.IsNaN(num2))
			{
				return "NaN";
			}
			if (num2 >= 1E+21)
			{
				return ToNumberString(num2);
			}
			return num2.ToString("f" + num, CultureInfo.InvariantCulture);
		}

		private JsValue ToExponential(JsValue thisObj, JsValue[] arguments)
		{
			int num = (int)TypeConverter.ToInteger(arguments.At(0, 16.0));
			if (num < 0 || num > 20)
			{
				throw new JavaScriptException(base.Engine.RangeError, "fractionDigits argument must be between 0 and 20");
			}
			double d = TypeConverter.ToNumber(thisObj);
			if (double.IsNaN(d))
			{
				return "NaN";
			}
			string text = "#." + new string('0', num) + "e+0";
			return d.ToString(text, CultureInfo.InvariantCulture);
		}

		private JsValue ToPrecision(JsValue thisObj, JsValue[] arguments)
		{
			double num = TypeConverter.ToNumber(thisObj);
			if (arguments.At(0) == Undefined.Instance)
			{
				return TypeConverter.ToString(num);
			}
			double num2 = TypeConverter.ToInteger(arguments.At(0));
			if (double.IsInfinity(num) || double.IsNaN(num))
			{
				return TypeConverter.ToString(num);
			}
			if (num2 < 1.0 || num2 > 21.0)
			{
				throw new JavaScriptException(base.Engine.RangeError, "precision must be between 1 and 21");
			}
			string text = num.ToString("e23", CultureInfo.InvariantCulture);
			int num3 = text.IndexOfAny(new char[2] { '.', 'e' });
			num3 = ((num3 == -1) ? text.Length : num3);
			num2 -= (double)num3;
			return num.ToString("f" + ((num2 < 1.0) ? 1.0 : num2), CultureInfo.InvariantCulture);
		}

		private JsValue ToNumberString(JsValue thisObject, JsValue[] arguments)
		{
			if (!thisObject.IsNumber() && thisObject.TryCast<NumberInstance>() == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			int num = ((arguments.At(0) == JsValue.Undefined) ? 10 : ((int)TypeConverter.ToInteger(arguments.At(0))));
			if (num < 2 || num > 36)
			{
				throw new JavaScriptException(base.Engine.RangeError, "radix must be between 2 and 36");
			}
			double num2 = TypeConverter.ToNumber(thisObject);
			if (double.IsNaN(num2))
			{
				return "NaN";
			}
			if (num2.Equals(0.0))
			{
				return "0";
			}
			if (double.IsPositiveInfinity(num2) || num2 >= double.MaxValue)
			{
				return "Infinity";
			}
			if (num2 < 0.0)
			{
				return "-" + ToNumberString(0.0 - num2, arguments).ToString();
			}
			if (num == 10)
			{
				return ToNumberString(num2);
			}
			long num3 = (long)num2;
			double n = num2 - (double)num3;
			string text = ToBase(num3, num);
			if (!n.Equals(0.0))
			{
				text = text + "." + ToFractionBase(n, num);
			}
			return text;
		}

		public static string ToBase(long n, int radix)
		{
			if (n == 0L)
			{
				return "0";
			}
			StringBuilder stringBuilder = new StringBuilder();
			while (n > 0)
			{
				int index = (int)(n % radix);
				n /= radix;
				stringBuilder.Insert(0, "0123456789abcdefghijklmnopqrstuvwxyz"[index].ToString());
			}
			return stringBuilder.ToString();
		}

		public static string ToFractionBase(double n, int radix)
		{
			if (n.Equals(0.0))
			{
				return "0";
			}
			StringBuilder stringBuilder = new StringBuilder();
			while (n > 0.0 && stringBuilder.Length < 50)
			{
				double num = n * (double)radix;
				int num2 = (int)num;
				n = num - (double)num2;
				stringBuilder.Append("0123456789abcdefghijklmnopqrstuvwxyz"[num2].ToString());
			}
			return stringBuilder.ToString();
		}

		public static string ToNumberString(double m)
		{
			if (double.IsNaN(m))
			{
				return "NaN";
			}
			if (m.Equals(0.0))
			{
				return "0";
			}
			if (double.IsPositiveInfinity(m) || m >= double.MaxValue)
			{
				return "Infinity";
			}
			if (m < 0.0)
			{
				return "-" + ToNumberString(0.0 - m);
			}
			string text = FastDtoa.NumberToString(m);
			if (text != null)
			{
				return text;
			}
			string text2 = null;
			string text3 = m.ToString("r");
			if (text3.IndexOf("e", StringComparison.OrdinalIgnoreCase) == -1)
			{
				text2 = text3.Replace(".", "").TrimStart('0').TrimEnd('0');
			}
			string[] array = m.ToString("0.00000000000000000e0", CultureInfo.InvariantCulture).Split('e');
			if (text2 == null)
			{
				text2 = array[0].TrimEnd('0').Replace(".", "");
			}
			int num = int.Parse(array[1]) + 1;
			int length = text2.Length;
			if (length <= num && num <= 21)
			{
				return text2 + new string('0', num - length);
			}
			if (0 < num && num <= 21)
			{
				return text2.Substring(0, num) + "." + text2.Substring(num);
			}
			if (-6 < num && num <= 0)
			{
				return "0." + new string('0', -num) + text2;
			}
			if (length == 1)
			{
				return text2 + "e" + ((num - 1 < 0) ? "-" : "+") + System.Math.Abs(num - 1);
			}
			return text2.Substring(0, 1) + "." + text2.Substring(1) + "e" + ((num - 1 < 0) ? "-" : "+") + System.Math.Abs(num - 1);
		}
	}
}
