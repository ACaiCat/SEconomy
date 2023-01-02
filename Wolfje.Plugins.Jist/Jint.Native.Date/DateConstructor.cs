using System;
using System.Globalization;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Interop;

namespace Jint.Native.Date
{
	public sealed class DateConstructor : FunctionInstance, IConstructor
	{
		internal static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public DatePrototype PrototypeObject { get; private set; }

		public DateConstructor(Engine engine)
			: base(engine, null, null, strict: false)
		{
		}

		public static DateConstructor CreateDateConstructor(Engine engine)
		{
			DateConstructor dateConstructor = new DateConstructor(engine);
			dateConstructor.Extensible = true;
			dateConstructor.Prototype = engine.Function.PrototypeObject;
			dateConstructor.PrototypeObject = DatePrototype.CreatePrototypeObject(engine, dateConstructor);
			dateConstructor.FastAddProperty("length", 7.0, writable: false, enumerable: false, configurable: false);
			dateConstructor.FastAddProperty("prototype", dateConstructor.PrototypeObject, writable: false, enumerable: false, configurable: false);
			return dateConstructor;
		}

		public void Configure()
		{
			FastAddProperty("parse", new ClrFunctionInstance(base.Engine, Parse, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("UTC", new ClrFunctionInstance(base.Engine, Utc, 7), writable: true, enumerable: false, configurable: true);
			FastAddProperty("now", new ClrFunctionInstance(base.Engine, Now, 0), writable: true, enumerable: false, configurable: true);
		}

		private JsValue Parse(JsValue thisObj, JsValue[] arguments)
		{
			string s = TypeConverter.ToString(arguments.At(0));
			if (!DateTime.TryParseExact(s, new string[6] { "yyyy-MM-ddTHH:mm:ss.FFF", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm", "yyyy-MM-dd", "yyyy-MM", "yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var result) && !DateTime.TryParseExact(s, new string[24]
			{
				"ddd MMM dd yyyy HH:mm:ss 'GMT'K", "ddd MMM dd yyyy", "HH:mm:ss 'GMT'K", "yyyy-M-dTH:m:s.FFFK", "yyyy/M/dTH:m:s.FFFK", "yyyy-M-dTH:m:sK", "yyyy/M/dTH:m:sK", "yyyy-M-dTH:mK", "yyyy/M/dTH:mK", "yyyy-M-d H:m:s.FFFK",
				"yyyy/M/d H:m:s.FFFK", "yyyy-M-d H:m:sK", "yyyy/M/d H:m:sK", "yyyy-M-d H:mK", "yyyy/M/d H:mK", "yyyy-M-dK", "yyyy/M/dK", "yyyy-MK", "yyyy/MK", "yyyyK",
				"THH:mm:ss.FFFK", "THH:mm:ssK", "THH:mmK", "THHK"
			}, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out result) && !DateTime.TryParse(s, base.Engine.Options._Culture, DateTimeStyles.AdjustToUniversal, out result) && !DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out result))
			{
				return double.NaN;
			}
			return FromDateTime(result);
		}

		private JsValue Utc(JsValue thisObj, JsValue[] arguments)
		{
			return TimeClip(ConstructTimeValue(arguments, useUtc: true));
		}

		private JsValue Now(JsValue thisObj, JsValue[] arguments)
		{
			return System.Math.Floor((DateTime.UtcNow - Epoch).TotalMilliseconds);
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			return PrototypeObject.ToString(Construct(Arguments.Empty), Arguments.Empty);
		}

		public ObjectInstance Construct(JsValue[] arguments)
		{
			if (arguments.Length == 0)
			{
				return Construct(DateTime.UtcNow);
			}
			if (arguments.Length == 1)
			{
				JsValue jsValue = TypeConverter.ToPrimitive(arguments[0]);
				if (jsValue.IsString())
				{
					return Construct(Parse(Undefined.Instance, Arguments.From(jsValue)).AsNumber());
				}
				return Construct(TypeConverter.ToNumber(jsValue));
			}
			return Construct(ConstructTimeValue(arguments, useUtc: false));
		}

		private double ConstructTimeValue(JsValue[] arguments, bool useUtc)
		{
			if (arguments.Length < 2)
			{
				throw new ArgumentOutOfRangeException("arguments", "There must be at least two arguments.");
			}
			double num = TypeConverter.ToNumber(arguments[0]);
			int num2 = (int)TypeConverter.ToInteger(arguments[1]);
			int num3 = ((arguments.Length <= 2) ? 1 : ((int)TypeConverter.ToInteger(arguments[2])));
			int num4 = ((arguments.Length > 3) ? ((int)TypeConverter.ToInteger(arguments[3])) : 0);
			int num5 = ((arguments.Length > 4) ? ((int)TypeConverter.ToInteger(arguments[4])) : 0);
			int num6 = ((arguments.Length > 5) ? ((int)TypeConverter.ToInteger(arguments[5])) : 0);
			int num7 = ((arguments.Length > 6) ? ((int)TypeConverter.ToInteger(arguments[6])) : 0);
			for (int i = 2; i < arguments.Length; i++)
			{
				if (double.IsNaN(TypeConverter.ToNumber(arguments[i])))
				{
					return double.NaN;
				}
			}
			if (!double.IsNaN(num) && 0.0 <= TypeConverter.ToInteger(num) && TypeConverter.ToInteger(num) <= 99.0)
			{
				num += 1900.0;
			}
			double num8 = DatePrototype.MakeDate(DatePrototype.MakeDay(num, num2, num3), DatePrototype.MakeTime(num4, num5, num6, num7));
			if (!useUtc)
			{
				return PrototypeObject.Utc(num8);
			}
			return num8;
		}

		public DateInstance Construct(DateTimeOffset value)
		{
			return Construct(value.UtcDateTime);
		}

		public DateInstance Construct(DateTime value)
		{
			return new DateInstance(base.Engine)
			{
				Prototype = PrototypeObject,
				PrimitiveValue = FromDateTime(value),
				Extensible = true
			};
		}

		public DateInstance Construct(double time)
		{
			return new DateInstance(base.Engine)
			{
				Prototype = PrototypeObject,
				PrimitiveValue = TimeClip(time),
				Extensible = true
			};
		}

		public static double TimeClip(double time)
		{
			if (double.IsInfinity(time) || double.IsNaN(time))
			{
				return double.NaN;
			}
			if (System.Math.Abs(time) > 8.64E+15)
			{
				return double.NaN;
			}
			return TypeConverter.ToInteger(time);
		}

		public double FromDateTime(DateTime dt)
		{
			bool flag = dt.Kind == DateTimeKind.Unspecified;
			DateTime dateTime = ((dt.Kind == DateTimeKind.Local) ? dt.ToUniversalTime() : DateTime.SpecifyKind(dt, DateTimeKind.Utc));
			double num = (dateTime - Epoch).TotalMilliseconds;
			if (flag)
			{
				num = PrototypeObject.Utc(num);
			}
			return System.Math.Floor(num);
		}
	}
}
