using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Jint.Native;
using Jint.Native.Number;
using Jint.Native.Object;
using Jint.Native.String;

namespace Jint.Runtime
{
	public class TypeConverter
	{
		public static JsValue ToPrimitive(JsValue input, Types preferredType = Types.None)
		{
			if (input == Null.Instance || input == Undefined.Instance)
			{
				return input;
			}
			if (input.IsPrimitive())
			{
				return input;
			}
			return input.AsObject().DefaultValue(preferredType);
		}

		public static bool ToBoolean(JsValue o)
		{
			if (o.IsObject())
			{
				return true;
			}
			if (o == Undefined.Instance || o == Null.Instance)
			{
				return false;
			}
			if (o.IsBoolean())
			{
				return o.AsBoolean();
			}
			if (o.IsNumber())
			{
				double d = o.AsNumber();
				if (d.Equals(0.0) || double.IsNaN(d))
				{
					return false;
				}
				return true;
			}
			if (o.IsString())
			{
				string value = o.AsString();
				if (string.IsNullOrEmpty(value))
				{
					return false;
				}
				return true;
			}
			return true;
		}

		public static double ToNumber(JsValue o)
		{
			if (o.IsNumber())
			{
				return o.AsNumber();
			}
			if (o.IsObject() && o.AsObject() is IPrimitiveInstance primitiveInstance)
			{
				o = primitiveInstance.PrimitiveValue;
			}
			if (o == Undefined.Instance)
			{
				return double.NaN;
			}
			if (o == Null.Instance)
			{
				return 0.0;
			}
			if (o.IsBoolean())
			{
				return o.AsBoolean() ? 1 : 0;
			}
			if (o.IsString())
			{
				string text = StringPrototype.TrimEx(o.AsString());
				if (string.IsNullOrEmpty(text))
				{
					return 0.0;
				}
				if ("+Infinity".Equals(text) || "Infinity".Equals(text))
				{
					return double.PositiveInfinity;
				}
				if ("-Infinity".Equals(text))
				{
					return double.NegativeInfinity;
				}
				try
				{
					if (!text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
					{
						char c = text[0];
						if (c != '+' && c != '-' && c != '.' && !char.IsDigit(c))
						{
							return double.NaN;
						}
						double result = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
						if (text.StartsWith("-") && result.Equals(0.0))
						{
							return -0.0;
						}
						return result;
					}
					int num = int.Parse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					return num;
				}
				catch (OverflowException)
				{
					return text.StartsWith("-") ? double.NegativeInfinity : double.PositiveInfinity;
				}
				catch
				{
					return double.NaN;
				}
			}
			return ToNumber(ToPrimitive(o, Types.Number));
		}

		public static double ToInteger(JsValue o)
		{
			double num = ToNumber(o);
			if (double.IsNaN(num))
			{
				return 0.0;
			}
			if (num.Equals(0.0) || double.IsInfinity(num))
			{
				return num;
			}
			return (long)num;
		}

		public static int ToInt32(JsValue o)
		{
			return (int)(uint)ToNumber(o);
		}

		public static uint ToUint32(JsValue o)
		{
			return (uint)ToNumber(o);
		}

		public static ushort ToUint16(JsValue o)
		{
			return (ushort)(uint)ToNumber(o);
		}

		public static string ToString(JsValue o)
		{
			if (o.IsObject() && o.AsObject() is IPrimitiveInstance primitiveInstance)
			{
				o = primitiveInstance.PrimitiveValue;
			}
			if (o.IsString())
			{
				return o.AsString();
			}
			if (o == Undefined.Instance)
			{
				return Undefined.Text;
			}
			if (o == Null.Instance)
			{
				return Null.Text;
			}
			if (o.IsBoolean())
			{
				if (!o.AsBoolean())
				{
					return "false";
				}
				return "true";
			}
			if (o.IsNumber())
			{
				return NumberPrototype.ToNumberString(o.AsNumber());
			}
			return ToString(ToPrimitive(o, Types.String));
		}

		public static ObjectInstance ToObject(Engine engine, JsValue value)
		{
			if (value.IsObject())
			{
				return value.AsObject();
			}
			if (value == Undefined.Instance)
			{
				throw new JavaScriptException(engine.TypeError);
			}
			if (value == Null.Instance)
			{
				throw new JavaScriptException(engine.TypeError);
			}
			if (value.IsBoolean())
			{
				return engine.Boolean.Construct(value.AsBoolean());
			}
			if (value.IsNumber())
			{
				return engine.Number.Construct(value.AsNumber());
			}
			if (value.IsString())
			{
				return engine.String.Construct(value.AsString());
			}
			throw new JavaScriptException(engine.TypeError);
		}

		public static Types GetPrimitiveType(JsValue value)
		{
			if (value.IsObject())
			{
				return value.TryCast<IPrimitiveInstance>()?.Type ?? Types.Object;
			}
			return value.Type;
		}

		public static void CheckObjectCoercible(Engine engine, JsValue o)
		{
			if (o == Undefined.Instance || o == Null.Instance)
			{
				throw new JavaScriptException(engine.TypeError);
			}
		}

		public static IEnumerable<MethodBase> FindBestMatch(Engine engine, MethodBase[] methods, JsValue[] arguments)
		{
			methods = methods.Where((MethodBase m) => m.GetParameters().Count() == arguments.Length).ToArray();
			if (methods.Length == 1 && !methods[0].GetParameters().Any())
			{
				yield return methods[0];
				yield break;
			}
			object[] array3 = arguments.Select((JsValue x) => x.ToObject()).ToArray();
			MethodBase[] array4 = methods;
			foreach (MethodBase methodBase in array4)
			{
				bool flag = true;
				ParameterInfo[] parameters = methodBase.GetParameters();
				for (int k = 0; k < arguments.Length; k++)
				{
					object obj = array3[k];
					Type parameterType = parameters[k].ParameterType;
					if (obj == null)
					{
						if (!TypeIsNullable(parameterType))
						{
							flag = false;
							break;
						}
					}
					else if (obj.GetType() != parameterType)
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					yield return methodBase;
					yield break;
				}
			}
			MethodBase[] array2 = methods;
			for (int i = 0; i < array2.Length; i++)
			{
				yield return array2[i];
			}
		}

		public static bool TypeIsNullable(Type type)
		{
			if (type.IsValueType)
			{
				return Nullable.GetUnderlyingType(type) != null;
			}
			return true;
		}
	}
}
