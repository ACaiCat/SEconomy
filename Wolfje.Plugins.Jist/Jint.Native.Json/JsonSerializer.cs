using System;
using System.Collections.Generic;
using System.Linq;
using Jint.Native.Array;
using Jint.Native.Global;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;

namespace Jint.Native.Json
{
	public class JsonSerializer
	{
		private readonly Engine _engine;

		private Stack<object> _stack;

		private string _indent;

		private string _gap;

		private List<string> _propertyList;

		private JsValue _replacerFunction = Undefined.Instance;

		public JsonSerializer(Engine engine)
		{
			_engine = engine;
		}

		public JsValue Serialize(JsValue value, JsValue replacer, JsValue space)
		{
			_stack = new Stack<object>();
			if (value.Is<ICallable>() && replacer == Undefined.Instance)
			{
				return Undefined.Instance;
			}
			if (replacer.IsObject())
			{
				if (replacer.Is<ICallable>())
				{
					_replacerFunction = replacer;
				}
				else
				{
					ObjectInstance objectInstance = replacer.AsObject();
					if (objectInstance.Class == "Array")
					{
						_propertyList = new List<string>();
					}
					foreach (PropertyDescriptor item in objectInstance.GetOwnProperties().Select(delegate(KeyValuePair<string, PropertyDescriptor> x)
					{
						KeyValuePair<string, PropertyDescriptor> keyValuePair = x;
						return keyValuePair.Value;
					}))
					{
						JsValue value2 = _engine.GetValue(item);
						string text = null;
						if (value2.IsString())
						{
							text = value2.AsString();
						}
						else if (value2.IsNumber())
						{
							text = TypeConverter.ToString(value2);
						}
						else if (value2.IsObject())
						{
							ObjectInstance objectInstance2 = value2.AsObject();
							if (objectInstance2.Class == "String" || objectInstance2.Class == "Number")
							{
								text = TypeConverter.ToString(value2);
							}
						}
						if (text != null && !_propertyList.Contains(text))
						{
							_propertyList.Add(text);
						}
					}
				}
			}
			if (space.IsObject())
			{
				ObjectInstance objectInstance3 = space.AsObject();
				if (objectInstance3.Class == "Number")
				{
					space = TypeConverter.ToNumber(objectInstance3);
				}
				else if (objectInstance3.Class == "String")
				{
					space = TypeConverter.ToString(objectInstance3);
				}
			}
			if (space.IsNumber())
			{
				if (space.AsNumber() > 0.0)
				{
					_gap = new string(' ', (int)System.Math.Min(10.0, space.AsNumber()));
				}
				else
				{
					_gap = string.Empty;
				}
			}
			else if (space.IsString())
			{
				string text2 = space.AsString();
				_gap = ((text2.Length <= 10) ? text2 : text2.Substring(0, 10));
			}
			else
			{
				_gap = string.Empty;
			}
			ObjectInstance objectInstance4 = _engine.Object.Construct(Arguments.Empty);
			objectInstance4.DefineOwnProperty("", new PropertyDescriptor(value, true, true, true), throwOnError: false);
			return Str("", objectInstance4);
		}

		private JsValue Str(string key, ObjectInstance holder)
		{
			JsValue jsValue = holder.Get(key);
			if (jsValue.IsObject())
			{
				JsValue jsValue2 = jsValue.AsObject().Get("toJSON");
				if (jsValue2.IsObject() && jsValue2.AsObject() is ICallable callable)
				{
					jsValue = callable.Call(jsValue, Arguments.From(key));
				}
			}
			if (_replacerFunction != Undefined.Instance)
			{
				ICallable callable2 = (ICallable)_replacerFunction.AsObject();
				jsValue = callable2.Call(holder, Arguments.From(key, jsValue));
			}
			if (jsValue.IsObject())
			{
				ObjectInstance objectInstance = jsValue.AsObject();
				switch (objectInstance.Class)
				{
				case "Number":
					jsValue = TypeConverter.ToNumber(jsValue);
					break;
				case "String":
					jsValue = TypeConverter.ToString(jsValue);
					break;
				case "Boolean":
					jsValue = TypeConverter.ToPrimitive(jsValue);
					break;
				case "Array":
					return SerializeArray(jsValue.As<ArrayInstance>());
				case "Object":
					return SerializeObject(jsValue.AsObject());
				}
			}
			if (jsValue == Null.Instance)
			{
				return "null";
			}
			if (jsValue.IsBoolean() && jsValue.AsBoolean())
			{
				return "true";
			}
			if (jsValue.IsBoolean() && !jsValue.AsBoolean())
			{
				return "false";
			}
			if (jsValue.IsString())
			{
				return Quote(jsValue.AsString());
			}
			if (jsValue.IsNumber())
			{
				if (GlobalObject.IsFinite(Undefined.Instance, Arguments.From(jsValue)).AsBoolean())
				{
					return TypeConverter.ToString(jsValue);
				}
				return "null";
			}
			bool flag = jsValue.IsObject() && jsValue.AsObject() is ICallable;
			if (jsValue.IsObject() && !flag)
			{
				if (jsValue.AsObject().Class == "Array")
				{
					return SerializeArray(jsValue.As<ArrayInstance>());
				}
				return SerializeObject(jsValue.AsObject());
			}
			return JsValue.Undefined;
		}

