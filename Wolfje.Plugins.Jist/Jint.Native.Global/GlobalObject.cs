using System;
using System.Linq;
using System.Text;
using Jint.Native.Object;
using Jint.Native.String;
using Jint.Runtime;
using Jint.Runtime.Interop;

namespace Jint.Native.Global
{
	public sealed class GlobalObject : ObjectInstance
	{
		private static readonly char[] UriReserved = new char[10] { ';', '/', '?', ':', '@', '&', '=', '+', '$', ',' };

		private static readonly char[] UriUnescaped = new char[71]
		{
			'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
			'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
			'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D',
			'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N',
			'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
			'Y', 'Z', '0', '1', '2', '3', '4', '5', '6', '7',
			'8', '9', '-', '_', '.', '!', '~', '*', '\'', '(',
			')'
		};

		private const string HexaMap = "0123456789ABCDEF";

		private GlobalObject(Engine engine)
			: base(engine)
		{
		}

		public static GlobalObject CreateGlobalObject(Engine engine)
		{
			return new GlobalObject(engine)
			{
				Prototype = null,
				Extensible = true
			};
		}

		public void Configure()
		{
			base.Prototype = base.Engine.Object.PrototypeObject;
			FastAddProperty("Object", base.Engine.Object, writable: true, enumerable: false, configurable: true);
			FastAddProperty("Function", base.Engine.Function, writable: true, enumerable: false, configurable: true);
			FastAddProperty("Array", base.Engine.Array, writable: true, enumerable: false, configurable: true);
			FastAddProperty("String", base.Engine.String, writable: true, enumerable: false, configurable: true);
			FastAddProperty("RegExp", base.Engine.RegExp, writable: true, enumerable: false, configurable: true);
			FastAddProperty("Number", base.Engine.Number, writable: true, enumerable: false, configurable: true);
			FastAddProperty("Boolean", base.Engine.Boolean, writable: true, enumerable: false, configurable: true);
			FastAddProperty("Date", base.Engine.Date, writable: true, enumerable: false, configurable: true);
			FastAddProperty("Math", base.Engine.Math, writable: true, enumerable: false, configurable: true);
			FastAddProperty("JSON", base.Engine.Json, writable: true, enumerable: false, configurable: true);
			FastAddProperty("Error", base.Engine.Error, writable: true, enumerable: false, configurable: true);
			FastAddProperty("EvalError", base.Engine.EvalError, writable: true, enumerable: false, configurable: true);
			FastAddProperty("RangeError", base.Engine.RangeError, writable: true, enumerable: false, configurable: true);
			FastAddProperty("ReferenceError", base.Engine.ReferenceError, writable: true, enumerable: false, configurable: true);
			FastAddProperty("SyntaxError", base.Engine.SyntaxError, writable: true, enumerable: false, configurable: true);
			FastAddProperty("TypeError", base.Engine.TypeError, writable: true, enumerable: false, configurable: true);
			FastAddProperty("URIError", base.Engine.UriError, writable: true, enumerable: false, configurable: true);
			FastAddProperty("NaN", double.NaN, writable: false, enumerable: false, configurable: false);
			FastAddProperty("Infinity", double.PositiveInfinity, writable: false, enumerable: false, configurable: false);
			FastAddProperty("undefined", Undefined.Instance, writable: false, enumerable: false, configurable: false);
			FastAddProperty("parseInt", new ClrFunctionInstance(base.Engine, ParseInt, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("parseFloat", new ClrFunctionInstance(base.Engine, ParseFloat, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("isNaN", new ClrFunctionInstance(base.Engine, IsNaN, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("isFinite", new ClrFunctionInstance(base.Engine, IsFinite, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("decodeURI", new ClrFunctionInstance(base.Engine, DecodeUri, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("decodeURIComponent", new ClrFunctionInstance(base.Engine, DecodeUriComponent, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("encodeURI", new ClrFunctionInstance(base.Engine, EncodeUri, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("encodeURIComponent", new ClrFunctionInstance(base.Engine, EncodeUriComponent, 1), writable: true, enumerable: false, configurable: true);
		}

		public static JsValue ParseInt(JsValue thisObject, JsValue[] arguments)
		{
			string s = TypeConverter.ToString(arguments.At(0));
			string text = StringPrototype.TrimEx(s);
			int num = 1;
			if (!string.IsNullOrEmpty(text))
			{
				if (text[0] == '-')
				{
					num = -1;
				}
				if (text[0] == '-' || text[0] == '+')
				{
					text = text.Substring(1);
				}
			}
			bool flag = true;
			int num2 = ((arguments.Length > 1) ? TypeConverter.ToInt32(arguments[1]) : 0);
			switch (num2)
			{
			case 0:
				num2 = (((text.Length < 2 || !text.StartsWith("0x")) && !text.StartsWith("0X")) ? 10 : 16);
				break;
			default:
				return double.NaN;
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 8:
			case 9:
			case 10:
			case 11:
			case 12:
			case 13:
			case 14:
			case 15:
			case 17:
			case 18:
			case 19:
			case 20:
			case 21:
			case 22:
			case 23:
			case 24:
			case 25:
			case 26:
			case 27:
			case 28:
			case 29:
			case 30:
			case 31:
			case 32:
			case 33:
			case 34:
			case 35:
			case 36:
				flag = false;
				break;
			case 16:
				break;
			}
			if ((flag && text.Length >= 2 && text.StartsWith("0x")) || text.StartsWith("0X"))
			{
				text = text.Substring(2);
			}
			try
			{
				return (double)num * Parse(text, num2).AsNumber();
			}
			catch
			{
				return double.NaN;
			}
		}

		private static JsValue Parse(string number, int radix)
		{
			if (number == "")
			{
				return double.NaN;
			}
			double num = 0.0;
			double num2 = 1.0;
			for (int num3 = number.Length - 1; num3 >= 0; num3--)
			{
				double num4 = double.NaN;
				char c = number[num3];
				if (c >= '0' && c <= '9')
				{
					num4 = c - 48;
				}
				else if (c >= 'a' && c <= 'z')
				{
					num4 = c - 97 + 10;
				}
				else if (c >= 'A' && c <= 'Z')
				{
					num4 = c - 65 + 10;
				}
				if (double.IsNaN(num4) || num4 >= (double)radix)
				{
					return Parse(number.Substring(0, num3), radix);
				}
				num += num4 * num2;
				num2 *= (double)radix;
			}
			return num;
		}

		public static JsValue ParseFloat(JsValue thisObject, JsValue[] arguments)
		{
			string s = TypeConverter.ToString(arguments.At(0));
			string text = StringPrototype.TrimStartEx(s);
			int num = 1;
			if (text.Length > 0)
			{
				if (text[0] == '-')
				{
					num = -1;
					text = text.Substring(1);
				}
				else if (text[0] == '+')
				{
					text = text.Substring(1);
				}
			}
			if (text.StartsWith("Infinity"))
			{
				return (double)num * double.PositiveInfinity;
			}
			if (text.StartsWith("NaN"))
			{
				return double.NaN;
			}
			char c = '\0';
			bool flag = true;
			decimal num2 = default(decimal);
			int i;
			int num3;
			for (i = 0; i < text.Length; flag = false, num2 = num2 * 10m + (decimal)num3, i++)
			{
				char c2 = text[i];
				switch (c2)
				{
				case '.':
					i++;
					c = '.';
					break;
				case 'E':
				case 'e':
					i++;
					c = 'e';
					break;
				default:
					num3 = c2 - 48;
					if (num3 >= 0 && num3 <= 9)
					{
						continue;
					}
					break;
				}
				break;
			}
			decimal num4 = 0.1m;
			if (c == '.')
			{
				for (; i < text.Length; i++)
				{
					char c3 = text[i];
					int num5 = c3 - 48;
					if (num5 >= 0 && num5 <= 9)
					{
						flag = false;
						num2 += (decimal)num5 * num4;
						num4 *= 0.1m;
						continue;
					}
					if (c3 == 'e' || c3 == 'E')
					{
						i++;
						c = 'e';
					}
					break;
				}
			}
			int num6 = 0;
			int num7 = 1;
			if (c == 'e')
			{
				if (i < text.Length)
				{
					if (text[i] == '-')
					{
						num7 = -1;
						i++;
					}
					else if (text[i] == '+')
					{
						i++;
					}
				}
				for (; i < text.Length; i++)
				{
					char c4 = text[i];
					int num8 = c4 - 48;
					if (num8 < 0 || num8 > 9)
					{
						break;
					}
					num6 = num6 * 10 + num8;
				}
			}
			if (flag)
			{
				return double.NaN;
			}
			for (int j = 1; j <= num6; j++)
			{
				if (num7 > 0)
				{
					num2 *= 10m;
				}
				else
				{
					num2 /= 10m;
				}
			}
			return (double)((decimal)num * num2);
		}

		public static JsValue IsNaN(JsValue thisObject, JsValue[] arguments)
		{
			double d = TypeConverter.ToNumber(arguments.At(0));
			return double.IsNaN(d);
		}

		public static JsValue IsFinite(JsValue thisObject, JsValue[] arguments)
		{
			if (arguments.Length != 1)
			{
				return false;
			}
			double d = TypeConverter.ToNumber(arguments.At(0));
			if (double.IsNaN(d) || double.IsInfinity(d))
			{
				return false;
			}
			return true;
		}

		private static bool IsValidHexaChar(char c)
		{
			if ((c < '0' || c > '9') && (c < 'a' || c > 'f'))
			{
				if (c >= 'A')
				{
					return c <= 'F';
				}
				return false;
			}
			return true;
		}

		public JsValue EncodeUri(JsValue thisObject, JsValue[] arguments)
		{
			string uriString = TypeConverter.ToString(arguments.At(0));
			char[] unescapedUriSet = UriReserved.Concat(UriUnescaped).Concat(new char[1] { '#' }).ToArray();
			return Encode(uriString, unescapedUriSet);
		}

		public JsValue EncodeUriComponent(JsValue thisObject, JsValue[] arguments)
		{
			string uriString = TypeConverter.ToString(arguments.At(0));
			return Encode(uriString, UriUnescaped);
		}

		private string Encode(string uriString, char[] unescapedUriSet)
		{
			int length = uriString.Length;
			StringBuilder stringBuilder = new StringBuilder(uriString.Length);
			for (int i = 0; i < length; i++)
			{
				char c = uriString[i];
				if (System.Array.IndexOf(unescapedUriSet, c) != -1)
				{
					stringBuilder.Append(c);
					continue;
				}
				if (c >= '\udc00' && c <= '\udbff')
				{
					throw new JavaScriptException(base.Engine.UriError);
				}
				int num;
				if (c < '\ud800' || c > '\udbff')
				{
					num = c;
				}
				else
				{
					i++;
					if (i == length)
					{
						throw new JavaScriptException(base.Engine.UriError);
					}
					int num2 = uriString[i];
					if (num2 < 56320 || num2 > 57343)
					{
						throw new JavaScriptException(base.Engine.UriError);
					}
					num = (c - 55296) * 1024 + (num2 - 56320) + 65536;
				}
				byte[] array;
				if (num >= 0 && num <= 127)
				{
					array = new byte[1] { (byte)num };
				}
				else if (num <= 2047)
				{
					array = new byte[2]
					{
						(byte)(0xC0u | (uint)(num >> 6)),
						(byte)(0x80u | ((uint)num & 0x3Fu))
					};
				}
				else if (num <= 55295)
				{
					array = new byte[3]
					{
						(byte)(0xE0u | (uint)(num >> 12)),
						(byte)(0x80u | ((uint)(num >> 6) & 0x3Fu)),
						(byte)(0x80u | ((uint)num & 0x3Fu))
					};
				}
				else
				{
					if (num <= 57343)
					{
						throw new JavaScriptException(base.Engine.UriError);
					}
					array = ((num <= 65535) ? new byte[3]
					{
						(byte)(0xE0u | (uint)(num >> 12)),
						(byte)(0x80u | ((uint)(num >> 6) & 0x3Fu)),
						(byte)(0x80u | ((uint)num & 0x3Fu))
					} : new byte[4]
					{
						(byte)(0xF0u | (uint)(num >> 18)),
						(byte)(0x80u | ((uint)(num >> 12) & 0x3Fu)),
						(byte)(0x80u | ((uint)(num >> 6) & 0x3Fu)),
						(byte)(0x80u | ((uint)num & 0x3Fu))
					});
				}
				byte[] array2 = array;
				foreach (byte b in array2)
				{
					char value = "0123456789ABCDEF"[(int)b / 16];
					char value2 = "0123456789ABCDEF"[(int)b % 16];
					stringBuilder.Append('%').Append(value).Append(value2);
				}
			}
			return stringBuilder.ToString();
		}

		public JsValue DecodeUri(JsValue thisObject, JsValue[] arguments)
		{
			string uriString = TypeConverter.ToString(arguments.At(0));
			char[] reservedSet = UriReserved.Concat(new char[1] { '#' }).ToArray();
			return Decode(uriString, reservedSet);
		}

		public JsValue DecodeUriComponent(JsValue thisObject, JsValue[] arguments)
		{
			string uriString = TypeConverter.ToString(arguments.At(0));
			char[] reservedSet = new char[0];
			return Decode(uriString, reservedSet);
		}

		public string Decode(string uriString, char[] reservedSet)
		{
			int length = uriString.Length;
			StringBuilder stringBuilder = new StringBuilder(length);
			for (int i = 0; i < length; i++)
			{
				char c = uriString[i];
				if (c != '%')
				{
					stringBuilder.Append(c);
					continue;
				}
				int num = i;
				if (i + 2 >= length)
				{
					throw new JavaScriptException(base.Engine.UriError);
				}
				if (!IsValidHexaChar(uriString[i + 1]) || !IsValidHexaChar(uriString[i + 2]))
				{
					throw new JavaScriptException(base.Engine.UriError);
				}
				byte b = Convert.ToByte(uriString[i + 1].ToString() + uriString[i + 2], 16);
				i += 2;
				if ((b & 0x80) == 0)
				{
					c = (char)b;
					if (System.Array.IndexOf(reservedSet, c) == -1)
					{
						stringBuilder.Append(c);
					}
					else
					{
						stringBuilder.Append(uriString.Substring(num, i - num + 1));
					}
					continue;
				}
				int j;
				for (j = 0; ((uint)(b << j) & 0x80u) != 0; j++)
				{
				}
				if (j == 1 || j > 4)
				{
					throw new JavaScriptException(base.Engine.UriError);
				}
				byte[] array = new byte[j];
				array[0] = b;
				if (i + 3 * (j - 1) >= length)
				{
					throw new JavaScriptException(base.Engine.UriError);
				}
				for (int k = 1; k < j; k++)
				{
					i++;
					if (uriString[i] != '%')
					{
						throw new JavaScriptException(base.Engine.UriError);
					}
					if (!IsValidHexaChar(uriString[i + 1]) || !IsValidHexaChar(uriString[i + 2]))
					{
						throw new JavaScriptException(base.Engine.UriError);
					}
					b = Convert.ToByte(uriString[i + 1].ToString() + uriString[i + 2], 16);
					if ((b & 0xC0) != 128)
					{
						throw new JavaScriptException(base.Engine.UriError);
					}
					i += 2;
					array[k] = b;
				}
				stringBuilder.Append(Encoding.UTF8.GetString(array, 0, array.Length));
			}
			return stringBuilder.ToString();
		}
	}
}
