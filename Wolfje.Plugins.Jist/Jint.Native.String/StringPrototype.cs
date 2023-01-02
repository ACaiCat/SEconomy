using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Jint.Native.Array;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Native.RegExp;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Jint.Native.String
{
	public sealed class StringPrototype : StringInstance
	{
		private const char BOM_CHAR = '\ufeff';

		private const char MONGOLIAN_VOWEL_SEPARATOR = '\u180e';

		private StringPrototype(Engine engine)
			: base(engine)
		{
		}

		public static StringPrototype CreatePrototypeObject(Engine engine, StringConstructor stringConstructor)
		{
			StringPrototype stringPrototype = new StringPrototype(engine);
			stringPrototype.Prototype = engine.Object.PrototypeObject;
			stringPrototype.PrimitiveValue = "";
			stringPrototype.Extensible = true;
			stringPrototype.FastAddProperty("length", 0.0, writable: false, enumerable: false, configurable: false);
			stringPrototype.FastAddProperty("constructor", stringConstructor, writable: true, enumerable: false, configurable: true);
			return stringPrototype;
		}

		public void Configure()
		{
			FastAddProperty("toString", new ClrFunctionInstance(base.Engine, ToStringString), writable: true, enumerable: false, configurable: true);
			FastAddProperty("valueOf", new ClrFunctionInstance(base.Engine, ValueOf), writable: true, enumerable: false, configurable: true);
			FastAddProperty("charAt", new ClrFunctionInstance(base.Engine, CharAt, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("charCodeAt", new ClrFunctionInstance(base.Engine, CharCodeAt, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("concat", new ClrFunctionInstance(base.Engine, Concat, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("indexOf", new ClrFunctionInstance(base.Engine, IndexOf, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("lastIndexOf", new ClrFunctionInstance(base.Engine, LastIndexOf, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("localeCompare", new ClrFunctionInstance(base.Engine, LocaleCompare, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("match", new ClrFunctionInstance(base.Engine, Match, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("replace", new ClrFunctionInstance(base.Engine, Replace, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("search", new ClrFunctionInstance(base.Engine, Search, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("slice", new ClrFunctionInstance(base.Engine, Slice, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("split", new ClrFunctionInstance(base.Engine, Split, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("substr", new ClrFunctionInstance(base.Engine, Substr, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("substring", new ClrFunctionInstance(base.Engine, Substring, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toLowerCase", new ClrFunctionInstance(base.Engine, ToLowerCase), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toLocaleLowerCase", new ClrFunctionInstance(base.Engine, ToLocaleLowerCase), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toUpperCase", new ClrFunctionInstance(base.Engine, ToUpperCase), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toLocaleUpperCase", new ClrFunctionInstance(base.Engine, ToLocaleUpperCase), writable: true, enumerable: false, configurable: true);
			FastAddProperty("trim", new ClrFunctionInstance(base.Engine, Trim), writable: true, enumerable: false, configurable: true);
		}

		private JsValue ToStringString(JsValue thisObj, JsValue[] arguments)
		{
			if (!(TypeConverter.ToObject(base.Engine, thisObj) is StringInstance stringInstance))
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			return stringInstance.PrimitiveValue;
		}

		private static bool IsWhiteSpaceEx(char c)
		{
			if (!char.IsWhiteSpace(c) && c != '\ufeff')
			{
				return c == '\u180e';
			}
			return true;
		}

		public static string TrimEndEx(string s)
		{
			if (s.Length == 0)
			{
				return string.Empty;
			}
			int num = s.Length - 1;
			while (num >= 0 && IsWhiteSpaceEx(s[num]))
			{
				num--;
			}
			if (num >= 0)
			{
				return s.Substring(0, num + 1);
			}
			return string.Empty;
		}

		public static string TrimStartEx(string s)
		{
			if (s.Length == 0)
			{
				return string.Empty;
			}
			int i;
			for (i = 0; i < s.Length && IsWhiteSpaceEx(s[i]); i++)
			{
			}
			if (i >= s.Length)
			{
				return string.Empty;
			}
			return s.Substring(i);
		}

		public static string TrimEx(string s)
		{
			return TrimEndEx(TrimStartEx(s));
		}

		private JsValue Trim(JsValue thisObj, JsValue[] arguments)
		{
			TypeConverter.CheckObjectCoercible(base.Engine, thisObj);
			string s = TypeConverter.ToString(thisObj);
			return TrimEx(s);
		}

		private static JsValue ToLocaleUpperCase(JsValue thisObj, JsValue[] arguments)
		{
			string text = TypeConverter.ToString(thisObj);
			return text.ToUpper();
		}

		private static JsValue ToUpperCase(JsValue thisObj, JsValue[] arguments)
		{
			string text = TypeConverter.ToString(thisObj);
			return text.ToUpperInvariant();
		}

		private static JsValue ToLocaleLowerCase(JsValue thisObj, JsValue[] arguments)
		{
			string text = TypeConverter.ToString(thisObj);
			return text.ToLower();
		}

		private static JsValue ToLowerCase(JsValue thisObj, JsValue[] arguments)
		{
			string text = TypeConverter.ToString(thisObj);
			return text.ToLowerInvariant();
		}

		private static int ToIntegerSupportInfinity(JsValue numberVal)
		{
			double num = TypeConverter.ToInteger(numberVal);
			int num2 = (int)num;
			if (double.IsPositiveInfinity(num))
			{
				return int.MaxValue;
			}
			if (double.IsNegativeInfinity(num))
			{
				return int.MinValue;
			}
			return (int)num;
		}

		private JsValue Substring(JsValue thisObj, JsValue[] arguments)
		{
			TypeConverter.CheckObjectCoercible(base.Engine, thisObj);
			string text = TypeConverter.ToString(thisObj);
			double num = TypeConverter.ToNumber(arguments.At(0));
			double num2 = TypeConverter.ToNumber(arguments.At(1));
			if (double.IsNaN(num) || num < 0.0)
			{
				num = 0.0;
			}
			if (double.IsNaN(num2) || num2 < 0.0)
			{
				num2 = 0.0;
			}
			int length = text.Length;
			int val = ToIntegerSupportInfinity(num);
			int val2 = ((arguments.At(1) == Undefined.Instance) ? length : ToIntegerSupportInfinity(num2));
			int val3 = System.Math.Min(length, System.Math.Max(val, 0));
			int val4 = System.Math.Min(length, System.Math.Max(val2, 0));
			int num3 = System.Math.Min(val3, val4);
			int num4 = System.Math.Max(val3, val4);
			return text.Substring(num3, num4 - num3);
		}

		private JsValue Substr(JsValue thisObj, JsValue[] arguments)
		{
			string text = TypeConverter.ToString(thisObj);
			double num = TypeConverter.ToInteger(arguments.At(0));
			double val = ((arguments.At(1) == JsValue.Undefined) ? double.PositiveInfinity : TypeConverter.ToInteger(arguments.At(1)));
			num = ((num >= 0.0) ? num : System.Math.Max((double)text.Length + num, 0.0));
			val = System.Math.Min(System.Math.Max(val, 0.0), (double)text.Length - num);
			if (val <= 0.0)
			{
				return "";
			}
			return text.Substring(TypeConverter.ToInt32(num), TypeConverter.ToInt32(val));
		}

		private JsValue Split(JsValue thisObj, JsValue[] arguments)
		{
			TypeConverter.CheckObjectCoercible(base.Engine, thisObj);
			string text = TypeConverter.ToString(thisObj);
			JsValue jsValue = arguments.At(0);
			JsValue jsValue2 = arguments.At(1);
			ArrayInstance arrayInstance = (ArrayInstance)base.Engine.Array.Construct(Arguments.Empty);
			uint num = ((jsValue2 == Undefined.Instance) ? uint.MaxValue : TypeConverter.ToUint32(jsValue2));
			int length = text.Length;
			if (num == 0)
			{
				return arrayInstance;
			}
			if (jsValue == Null.Instance)
			{
				jsValue = Null.Text;
			}
			else
			{
				if (jsValue == Undefined.Instance)
				{
					return (ArrayInstance)base.Engine.Array.Construct(Arguments.From(text));
				}
				if (!jsValue.IsRegExp())
				{
					jsValue = TypeConverter.ToString(jsValue);
				}
			}
			RegExpInstance regExpInstance = TypeConverter.ToObject(base.Engine, jsValue) as RegExpInstance;
			if (regExpInstance != null && regExpInstance.Source != "(?:)")
			{
				Match match = regExpInstance.Value.Match(text, 0);
				if (!match.Success)
				{
					arrayInstance.DefineOwnProperty("0", new PropertyDescriptor(text, true, true, true), throwOnError: false);
					return arrayInstance;
				}
				int num2 = 0;
				int num3 = 0;
				while (match.Success && num3 < num)
				{
					if (match.Length == 0 && (match.Index == 0 || match.Index == length || match.Index == num2))
					{
						match = match.NextMatch();
						continue;
					}
					arrayInstance.DefineOwnProperty(num3++.ToString(), new PropertyDescriptor(text.Substring(num2, match.Index - num2), true, true, true), throwOnError: false);
					if (num3 >= num)
					{
						return arrayInstance;
					}
					num2 = match.Index + match.Length;
					for (int i = 1; i < match.Groups.Count; i++)
					{
						Group group = match.Groups[i];
						JsValue value = Undefined.Instance;
						if (group.Captures.Count > 0)
						{
							value = match.Groups[i].Value;
						}
						arrayInstance.DefineOwnProperty(num3++.ToString(), new PropertyDescriptor(value, true, true, true), throwOnError: false);
						if (num3 >= num)
						{
							return arrayInstance;
						}
					}
					match = match.NextMatch();
					if (!match.Success)
					{
						arrayInstance.DefineOwnProperty(num3++.ToString(), new PropertyDescriptor(text.Substring(num2), true, true, true), throwOnError: false);
					}
				}
				return arrayInstance;
			}
			List<string> list = new List<string>();
			string text2 = TypeConverter.ToString(jsValue);
			if (text2 == string.Empty || (regExpInstance != null && regExpInstance.Source == "(?:)"))
			{
				string text3 = text;
				for (int j = 0; j < text3.Length; j++)
				{
					list.Add(text3[j].ToString());
				}
			}
			else
			{
				list = text.Split(new string[1] { text2 }, StringSplitOptions.None).ToList();
			}
			for (int k = 0; k < list.Count && k < num; k++)
			{
				arrayInstance.DefineOwnProperty(k.ToString(), new PropertyDescriptor(list[k], true, true, true), throwOnError: false);
			}
			return arrayInstance;
		}

		private JsValue Slice(JsValue thisObj, JsValue[] arguments)
		{
			TypeConverter.CheckObjectCoercible(base.Engine, thisObj);
			string text = TypeConverter.ToString(thisObj);
			double num = TypeConverter.ToNumber(arguments.At(0));
			if (double.NegativeInfinity.Equals(num))
			{
				num = 0.0;
			}
			if (double.PositiveInfinity.Equals(num))
			{
				return string.Empty;
			}
			double num2 = TypeConverter.ToNumber(arguments.At(1));
			if (double.PositiveInfinity.Equals(num2))
			{
				num2 = text.Length;
			}
			int length = text.Length;
			int num3 = (int)TypeConverter.ToInteger(num);
			int num4 = ((arguments.At(1) == Undefined.Instance) ? length : ((int)TypeConverter.ToInteger(num2)));
			int num5 = ((num3 < 0) ? System.Math.Max(length + num3, 0) : System.Math.Min(num3, length));
			int num6 = ((num4 < 0) ? System.Math.Max(length + num4, 0) : System.Math.Min(num4, length));
			int length2 = System.Math.Max(num6 - num5, 0);
			return text.Substring(num5, length2);
		}

		private JsValue Search(JsValue thisObj, JsValue[] arguments)
		{
			TypeConverter.CheckObjectCoercible(base.Engine, thisObj);
			string input = TypeConverter.ToString(thisObj);
			JsValue jsValue = arguments.At(0);
			if (jsValue.IsUndefined())
			{
				jsValue = string.Empty;
			}
			else if (jsValue.IsNull())
			{
				jsValue = Null.Text;
			}
			RegExpInstance regExpInstance = (TypeConverter.ToObject(base.Engine, jsValue) as RegExpInstance) ?? ((RegExpInstance)base.Engine.RegExp.Construct(new JsValue[1] { jsValue }));
			Match match = regExpInstance.Value.Match(input);
			if (!match.Success)
			{
				return -1.0;
			}
			return match.Index;
		}

		private JsValue Replace(JsValue thisObj, JsValue[] arguments)
		{
			TypeConverter.CheckObjectCoercible(base.Engine, thisObj);
			string thisString = TypeConverter.ToString(thisObj);
			JsValue jsValue = arguments.At(0);
			JsValue replaceValue = arguments.At(1);
			FunctionInstance replaceFunction = replaceValue.TryCast<FunctionInstance>();
			if (replaceFunction == null)
			{
				replaceFunction = new ClrFunctionInstance(base.Engine, delegate(JsValue self, JsValue[] args)
				{
					string text3 = TypeConverter.ToString(replaceValue);
					string text4 = TypeConverter.ToString(args.At(0));
					int num3 = (int)TypeConverter.ToInteger(args.At(args.Length - 2));
					if (text3.IndexOf('$') < 0)
					{
						return text3;
					}
					StringBuilder stringBuilder2 = new StringBuilder();
					for (int j = 0; j < text3.Length; j++)
					{
						char c = text3[j];
						if (c == '$' && j < text3.Length - 1)
						{
							c = text3[++j];
							switch (c)
							{
							case '$':
								stringBuilder2.Append('$');
								break;
							case '&':
								stringBuilder2.Append(text4);
								break;
							case '`':
								stringBuilder2.Append(thisString.Substring(0, num3));
								break;
							case '\'':
								stringBuilder2.Append(thisString.Substring(num3 + text4.Length));
								break;
							case '0':
							case '1':
							case '2':
							case '3':
							case '4':
							case '5':
							case '6':
							case '7':
							case '8':
							case '9':
							{
								int num4 = c - 48;
								int num5 = 0;
								if (j < text3.Length - 1 && text3[j + 1] >= '0' && text3[j + 1] <= '9')
								{
									num5 = num4 * 10 + (text3[j + 1] - 48);
								}
								if (num5 > 0 && num5 < args.Length - 2)
								{
									stringBuilder2.Append(TypeConverter.ToString(args[num5]));
									j++;
								}
								else if (num4 > 0 && num4 < args.Length - 2)
								{
									stringBuilder2.Append(TypeConverter.ToString(args[num4]));
								}
								else
								{
									stringBuilder2.Append('$');
									j--;
								}
								break;
							}
							default:
								stringBuilder2.Append('$');
								stringBuilder2.Append(c);
								break;
							}
						}
						else
						{
							stringBuilder2.Append(c);
						}
					}
					return stringBuilder2.ToString();
				});
			}
			if (jsValue.IsNull())
			{
				jsValue = new JsValue(Null.Text);
			}
			if (jsValue.IsUndefined())
			{
				jsValue = new JsValue(Undefined.Text);
			}
			if (TypeConverter.ToObject(base.Engine, jsValue) is RegExpInstance regExpInstance)
			{
				string text = regExpInstance.Value.Replace(thisString, delegate(Match match)
				{
					List<JsValue> list2 = new List<JsValue>();
					for (int i = 0; i < match.Groups.Count; i++)
					{
						Group group = match.Groups[i];
						list2.Add(group.Value);
					}
					list2.Add(match.Index);
					list2.Add(thisString);
					return TypeConverter.ToString(replaceFunction.Call(Undefined.Instance, list2.ToArray()));
				}, (!regExpInstance.Global) ? 1 : (-1));
				return text;
			}
			string text2 = TypeConverter.ToString(jsValue);
			int num = thisString.IndexOf(text2, StringComparison.Ordinal);
			if (num == -1)
			{
				return thisString;
			}
			int num2 = num + text2.Length;
			List<JsValue> list = new List<JsValue>();
			list.Add(text2);
			list.Add(num);
			list.Add(thisString);
			string value = TypeConverter.ToString(replaceFunction.Call(Undefined.Instance, list.ToArray()));
			StringBuilder stringBuilder = new StringBuilder(thisString.Length + (text2.Length - text2.Length));
			stringBuilder.Append(thisString, 0, num);
			stringBuilder.Append(value);
			stringBuilder.Append(thisString, num2, thisString.Length - num2);
			return stringBuilder.ToString();
		}

		private JsValue Match(JsValue thisObj, JsValue[] arguments)
		{
			TypeConverter.CheckObjectCoercible(base.Engine, thisObj);
			string text = TypeConverter.ToString(thisObj);
			JsValue jsValue = arguments.At(0);
			RegExpInstance regExpInstance = jsValue.TryCast<RegExpInstance>();
			regExpInstance = regExpInstance ?? ((RegExpInstance)base.Engine.RegExp.Construct(new JsValue[1] { jsValue }));
			if (!regExpInstance.Get("global").AsBoolean())
			{
				return base.Engine.RegExp.PrototypeObject.Exec(regExpInstance, Arguments.From(text));
			}
			regExpInstance.Put("lastIndex", 0.0, throwOnError: false);
			ObjectInstance objectInstance = base.Engine.Array.Construct(Arguments.Empty);
			double num = 0.0;
			int num2 = 0;
			bool flag = true;
			while (flag)
			{
				ObjectInstance objectInstance2 = base.Engine.RegExp.PrototypeObject.Exec(regExpInstance, Arguments.From(text)).TryCast<ObjectInstance>();
				if (objectInstance2 == null)
				{
					flag = false;
					continue;
				}
				double num3 = regExpInstance.Get("lastIndex").AsNumber();
				if (num3 == num)
				{
					regExpInstance.Put("lastIndex", num3 + 1.0, throwOnError: false);
					num = num3;
				}
				JsValue value = objectInstance2.Get("0");
				objectInstance.DefineOwnProperty(TypeConverter.ToString(num2), new PropertyDescriptor(value, true, true, true), throwOnError: false);
				num2++;
			}
			if (num2 == 0)
			{
				return Null.Instance;
			}
			return objectInstance;
		}

		private JsValue LocaleCompare(JsValue thisObj, JsValue[] arguments)
		{
			TypeConverter.CheckObjectCoercible(base.Engine, thisObj);
			string strA = TypeConverter.ToString(thisObj);
			string strB = TypeConverter.ToString(arguments.At(0));
			return string.CompareOrdinal(strA, strB);
		}

		private JsValue LastIndexOf(JsValue thisObj, JsValue[] arguments)
		{
			TypeConverter.CheckObjectCoercible(base.Engine, thisObj);
			string text = TypeConverter.ToString(thisObj);
			string text2 = TypeConverter.ToString(arguments.At(0));
			double num = double.NaN;
			if (arguments.Length > 1 && arguments[1] != Undefined.Instance)
			{
				num = TypeConverter.ToNumber(arguments[1]);
			}
			double val = (double.IsNaN(num) ? double.PositiveInfinity : TypeConverter.ToInteger(num));
			int length = text.Length;
			int num2 = (int)System.Math.Min(System.Math.Max(val, 0.0), length);
			int length2 = text2.Length;
			int num3 = num2;
			bool flag;
			do
			{
				flag = true;
				int num4 = 0;
				while (flag && num4 < length2)
				{
					if (num3 + length2 > length || text[num3 + num4] != text2[num4])
					{
						flag = false;
					}
					else
					{
						num4++;
					}
				}
				if (!flag)
				{
					num3--;
				}
			}
			while (!flag && num3 >= 0);
			return num3;
		}

		private JsValue IndexOf(JsValue thisObj, JsValue[] arguments)
		{
			TypeConverter.CheckObjectCoercible(base.Engine, thisObj);
			string text = TypeConverter.ToString(thisObj);
			string value = TypeConverter.ToString(arguments.At(0));
			double num = 0.0;
			if (arguments.Length > 1 && arguments[1] != Undefined.Instance)
			{
				num = TypeConverter.ToInteger(arguments[1]);
			}
			if (num >= (double)text.Length)
			{
				return -1.0;
			}
			if (num < 0.0)
			{
				num = 0.0;
			}
			return text.IndexOf(value, (int)num, StringComparison.Ordinal);
		}

		private JsValue Concat(JsValue thisObj, JsValue[] arguments)
		{
			TypeConverter.CheckObjectCoercible(base.Engine, thisObj);
			string value = TypeConverter.ToString(thisObj);
			StringBuilder stringBuilder = new StringBuilder(value);
			for (int i = 0; i < arguments.Length; i++)
			{
				stringBuilder.Append(TypeConverter.ToString(arguments[i]));
			}
			return stringBuilder.ToString();
		}

		private JsValue CharCodeAt(JsValue thisObj, JsValue[] arguments)
		{
			TypeConverter.CheckObjectCoercible(base.Engine, thisObj);
			JsValue o = ((arguments.Length != 0) ? arguments[0] : ((JsValue)0.0));
			string text = TypeConverter.ToString(thisObj);
			int num = (int)TypeConverter.ToInteger(o);
			if (num < 0 || num >= text.Length)
			{
				return double.NaN;
			}
			return (int)text[num];
		}

		private JsValue CharAt(JsValue thisObj, JsValue[] arguments)
		{
			TypeConverter.CheckObjectCoercible(base.Engine, thisObj);
			string text = TypeConverter.ToString(thisObj);
			double num = TypeConverter.ToInteger(arguments.At(0));
			int length = text.Length;
			if (num >= (double)length || num < 0.0)
			{
				return "";
			}
			return text[(int)num].ToString();
		}

		private JsValue ValueOf(JsValue thisObj, JsValue[] arguments)
		{
			StringInstance stringInstance = thisObj.TryCast<StringInstance>();
			if (stringInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			return stringInstance.PrimitiveValue;
		}
	}
}
