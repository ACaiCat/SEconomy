using System;
using System.Globalization;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Interop;

namespace Jint.Native.Date
{
	public sealed class DatePrototype : DateInstance
	{
		public static double HoursPerDay = 24.0;

		public static double MinutesPerHour = 60.0;

		public static double SecondsPerMinute = 60.0;

		public static double MsPerSecond = 1000.0;

		public static double MsPerMinute = 60000.0;

		public static double MsPerHour = 3600000.0;

		public static double MsPerDay = 86400000.0;

		public double LocalTza => base.Engine.Options._LocalTimeZone.BaseUtcOffset.TotalMilliseconds;

		private DatePrototype(Engine engine)
			: base(engine)
		{
		}

		public static DatePrototype CreatePrototypeObject(Engine engine, DateConstructor dateConstructor)
		{
			DatePrototype datePrototype = new DatePrototype(engine)
			{
				Prototype = engine.Object.PrototypeObject,
				Extensible = true,
				PrimitiveValue = double.NaN
			};
			datePrototype.FastAddProperty("constructor", dateConstructor, writable: true, enumerable: false, configurable: true);
			return datePrototype;
		}

		public void Configure()
		{
			FastAddProperty("toString", new ClrFunctionInstance(base.Engine, ToString, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toDateString", new ClrFunctionInstance(base.Engine, ToDateString, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toTimeString", new ClrFunctionInstance(base.Engine, ToTimeString, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toLocaleString", new ClrFunctionInstance(base.Engine, ToLocaleString, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toLocaleDateString", new ClrFunctionInstance(base.Engine, ToLocaleDateString, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toLocaleTimeString", new ClrFunctionInstance(base.Engine, ToLocaleTimeString, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("valueOf", new ClrFunctionInstance(base.Engine, ValueOf, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getTime", new ClrFunctionInstance(base.Engine, GetTime, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getFullYear", new ClrFunctionInstance(base.Engine, GetFullYear, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getYear", new ClrFunctionInstance(base.Engine, GetYear, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getUTCFullYear", new ClrFunctionInstance(base.Engine, GetUTCFullYear, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getMonth", new ClrFunctionInstance(base.Engine, GetMonth, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getUTCMonth", new ClrFunctionInstance(base.Engine, GetUTCMonth, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getDate", new ClrFunctionInstance(base.Engine, GetDate, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getUTCDate", new ClrFunctionInstance(base.Engine, GetUTCDate, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getDay", new ClrFunctionInstance(base.Engine, GetDay, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getUTCDay", new ClrFunctionInstance(base.Engine, GetUTCDay, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getHours", new ClrFunctionInstance(base.Engine, GetHours, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getUTCHours", new ClrFunctionInstance(base.Engine, GetUTCHours, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getMinutes", new ClrFunctionInstance(base.Engine, GetMinutes, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getUTCMinutes", new ClrFunctionInstance(base.Engine, GetUTCMinutes, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getSeconds", new ClrFunctionInstance(base.Engine, GetSeconds, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getUTCSeconds", new ClrFunctionInstance(base.Engine, GetUTCSeconds, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getMilliseconds", new ClrFunctionInstance(base.Engine, GetMilliseconds, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getUTCMilliseconds", new ClrFunctionInstance(base.Engine, GetUTCMilliseconds, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getTimezoneOffset", new ClrFunctionInstance(base.Engine, GetTimezoneOffset, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setTime", new ClrFunctionInstance(base.Engine, SetTime, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setMilliseconds", new ClrFunctionInstance(base.Engine, SetMilliseconds, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setUTCMilliseconds", new ClrFunctionInstance(base.Engine, SetUTCMilliseconds, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setSeconds", new ClrFunctionInstance(base.Engine, SetSeconds, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setUTCSeconds", new ClrFunctionInstance(base.Engine, SetUTCSeconds, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setMinutes", new ClrFunctionInstance(base.Engine, SetMinutes, 3), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setUTCMinutes", new ClrFunctionInstance(base.Engine, SetUTCMinutes, 3), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setHours", new ClrFunctionInstance(base.Engine, SetHours, 4), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setUTCHours", new ClrFunctionInstance(base.Engine, SetUTCHours, 4), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setDate", new ClrFunctionInstance(base.Engine, SetDate, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setUTCDate", new ClrFunctionInstance(base.Engine, SetUTCDate, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setMonth", new ClrFunctionInstance(base.Engine, SetMonth, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setUTCMonth", new ClrFunctionInstance(base.Engine, SetUTCMonth, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setFullYear", new ClrFunctionInstance(base.Engine, SetFullYear, 3), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setYear", new ClrFunctionInstance(base.Engine, SetYear, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("setUTCFullYear", new ClrFunctionInstance(base.Engine, SetUTCFullYear, 3), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toUTCString", new ClrFunctionInstance(base.Engine, ToUtcString, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toISOString", new ClrFunctionInstance(base.Engine, ToISOString, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toJSON", new ClrFunctionInstance(base.Engine, ToJSON, 1), writable: true, enumerable: false, configurable: true);
		}

		private JsValue ValueOf(JsValue thisObj, JsValue[] arguments)
		{
			return EnsureDateInstance(thisObj).PrimitiveValue;
		}

		private DateInstance EnsureDateInstance(JsValue thisObj)
		{
			return thisObj.TryCast<DateInstance>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError, "Invalid Date");
			});
		}

		public JsValue ToString(JsValue thisObj, JsValue[] arg2)
		{
			return ToLocalTime(EnsureDateInstance(thisObj).ToDateTime()).ToString("ddd MMM dd yyyy HH:mm:ss 'GMT'K", CultureInfo.InvariantCulture);
		}

		private JsValue ToDateString(JsValue thisObj, JsValue[] arguments)
		{
			return ToLocalTime(EnsureDateInstance(thisObj).ToDateTime()).ToString("ddd MMM dd yyyy", CultureInfo.InvariantCulture);
		}

		private JsValue ToTimeString(JsValue thisObj, JsValue[] arguments)
		{
			return ToLocalTime(EnsureDateInstance(thisObj).ToDateTime()).ToString("HH:mm:ss 'GMT'K", CultureInfo.InvariantCulture);
		}

		private JsValue ToLocaleString(JsValue thisObj, JsValue[] arguments)
		{
			return ToLocalTime(EnsureDateInstance(thisObj).ToDateTime()).ToString("F", base.Engine.Options._Culture);
		}

		private JsValue ToLocaleDateString(JsValue thisObj, JsValue[] arguments)
		{
			return ToLocalTime(EnsureDateInstance(thisObj).ToDateTime()).ToString("D", base.Engine.Options._Culture);
		}

		private JsValue ToLocaleTimeString(JsValue thisObj, JsValue[] arguments)
		{
			return ToLocalTime(EnsureDateInstance(thisObj).ToDateTime()).ToString("T", base.Engine.Options._Culture);
		}

		private JsValue GetTime(JsValue thisObj, JsValue[] arguments)
		{
			if (double.IsNaN(EnsureDateInstance(thisObj).PrimitiveValue))
			{
				return double.NaN;
			}
			return EnsureDateInstance(thisObj).PrimitiveValue;
		}

		private JsValue GetFullYear(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return YearFromTime(LocalTime(primitiveValue));
		}

		private JsValue GetYear(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return YearFromTime(LocalTime(primitiveValue)) - 1900.0;
		}

		private JsValue GetUTCFullYear(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return YearFromTime(primitiveValue);
		}

		private JsValue GetMonth(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return MonthFromTime(LocalTime(primitiveValue));
		}

		private JsValue GetUTCMonth(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return MonthFromTime(primitiveValue);
		}

		private JsValue GetDate(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return DateFromTime(LocalTime(primitiveValue));
		}

		private JsValue GetUTCDate(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return DateFromTime(primitiveValue);
		}

		private JsValue GetDay(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return WeekDay(LocalTime(primitiveValue));
		}

		private JsValue GetUTCDay(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return WeekDay(primitiveValue);
		}

		private JsValue GetHours(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return HourFromTime(LocalTime(primitiveValue));
		}

		private JsValue GetUTCHours(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return HourFromTime(primitiveValue);
		}

		private JsValue GetMinutes(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return MinFromTime(LocalTime(primitiveValue));
		}

		private JsValue GetUTCMinutes(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return MinFromTime(primitiveValue);
		}

		private JsValue GetSeconds(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = thisObj.TryCast<DateInstance>().PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return SecFromTime(LocalTime(primitiveValue));
		}

		private JsValue GetUTCSeconds(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return SecFromTime(primitiveValue);
		}

		private JsValue GetMilliseconds(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return MsFromTime(LocalTime(primitiveValue));
		}

		private JsValue GetUTCMilliseconds(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return MsFromTime(primitiveValue);
		}

		private JsValue GetTimezoneOffset(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			if (double.IsNaN(primitiveValue))
			{
				return double.NaN;
			}
			return (primitiveValue - LocalTime(primitiveValue)) / MsPerMinute;
		}

		private JsValue SetTime(JsValue thisObj, JsValue[] arguments)
		{
			double num2 = (EnsureDateInstance(thisObj).PrimitiveValue = TimeClip(TypeConverter.ToNumber(arguments.At(0))));
			double num3 = num2;
			return num3;
		}

		private JsValue SetMilliseconds(JsValue thisObj, JsValue[] arguments)
		{
			double t = LocalTime(EnsureDateInstance(thisObj).PrimitiveValue);
			double time = MakeTime(HourFromTime(t), MinFromTime(t), SecFromTime(t), TypeConverter.ToNumber(arguments.At(0)));
			double num = TimeClip(Utc(MakeDate(Day(t), time)));
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue SetUTCMilliseconds(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			double time = MakeTime(HourFromTime(primitiveValue), MinFromTime(primitiveValue), SecFromTime(primitiveValue), TypeConverter.ToNumber(arguments.At(0)));
			double num = TimeClip(MakeDate(Day(primitiveValue), time));
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue SetSeconds(JsValue thisObj, JsValue[] arguments)
		{
			double t = LocalTime(EnsureDateInstance(thisObj).PrimitiveValue);
			double sec = TypeConverter.ToNumber(arguments.At(0));
			double ms = ((arguments.Length <= 1) ? MsFromTime(t) : TypeConverter.ToNumber(arguments.At(1)));
			double t2 = MakeDate(Day(t), MakeTime(HourFromTime(t), MinFromTime(t), sec, ms));
			double num = TimeClip(Utc(t2));
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue SetUTCSeconds(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			double sec = TypeConverter.ToNumber(arguments.At(0));
			double ms = ((arguments.Length <= 1) ? MsFromTime(primitiveValue) : TypeConverter.ToNumber(arguments.At(1)));
			double time = MakeDate(Day(primitiveValue), MakeTime(HourFromTime(primitiveValue), MinFromTime(primitiveValue), sec, ms));
			double num = TimeClip(time);
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue SetMinutes(JsValue thisObj, JsValue[] arguments)
		{
			double t = LocalTime(EnsureDateInstance(thisObj).PrimitiveValue);
			double min = TypeConverter.ToNumber(arguments.At(0));
			double sec = ((arguments.Length <= 1) ? SecFromTime(t) : TypeConverter.ToNumber(arguments.At(1)));
			double ms = ((arguments.Length <= 2) ? MsFromTime(t) : TypeConverter.ToNumber(arguments.At(2)));
			double t2 = MakeDate(Day(t), MakeTime(HourFromTime(t), min, sec, ms));
			double num = TimeClip(Utc(t2));
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue SetUTCMinutes(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			double min = TypeConverter.ToNumber(arguments.At(0));
			double sec = ((arguments.Length <= 1) ? SecFromTime(primitiveValue) : TypeConverter.ToNumber(arguments.At(1)));
			double ms = ((arguments.Length <= 2) ? MsFromTime(primitiveValue) : TypeConverter.ToNumber(arguments.At(2)));
			double time = MakeDate(Day(primitiveValue), MakeTime(HourFromTime(primitiveValue), min, sec, ms));
			double num = TimeClip(time);
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue SetHours(JsValue thisObj, JsValue[] arguments)
		{
			double t = LocalTime(EnsureDateInstance(thisObj).PrimitiveValue);
			double hour = TypeConverter.ToNumber(arguments.At(0));
			double min = ((arguments.Length <= 1) ? MinFromTime(t) : TypeConverter.ToNumber(arguments.At(1)));
			double sec = ((arguments.Length <= 2) ? SecFromTime(t) : TypeConverter.ToNumber(arguments.At(2)));
			double ms = ((arguments.Length <= 3) ? MsFromTime(t) : TypeConverter.ToNumber(arguments.At(3)));
			double t2 = MakeDate(Day(t), MakeTime(hour, min, sec, ms));
			double num = TimeClip(Utc(t2));
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue SetUTCHours(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			double hour = TypeConverter.ToNumber(arguments.At(0));
			double min = ((arguments.Length <= 1) ? MinFromTime(primitiveValue) : TypeConverter.ToNumber(arguments.At(1)));
			double sec = ((arguments.Length <= 2) ? SecFromTime(primitiveValue) : TypeConverter.ToNumber(arguments.At(2)));
			double ms = ((arguments.Length <= 3) ? MsFromTime(primitiveValue) : TypeConverter.ToNumber(arguments.At(3)));
			double time = MakeDate(Day(primitiveValue), MakeTime(hour, min, sec, ms));
			double num = TimeClip(time);
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue SetDate(JsValue thisObj, JsValue[] arguments)
		{
			double t = LocalTime(EnsureDateInstance(thisObj).PrimitiveValue);
			double date = TypeConverter.ToNumber(arguments.At(0));
			double t2 = MakeDate(MakeDay(YearFromTime(t), MonthFromTime(t), date), TimeWithinDay(t));
			double num = TimeClip(Utc(t2));
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue SetUTCDate(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			double date = TypeConverter.ToNumber(arguments.At(0));
			double time = MakeDate(MakeDay(YearFromTime(primitiveValue), MonthFromTime(primitiveValue), date), TimeWithinDay(primitiveValue));
			double num = TimeClip(time);
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue SetMonth(JsValue thisObj, JsValue[] arguments)
		{
			double t = LocalTime(EnsureDateInstance(thisObj).PrimitiveValue);
			double month = TypeConverter.ToNumber(arguments.At(0));
			double date = ((arguments.Length <= 1) ? DateFromTime(t) : TypeConverter.ToNumber(arguments.At(1)));
			double t2 = MakeDate(MakeDay(YearFromTime(t), month, date), TimeWithinDay(t));
			double num = TimeClip(Utc(t2));
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue SetUTCMonth(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			double month = TypeConverter.ToNumber(arguments.At(0));
			double date = ((arguments.Length <= 1) ? DateFromTime(primitiveValue) : TypeConverter.ToNumber(arguments.At(1)));
			double time = MakeDate(MakeDay(YearFromTime(primitiveValue), month, date), TimeWithinDay(primitiveValue));
			double num = TimeClip(time);
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue SetFullYear(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			double t = (double.IsNaN(primitiveValue) ? 0.0 : LocalTime(primitiveValue));
			double year = TypeConverter.ToNumber(arguments.At(0));
			double month = ((arguments.Length <= 1) ? MonthFromTime(t) : TypeConverter.ToNumber(arguments.At(1)));
			double date = ((arguments.Length <= 2) ? DateFromTime(t) : TypeConverter.ToNumber(arguments.At(2)));
			double t2 = MakeDate(MakeDay(year, month, date), TimeWithinDay(t));
			double num = TimeClip(Utc(t2));
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue SetYear(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			double t = (double.IsNaN(primitiveValue) ? 0.0 : LocalTime(primitiveValue));
			double num = TypeConverter.ToNumber(arguments.At(0));
			if (double.IsNaN(num))
			{
				EnsureDateInstance(thisObj).PrimitiveValue = double.NaN;
				return double.NaN;
			}
			double num2 = TypeConverter.ToInteger(num);
			if (num >= 0.0 && num <= 99.0)
			{
				num2 += 1900.0;
			}
			double day = MakeDay(num2, MonthFromTime(t), DateFromTime(t));
			double num3 = Utc(MakeDate(day, TimeWithinDay(t)));
			EnsureDateInstance(thisObj).PrimitiveValue = TimeClip(num3);
			return num3;
		}

		private JsValue SetUTCFullYear(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = EnsureDateInstance(thisObj).PrimitiveValue;
			double t = (double.IsNaN(primitiveValue) ? 0.0 : primitiveValue);
			double year = TypeConverter.ToNumber(arguments.At(0));
			double month = ((arguments.Length <= 1) ? MonthFromTime(t) : TypeConverter.ToNumber(arguments.At(1)));
			double date = ((arguments.Length <= 2) ? DateFromTime(t) : TypeConverter.ToNumber(arguments.At(2)));
			double time = MakeDate(MakeDay(year, month, date), TimeWithinDay(t));
			double num = TimeClip(time);
			thisObj.As<DateInstance>().PrimitiveValue = num;
			return num;
		}

		private JsValue ToUtcString(JsValue thisObj, JsValue[] arguments)
		{
			return thisObj.TryCast<DateInstance>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}).ToDateTime().ToUniversalTime()
				.ToString("ddd MMM dd yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture);
		}

		private JsValue ToISOString(JsValue thisObj, JsValue[] arguments)
		{
			double primitiveValue = thisObj.TryCast<DateInstance>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}).PrimitiveValue;
			if (double.IsInfinity(primitiveValue) || double.IsNaN(primitiveValue))
			{
				throw new JavaScriptException(base.Engine.RangeError);
			}
			return $"{YearFromTime(primitiveValue):0000}-{MonthFromTime(primitiveValue) + 1.0:00}-{DateFromTime(primitiveValue):00}T{HourFromTime(primitiveValue):00}:{MinFromTime(primitiveValue):00}:{SecFromTime(primitiveValue):00}.{MsFromTime(primitiveValue):000}Z";
		}

		private JsValue ToJSON(JsValue thisObj, JsValue[] arguments)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue jsValue = TypeConverter.ToPrimitive(objectInstance, Types.Number);
			if (jsValue.IsNumber() && double.IsInfinity(jsValue.AsNumber()))
			{
				return JsValue.Null;
			}
			JsValue jsValue2 = objectInstance.Get("toISOString");
			if (!jsValue2.Is<ICallable>())
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			return jsValue2.TryCast<ICallable>().Call(objectInstance, Arguments.Empty);
		}

		public static double Day(double t)
		{
			return System.Math.Floor(t / MsPerDay);
		}

		public static double TimeWithinDay(double t)
		{
			return t % MsPerDay;
		}

		public static double DaysInYear(double y)
		{
			if (!(y % 4.0).Equals(0.0))
			{
				return 365.0;
			}
			if ((y % 4.0).Equals(0.0) && !(y % 100.0).Equals(0.0))
			{
				return 366.0;
			}
			if ((y % 100.0).Equals(0.0) && !(y % 400.0).Equals(0.0))
			{
				return 365.0;
			}
			if ((y % 400.0).Equals(0.0))
			{
				return 366.0;
			}
			return 365.0;
		}

		public static double DayFromYear(double y)
		{
			return 365.0 * (y - 1970.0) + System.Math.Floor((y - 1969.0) / 4.0) - System.Math.Floor((y - 1901.0) / 100.0) + System.Math.Floor((y - 1601.0) / 400.0);
		}

		public static double TimeFromYear(double y)
		{
			return MsPerDay * DayFromYear(y);
		}

		public static double YearFromTime(double t)
		{
			if (!AreFinite(t))
			{
				return double.NaN;
			}
			double num = double.MaxValue;
			double num2 = double.MinValue;
			while (num > num2 + 1.0)
			{
				double num3 = System.Math.Floor((num + num2) / 2.0);
				double num4 = TimeFromYear(num3);
				if (num4 <= t)
				{
					num2 = num3;
				}
				else
				{
					num = num3;
				}
			}
			return num2;
		}

		public static double InLeapYear(double t)
		{
			double num = DaysInYear(YearFromTime(t));
			if (num.Equals(365.0))
			{
				return 0.0;
			}
			if (num.Equals(366.0))
			{
				return 1.0;
			}
			throw new ArgumentException();
		}

		public static double MonthFromTime(double t)
		{
			double num = DayWithinYear(t);
			double num2 = InLeapYear(t);
			if (num < 31.0)
			{
				return 0.0;
			}
			if (num < 59.0 + num2)
			{
				return 1.0;
			}
			if (num < 90.0 + num2)
			{
				return 2.0;
			}
			if (num < 120.0 + num2)
			{
				return 3.0;
			}
			if (num < 151.0 + num2)
			{
				return 4.0;
			}
			if (num < 181.0 + num2)
			{
				return 5.0;
			}
			if (num < 212.0 + num2)
			{
				return 6.0;
			}
			if (num < 243.0 + num2)
			{
				return 7.0;
			}
			if (num < 273.0 + num2)
			{
				return 8.0;
			}
			if (num < 304.0 + num2)
			{
				return 9.0;
			}
			if (num < 334.0 + num2)
			{
				return 10.0;
			}
			if (num < 365.0 + num2)
			{
				return 11.0;
			}
			throw new InvalidOperationException();
		}

		public static double DayWithinYear(double t)
		{
			return Day(t) - DayFromYear(YearFromTime(t));
		}

		public static double DateFromTime(double t)
		{
			double num = MonthFromTime(t);
			double num2 = DayWithinYear(t);
			if (num.Equals(0.0))
			{
				return num2 + 1.0;
			}
			if (num.Equals(1.0))
			{
				return num2 - 30.0;
			}
			if (num.Equals(2.0))
			{
				return num2 - 58.0 - InLeapYear(t);
			}
			if (num.Equals(3.0))
			{
				return num2 - 89.0 - InLeapYear(t);
			}
			if (num.Equals(4.0))
			{
				return num2 - 119.0 - InLeapYear(t);
			}
			if (num.Equals(5.0))
			{
				return num2 - 150.0 - InLeapYear(t);
			}
			if (num.Equals(6.0))
			{
				return num2 - 180.0 - InLeapYear(t);
			}
			if (num.Equals(7.0))
			{
				return num2 - 211.0 - InLeapYear(t);
			}
			if (num.Equals(8.0))
			{
				return num2 - 242.0 - InLeapYear(t);
			}
			if (num.Equals(9.0))
			{
				return num2 - 272.0 - InLeapYear(t);
			}
			if (num.Equals(10.0))
			{
				return num2 - 303.0 - InLeapYear(t);
			}
			if (num.Equals(11.0))
			{
				return num2 - 333.0 - InLeapYear(t);
			}
			throw new InvalidOperationException();
		}

		public static double WeekDay(double t)
		{
			return (Day(t) + 4.0) % 7.0;
		}

		public double DaylightSavingTa(double t)
		{
			double num = t - TimeFromYear(YearFromTime(t));
			if (double.IsInfinity(num) || double.IsNaN(num))
			{
				return 0.0;
			}
			double num2 = YearFromTime(t);
			if (!(num2 < 9999.0) || !(num2 > -9999.0))
			{
				num2 = (InLeapYear(t).Equals(1.0) ? 2000 : 1999);
			}
			DateTime dateTime = new DateTime((int)num2, 1, 1).AddMilliseconds(num);
			if (!base.Engine.Options._LocalTimeZone.IsDaylightSavingTime(dateTime))
			{
				return 0.0;
			}
			return MsPerHour;
		}

		public DateTimeOffset ToLocalTime(DateTime t)
		{
			return t.Kind switch
			{
				DateTimeKind.Local => new DateTimeOffset(TimeZoneInfo.ConvertTime(t.ToUniversalTime(), base.Engine.Options._LocalTimeZone), base.Engine.Options._LocalTimeZone.GetUtcOffset(t)), 
				DateTimeKind.Utc => new DateTimeOffset(TimeZoneInfo.ConvertTime(t, base.Engine.Options._LocalTimeZone), base.Engine.Options._LocalTimeZone.GetUtcOffset(t)), 
				_ => t, 
			};
		}

		public double LocalTime(double t)
		{
			return t + LocalTza + DaylightSavingTa(t);
		}

		public double Utc(double t)
		{
			return t - LocalTza - DaylightSavingTa(t - LocalTza);
		}

		public static double HourFromTime(double t)
		{
			return System.Math.Floor(t / MsPerHour) % HoursPerDay;
		}

		public static double MinFromTime(double t)
		{
			return System.Math.Floor(t / MsPerMinute) % MinutesPerHour;
		}

		public static double SecFromTime(double t)
		{
			return System.Math.Floor(t / MsPerSecond) % SecondsPerMinute;
		}

		public static double MsFromTime(double t)
		{
			return t % MsPerSecond;
		}

		public static double DayFromMonth(double year, double month)
		{
			double num = month * 30.0;
			num = ((month >= 7.0) ? (num + (month / 2.0 - 1.0)) : ((!(month >= 2.0)) ? (num + month) : (num + ((month - 1.0) / 2.0 - 1.0))));
			if (month >= 2.0 && InLeapYear(year).Equals(1.0))
			{
				num += 1.0;
			}
			return num;
		}

		public static double DaysInMonth(double month, double leap)
		{
			month %= 12.0;
			long num = (long)month;
			if ((ulong)num <= 11uL)
			{
				if ((ulong)num <= 11uL)
				{
					switch (num)
					{
					case 0L:
					case 2L:
					case 4L:
					case 6L:
					case 7L:
					case 9L:
					case 11L:
						return 31.0;
					case 3L:
					case 5L:
					case 8L:
					case 10L:
						return 30.0;
					case 1L:
						return 28.0 + leap;
					}
				}
			}
			throw new ArgumentOutOfRangeException("month");
		}

		public static double MakeTime(double hour, double min, double sec, double ms)
		{
			if (!AreFinite(hour, min, sec, ms))
			{
				return double.NaN;
			}
			long num = (long)hour;
			long num2 = (long)min;
			long num3 = (long)sec;
			long num4 = (long)ms;
			return (double)num * MsPerHour + (double)num2 * MsPerMinute + (double)num3 * MsPerSecond + (double)num4;
		}

		public static double MakeDay(double year, double month, double date)
		{
			if (!AreFinite(year, month, date))
			{
				return double.NaN;
			}
			year = TypeConverter.ToInteger(year);
			month = TypeConverter.ToInteger(month);
			date = TypeConverter.ToInteger(date);
			int num = ((!(year < 1970.0)) ? 1 : (-1));
			double num2 = ((year < 1970.0) ? 1 : 0);
			if (num == -1)
			{
				for (int i = 1969; (double)i >= year; i += num)
				{
					num2 += (double)num * DaysInYear(i) * MsPerDay;
				}
			}
			else
			{
				for (int j = 1970; (double)j < year; j += num)
				{
					num2 += (double)num * DaysInYear(j) * MsPerDay;
				}
			}
			for (int k = 0; (double)k < month; k++)
			{
				num2 += DaysInMonth(k, InLeapYear(num2)) * MsPerDay;
			}
			return Day(num2) + date - 1.0;
		}

		public static double MakeDate(double day, double time)
		{
			if (!AreFinite(day, time))
			{
				return double.NaN;
			}
			return day * MsPerDay + time;
		}

		public static double TimeClip(double time)
		{
			if (!AreFinite(time))
			{
				return double.NaN;
			}
			if (System.Math.Abs(time) > 8.64E+15)
			{
				return double.NaN;
			}
			return (long)time;
		}

		private static bool AreFinite(params double[] values)
		{
			foreach (double d in values)
			{
				if (double.IsNaN(d) || double.IsInfinity(d))
				{
					return false;
				}
			}
			return true;
		}
	}
}
