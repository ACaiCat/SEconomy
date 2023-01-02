using System;
using System.Text.RegularExpressions;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime;

namespace Jint.Native.RegExp
{
	public sealed class RegExpConstructor : FunctionInstance, IConstructor
	{
		public RegExpPrototype PrototypeObject { get; private set; }

		public RegExpConstructor(Engine engine)
			: base(engine, null, null, strict: false)
		{
		}

		public static RegExpConstructor CreateRegExpConstructor(Engine engine)
		{
			RegExpConstructor regExpConstructor = new RegExpConstructor(engine);
			regExpConstructor.Extensible = true;
			regExpConstructor.Prototype = engine.Function.PrototypeObject;
			regExpConstructor.PrototypeObject = RegExpPrototype.CreatePrototypeObject(engine, regExpConstructor);
			regExpConstructor.FastAddProperty("length", 2.0, writable: false, enumerable: false, configurable: false);
			regExpConstructor.FastAddProperty("prototype", regExpConstructor.PrototypeObject, writable: false, enumerable: false, configurable: false);
			return regExpConstructor;
		}

		public void Configure()
		{
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			JsValue jsValue = arguments.At(0);
			JsValue jsValue2 = arguments.At(1);
			if (jsValue != Undefined.Instance && jsValue2 == Undefined.Instance && TypeConverter.ToObject(base.Engine, jsValue).Class == "Regex")
			{
				return jsValue;
			}
			return Construct(arguments);
		}

		public ObjectInstance Construct(JsValue[] arguments)
		{
			JsValue jsValue = arguments.At(0);
			JsValue jsValue2 = arguments.At(1);
			RegExpInstance regExpInstance = jsValue.TryCast<RegExpInstance>();
			if (jsValue2 == Undefined.Instance && regExpInstance != null)
			{
				return regExpInstance;
			}
			if (jsValue2 != Undefined.Instance && regExpInstance != null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			string text = ((!(jsValue == Undefined.Instance)) ? TypeConverter.ToString(jsValue) : "");
			string flags = ((jsValue2 != Undefined.Instance) ? TypeConverter.ToString(jsValue2) : "");
			regExpInstance = new RegExpInstance(base.Engine);
			regExpInstance.Prototype = PrototypeObject;
			regExpInstance.Extensible = true;
			RegexOptions options = ParseOptions(regExpInstance, flags);
			try
			{
				regExpInstance.Value = new Regex(text, options);
			}
			catch (Exception ex)
			{
				throw new JavaScriptException(base.Engine.SyntaxError, ex.Message);
			}
			string text2 = text;
			if (string.IsNullOrEmpty(text2))
			{
				text2 = "(?:)";
			}
			regExpInstance.Flags = flags;
			regExpInstance.Source = text2;
			regExpInstance.FastAddProperty("global", regExpInstance.Global, writable: false, enumerable: false, configurable: false);
			regExpInstance.FastAddProperty("ignoreCase", regExpInstance.IgnoreCase, writable: false, enumerable: false, configurable: false);
			regExpInstance.FastAddProperty("multiline", regExpInstance.Multiline, writable: false, enumerable: false, configurable: false);
			regExpInstance.FastAddProperty("source", regExpInstance.Source, writable: false, enumerable: false, configurable: false);
			regExpInstance.FastAddProperty("lastIndex", 0.0, writable: true, enumerable: false, configurable: false);
			return regExpInstance;
		}

		public RegExpInstance Construct(string regExp)
		{
			RegExpInstance regExpInstance = new RegExpInstance(base.Engine);
			regExpInstance.Prototype = PrototypeObject;
			regExpInstance.Extensible = true;
			if (regExp[0] != '/')
			{
				throw new JavaScriptException(base.Engine.SyntaxError, "Regexp should start with slash");
			}
			int num = regExp.LastIndexOf('/');
			string text = regExp.Substring(1, num - 1).Replace("\\/", "/");
			string flags = regExp.Substring(num + 1);
			RegexOptions regexOptions = ParseOptions(regExpInstance, flags);
			try
			{
				if ((RegexOptions.Multiline & regexOptions) == RegexOptions.Multiline)
				{
					int num2 = 0;
					string text2 = text;
					while ((num2 = text2.IndexOf("$", num2)) != -1)
					{
						if (num2 > 0 && text2[num2 - 1] != '\\')
						{
							text2 = text2.Substring(0, num2) + "\\r?" + text2.Substring(num2);
							num2 += 4;
						}
					}
					regExpInstance.Value = new Regex(text2, regexOptions);
				}
				else
				{
					regExpInstance.Value = new Regex(text, regexOptions);
				}
			}
			catch (Exception ex)
			{
				throw new JavaScriptException(base.Engine.SyntaxError, ex.Message);
			}
			regExpInstance.Flags = flags;
			regExpInstance.Source = (string.IsNullOrEmpty(text) ? "(?:)" : text);
			regExpInstance.FastAddProperty("global", regExpInstance.Global, writable: false, enumerable: false, configurable: false);
			regExpInstance.FastAddProperty("ignoreCase", regExpInstance.IgnoreCase, writable: false, enumerable: false, configurable: false);
			regExpInstance.FastAddProperty("multiline", regExpInstance.Multiline, writable: false, enumerable: false, configurable: false);
			regExpInstance.FastAddProperty("source", regExpInstance.Source, writable: false, enumerable: false, configurable: false);
			regExpInstance.FastAddProperty("lastIndex", 0.0, writable: true, enumerable: false, configurable: false);
			return regExpInstance;
		}

		private RegexOptions ParseOptions(RegExpInstance r, string flags)
		{
			for (int i = 0; i < flags.Length; i++)
			{
				switch (flags[i])
				{
				case 'g':
					if (r.Global)
					{
						throw new JavaScriptException(base.Engine.SyntaxError);
					}
					r.Global = true;
					break;
				case 'i':
					if (r.IgnoreCase)
					{
						throw new JavaScriptException(base.Engine.SyntaxError);
					}
					r.IgnoreCase = true;
					break;
				case 'm':
					if (r.Multiline)
					{
						throw new JavaScriptException(base.Engine.SyntaxError);
					}
					r.Multiline = true;
					break;
				default:
					throw new JavaScriptException(base.Engine.SyntaxError);
				}
			}
			RegexOptions regexOptions = RegexOptions.ECMAScript;
			if (r.Multiline)
			{
				regexOptions |= RegexOptions.Multiline;
			}
			if (r.IgnoreCase)
			{
				regexOptions |= RegexOptions.IgnoreCase;
			}
			return regexOptions;
		}
	}
}