		private string Quote(string value)
		{
			string text = "\"";
			for (int i = 0; i < value.Length; i++)
			{
				char c = value[i];
				switch (c)
				{
				case '"':
					text += "\\\"";
					continue;
				case '\\':
					text += "\\\\";
					continue;
				case '\b':
					text += "\\b";
					continue;
				case '\f':
					text += "\\f";
					continue;
				case '\n':
					text += "\\n";
					continue;
				case '\r':
					text += "\\r";
					continue;
				case '\t':
					text += "\\t";
					continue;
				}
				if (c < ' ')
				{
					text += "\\u";
					int num = c;
					text += num.ToString("x4");
				}
				else
				{
					text += c;
				}
			}
			return text + "\"";
		}

		private string SerializeArray(ArrayInstance value)
		{
			EnsureNonCyclicity(value);
			_stack.Push(value);
			string indent = _indent;
			_indent += _gap;
			List<string> list = new List<string>();
			uint num = TypeConverter.ToUint32(value.Get("length"));
			for (int i = 0; i < num; i++)
			{
				JsValue jsValue = Str(TypeConverter.ToString(i), value);
				if (jsValue == JsValue.Undefined)
				{
					jsValue = "null";
				}
				list.Add(jsValue.AsString());
			}
			if (list.Count == 0)
			{
				return "[]";
			}
			string result;
			if (_gap == "")
			{
				string separator = ",";
				string text = string.Join(separator, list.ToArray());
				result = "[" + text + "]";
			}
			else
			{
				string separator2 = ",\n" + _indent;
				string text2 = string.Join(separator2, list.ToArray());
				result = "[\n" + _indent + text2 + "\n" + indent + "]";
			}
			_stack.Pop();
			_indent = indent;
			return result;
		}

		private void EnsureNonCyclicity(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (_stack.Contains(value))
			{
				throw new JavaScriptException(_engine.TypeError, "Cyclic reference detected.");
			}
		}

		private string SerializeObject(ObjectInstance value)
		{
			EnsureNonCyclicity(value);
			_stack.Push(value);
			string indent = _indent;
			_indent += _gap;
			List<string> list = _propertyList ?? value.GetOwnProperties().Where(delegate(KeyValuePair<string, PropertyDescriptor> x)
			{
				KeyValuePair<string, PropertyDescriptor> keyValuePair2 = x;
				if (keyValuePair2.Value.Enumerable.HasValue)
				{
					keyValuePair2 = x;
					return keyValuePair2.Value.Enumerable.Value;
				}
				return false;
			}).Select(delegate(KeyValuePair<string, PropertyDescriptor> x)
			{
				KeyValuePair<string, PropertyDescriptor> keyValuePair = x;
				return keyValuePair.Key;
			})
				.ToList();
			List<string> list2 = new List<string>();
			foreach (string item in list)
			{
				JsValue jsValue = Str(item, value);
				if (jsValue != JsValue.Undefined)
				{
					string text = Quote(item) + ":";
					if (_gap != "")
					{
						text += " ";
					}
					text += jsValue.AsString();
					list2.Add(text);
				}
			}
			string result;
			if (list2.Count == 0)
			{
				result = "{}";
			}
			else if (_gap == "")
			{
				string separator = ",";
				string text2 = string.Join(separator, list2.ToArray());
				result = "{" + text2 + "}";
			}
			else
			{
				string separator2 = ",\n" + _indent;
				string text3 = string.Join(separator2, list2.ToArray());
				result = "{\n" + _indent + text3 + "\n" + indent + "}";
			}
			_stack.Pop();
			_indent = indent;
			return result;
		}
	}
}
