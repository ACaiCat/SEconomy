using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Text.RegularExpressions;
using Jint.Native.Array;
using Jint.Native.Boolean;
using Jint.Native.Date;
using Jint.Native.Function;
using Jint.Native.Number;
using Jint.Native.Object;
using Jint.Native.RegExp;
using Jint.Native.String;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Jint.Native
{
	[DebuggerTypeProxy(typeof(JsValueDebugView))]
	public struct JsValue : IEquatable<JsValue>
	{
		internal class JsValueDebugView
		{
			public string Value;

			public JsValueDebugView(JsValue value)
			{
				switch (value.Type)
				{
				case Types.None:
					Value = "None";
					break;
				case Types.Undefined:
					Value = "undefined";
					break;
				case Types.Null:
					Value = "null";
					break;
				case Types.Boolean:
					Value = value.AsBoolean() + " (bool)";
					break;
				case Types.String:
					Value = value.AsString() + " (string)";
					break;
				case Types.Number:
					Value = value.AsNumber() + " (number)";
					break;
				case Types.Object:
					Value = value.AsObject().GetType().Name;
					break;
				default:
					Value = "Unknown";
					break;
				}
			}
		}

		public static readonly JsValue Undefined = new JsValue(Types.Undefined);

		public static readonly JsValue Null = new JsValue(Types.Null);

		public static readonly JsValue False = new JsValue(value: false);

		public static readonly JsValue True = new JsValue(value: true);

		private readonly double _double;

		private readonly object _object;

		private readonly Types _type;

		public Types Type => _type;

		public JsValue(bool value)
		{
			_double = (value ? 1.0 : 0.0);
			_object = null;
			_type = Types.Boolean;
		}

		public JsValue(double value)
		{
			_object = null;
			_type = Types.Number;
			_double = value;
		}

		public JsValue(string value)
		{
			_double = double.NaN;
			_object = value;
			_type = Types.String;
		}

		public JsValue(ObjectInstance value)
		{
			_double = double.NaN;
			_type = Types.Object;
			_object = value;
		}

		private JsValue(Types type)
		{
			_double = double.NaN;
			_object = null;
			_type = type;
		}

		public bool IsPrimitive()
		{
			if (_type != Types.Object)
			{
				return _type != Types.None;
			}
			return false;
		}

		public bool IsUndefined()
		{
			return _type == Types.Undefined;
		}

		public bool IsArray()
		{
			if (IsObject())
			{
				return AsObject() is ArrayInstance;
			}
			return false;
		}

		public bool IsDate()
		{
			if (IsObject())
			{
				return AsObject() is DateInstance;
			}
			return false;
		}

		public bool IsRegExp()
		{
			if (IsObject())
			{
				return AsObject() is RegExpInstance;
			}
			return false;
		}

		public bool IsObject()
		{
			return _type == Types.Object;
		}

		public bool IsString()
		{
			return _type == Types.String;
		}

		public bool IsNumber()
		{
			return _type == Types.Number;
		}

		public bool IsBoolean()
		{
			return _type == Types.Boolean;
		}

		public bool IsNull()
		{
			return _type == Types.Null;
		}

		public ObjectInstance AsObject()
		{
			if (_type != Types.Object)
			{
				throw new ArgumentException("The value is not an object");
			}
			return _object as ObjectInstance;
		}

		public ArrayInstance AsArray()
		{
			if (!IsArray())
			{
				throw new ArgumentException("The value is not an array");
			}
			return _object as ArrayInstance;
		}

		public DateInstance AsDate()
		{
			if (!IsDate())
			{
				throw new ArgumentException("The value is not a date");
			}
			return _object as DateInstance;
		}

		public RegExpInstance AsRegExp()
		{
			if (!IsRegExp())
			{
				throw new ArgumentException("The value is not a date");
			}
			return _object as RegExpInstance;
		}

		public T TryCast<T>(Action<JsValue> fail = null) where T : class
		{
			if (IsObject())
			{
				ObjectInstance objectInstance = AsObject();
				if (objectInstance is T result)
				{
					return result;
				}
			}
			fail?.Invoke(this);
			return null;
		}

		public bool Is<T>()
		{
			if (IsObject())
			{
				return AsObject() is T;
			}
			return false;
		}

		public T As<T>() where T : ObjectInstance
		{
			return _object as T;
		}

		public bool AsBoolean()
		{
			if (_type != Types.Boolean)
			{
				throw new ArgumentException("The value is not a boolean");
			}
			return _double != 0.0;
		}

		public string AsString()
		{
			if (_type != Types.String)
			{
				throw new ArgumentException("The value is not a string");
			}
			if (_object == null)
			{
				throw new ArgumentException("The value is not defined");
			}
			return _object as string;
		}

		public double AsNumber()
		{
			if (_type != Types.Number)
			{
				throw new ArgumentException("The value is not a number");
			}
			return _double;
		}

		public bool Equals(JsValue other)
		{
			if (_type != other._type)
			{
				return false;
			}
			switch (_type)
			{
			case Types.None:
				return false;
			case Types.Undefined:
				return true;
			case Types.Null:
				return true;
			case Types.Boolean:
			case Types.Number:
				return _double == other._double;
			case Types.String:
			case Types.Object:
				return _object == other._object;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public static JsValue FromObject(Engine engine, object value)
		{
			if (value == null)
			{
				return Null;
			}
			foreach (IObjectConverter objectConverter in engine.Options._ObjectConverters)
			{
				if (objectConverter.TryConvert(value, out var result))
				{
					return result;
				}
			}
			switch (System.Type.GetTypeCode(value.GetType()))
			{
			case TypeCode.Boolean:
				return new JsValue((bool)value);
			case TypeCode.Byte:
				return new JsValue((int)(byte)value);
			case TypeCode.Char:
				return new JsValue(value.ToString());
			case TypeCode.DateTime:
				return engine.Date.Construct((DateTime)value);
			case TypeCode.Decimal:
				return new JsValue((double)(decimal)value);
			case TypeCode.Double:
				return new JsValue((double)value);
			case TypeCode.Int16:
				return new JsValue((short)value);
			case TypeCode.Int32:
				return new JsValue((int)value);
			case TypeCode.Int64:
				return new JsValue((long)value);
			case TypeCode.SByte:
				return new JsValue((sbyte)value);
			case TypeCode.Single:
				return new JsValue((float)value);
			case TypeCode.String:
				return new JsValue((string)value);
			case TypeCode.UInt16:
				return new JsValue((int)(ushort)value);
			case TypeCode.UInt32:
				return new JsValue((uint)value);
			case TypeCode.UInt64:
				return new JsValue((ulong)value);
			default:
				throw new ArgumentOutOfRangeException();
			case TypeCode.Empty:
			case TypeCode.Object:
				if (value is DateTimeOffset)
				{
					return engine.Date.Construct((DateTimeOffset)value);
				}
				if (value is ObjectInstance value2)
				{
					return new JsValue(value2);
				}
				if (value is JsValue)
				{
					return (JsValue)value;
				}
				if (value is System.Array array)
				{
					ObjectInstance objectInstance = engine.Array.Construct(Arguments.Empty);
					foreach (object item in array)
					{
						JsValue jsValue = FromObject(engine, item);
						engine.Array.PrototypeObject.Push(objectInstance, Arguments.From(jsValue));
					}
					return objectInstance;
				}
				if (value is Regex regex)
				{
					RegExpInstance regExpInstance = engine.RegExp.Construct(regex.ToString().Trim('/'));
					return regExpInstance;
				}
				if (value is Delegate d)
				{
					return new DelegateWrapper(engine, d);
				}
				if (value.GetType().IsEnum)
				{
					return new JsValue((int)value);
				}
				return new ObjectWrapper(engine, value);
			}
		}

		public object ToObject()
		{
			switch (_type)
			{
			case Types.None:
			case Types.Undefined:
			case Types.Null:
				return null;
			case Types.String:
				return _object;
			case Types.Boolean:
				return _double != 0.0;
			case Types.Number:
				return _double;
			case Types.Object:
				if (_object is IObjectWrapper objectWrapper)
				{
					return objectWrapper.Target;
				}
				switch ((_object as ObjectInstance).Class)
				{
				case "Array":
				{
					if (!(_object is ArrayInstance arrayInstance))
					{
						break;
					}
					int num = TypeConverter.ToInt32(arrayInstance.Get("length"));
					object[] array = new object[num];
					for (int i = 0; i < num; i++)
					{
						string propertyName = i.ToString();
						if (arrayInstance.HasProperty(propertyName))
						{
							JsValue jsValue = arrayInstance.Get(propertyName);
							array[i] = jsValue.ToObject();
						}
						else
						{
							array[i] = null;
						}
					}
					return array;
				}
				case "String":
					if (_object is StringInstance stringInstance)
					{
						return stringInstance.PrimitiveValue.AsString();
					}
					break;
				case "Date":
					if (_object is DateInstance dateInstance)
					{
						return dateInstance.ToDateTime();
					}
					break;
				case "Boolean":
					if (_object is BooleanInstance booleanInstance)
					{
						return booleanInstance.PrimitiveValue.AsBoolean();
					}
					break;
				case "Function":
					if (_object is FunctionInstance functionInstance)
					{
						return new Func<JsValue, JsValue[], JsValue>(functionInstance.Call);
					}
					break;
				case "Number":
					if (_object is NumberInstance numberInstance)
					{
						return numberInstance.PrimitiveValue.AsNumber();
					}
					break;
				case "RegExp":
					if (_object is RegExpInstance regExpInstance)
					{
						return regExpInstance.Value;
					}
					break;
				case "Object":
				{
					IDictionary<string, object> dictionary = new ExpandoObject();
					{
						foreach (KeyValuePair<string, PropertyDescriptor> ownProperty in (_object as ObjectInstance).GetOwnProperties())
						{
							if (ownProperty.Value.Enumerable.HasValue && ownProperty.Value.Enumerable.Value)
							{
								dictionary.Add(ownProperty.Key, (_object as ObjectInstance).Get(ownProperty.Key).ToObject());
							}
						}
						return dictionary;
					}
				}
				}
				return _object;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public JsValue Invoke(params JsValue[] arguments)
		{
			return Invoke(Undefined, arguments);
		}

		public JsValue Invoke(JsValue thisObj, JsValue[] arguments)
		{
			ICallable callable = TryCast<ICallable>();
			if (callable == null)
			{
				throw new ArgumentException("Can only invoke functions");
			}
			return callable.Call(thisObj, arguments);
		}

		public override string ToString()
		{
			switch (Type)
			{
			case Types.None:
				return "None";
			case Types.Undefined:
				return "undefined";
			case Types.Null:
				return "null";
			case Types.Boolean:
				if (_double == 0.0)
				{
					return bool.FalseString;
				}
				return bool.TrueString;
			case Types.Number:
				return _double.ToString();
			case Types.String:
			case Types.Object:
				return _object.ToString();
			default:
				return string.Empty;
			}
		}

		public static bool operator ==(JsValue a, JsValue b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(JsValue a, JsValue b)
		{
			return !a.Equals(b);
		}

		public static implicit operator JsValue(double value)
		{
			return new JsValue(value);
		}

		public static implicit operator JsValue(bool value)
		{
			return new JsValue(value);
		}

		public static implicit operator JsValue(string value)
		{
			return new JsValue(value);
		}

		public static implicit operator JsValue(ObjectInstance value)
		{
			return new JsValue(value);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is JsValue)
			{
				return Equals((JsValue)obj);
			}
			return false;
		}

		public override int GetHashCode()
		{
			int num = 0;
			num = (num * 397) ^ _double.GetHashCode();
			num = (num * 397) ^ ((_object != null) ? _object.GetHashCode() : 0);
			return (num * 397) ^ (int)_type;
		}
	}
}
