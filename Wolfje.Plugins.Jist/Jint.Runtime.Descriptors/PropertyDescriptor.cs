using Jint.Native;
using Jint.Native.Object;

namespace Jint.Runtime.Descriptors
{
	public class PropertyDescriptor
	{
		public static PropertyDescriptor Undefined = new PropertyDescriptor();

		public JsValue? Get { get; set; }

		public JsValue? Set { get; set; }

		public bool? Enumerable { get; set; }

		public bool? Writable { get; set; }

		public bool? Configurable { get; set; }

		public virtual JsValue? Value { get; set; }

		public PropertyDescriptor()
		{
		}

		public PropertyDescriptor(JsValue? value, bool? writable, bool? enumerable, bool? configurable)
		{
			Value = value;
			if (writable.HasValue)
			{
				Writable = writable.Value;
			}
			if (enumerable.HasValue)
			{
				Enumerable = enumerable.Value;
			}
			if (configurable.HasValue)
			{
				Configurable = configurable.Value;
			}
		}

		public PropertyDescriptor(JsValue? get, JsValue? set, bool? enumerable = null, bool? configurable = null)
		{
			Get = get;
			Set = set;
			if (enumerable.HasValue)
			{
				Enumerable = enumerable.Value;
			}
			if (configurable.HasValue)
			{
				Configurable = configurable.Value;
			}
		}

		public PropertyDescriptor(PropertyDescriptor descriptor)
		{
			Get = descriptor.Get;
			Set = descriptor.Set;
			Value = descriptor.Value;
			Enumerable = descriptor.Enumerable;
			Configurable = descriptor.Configurable;
			Writable = descriptor.Writable;
		}

		public bool IsAccessorDescriptor()
		{
			if (!Get.HasValue && !Set.HasValue)
			{
				return false;
			}
			return true;
		}

		public bool IsDataDescriptor()
		{
			if (!Writable.HasValue && !Value.HasValue)
			{
				return false;
			}
			return true;
		}

		public bool IsGenericDescriptor()
		{
			if (!IsDataDescriptor())
			{
				return !IsAccessorDescriptor();
			}
			return false;
		}

		public static PropertyDescriptor ToPropertyDescriptor(Engine engine, JsValue o)
		{
			ObjectInstance objectInstance = o.TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(engine.TypeError);
			}
			if ((objectInstance.HasProperty("value") || objectInstance.HasProperty("writable")) && (objectInstance.HasProperty("get") || objectInstance.HasProperty("set")))
			{
				throw new JavaScriptException(engine.TypeError);
			}
			PropertyDescriptor propertyDescriptor = new PropertyDescriptor();
			if (objectInstance.HasProperty("enumerable"))
			{
				propertyDescriptor.Enumerable = TypeConverter.ToBoolean(objectInstance.Get("enumerable"));
			}
			if (objectInstance.HasProperty("configurable"))
			{
				propertyDescriptor.Configurable = TypeConverter.ToBoolean(objectInstance.Get("configurable"));
			}
			if (objectInstance.HasProperty("value"))
			{
				JsValue value = objectInstance.Get("value");
				propertyDescriptor.Value = value;
			}
			if (objectInstance.HasProperty("writable"))
			{
				propertyDescriptor.Writable = TypeConverter.ToBoolean(objectInstance.Get("writable"));
			}
			if (objectInstance.HasProperty("get"))
			{
				JsValue jsValue = objectInstance.Get("get");
				if (jsValue != JsValue.Undefined && jsValue.TryCast<ICallable>() == null)
				{
					throw new JavaScriptException(engine.TypeError);
				}
				propertyDescriptor.Get = jsValue;
			}
			if (objectInstance.HasProperty("set"))
			{
				JsValue jsValue2 = objectInstance.Get("set");
				if (jsValue2 != Jint.Native.Undefined.Instance && jsValue2.TryCast<ICallable>() == null)
				{
					throw new JavaScriptException(engine.TypeError);
				}
				propertyDescriptor.Set = jsValue2;
			}
			if ((propertyDescriptor.Get.HasValue || propertyDescriptor.Get.HasValue) && (propertyDescriptor.Value.HasValue || propertyDescriptor.Writable.HasValue))
			{
				throw new JavaScriptException(engine.TypeError);
			}
			return propertyDescriptor;
		}

		public static JsValue FromPropertyDescriptor(Engine engine, PropertyDescriptor desc)
		{
			if (desc == Undefined)
			{
				return Jint.Native.Undefined.Instance;
			}
			ObjectInstance objectInstance = engine.Object.Construct(Arguments.Empty);
			if (desc.IsDataDescriptor())
			{
				objectInstance.DefineOwnProperty("value", new PropertyDescriptor(desc.Value.HasValue ? desc.Value : new JsValue?(Jint.Native.Undefined.Instance), true, true, true), throwOnError: false);
				objectInstance.DefineOwnProperty("writable", new PropertyDescriptor(desc.Writable.HasValue && desc.Writable.Value, true, true, true), throwOnError: false);
			}
			else
			{
				objectInstance.DefineOwnProperty("get", new PropertyDescriptor(desc.Get ?? Jint.Native.Undefined.Instance, true, true, true), throwOnError: false);
				objectInstance.DefineOwnProperty("set", new PropertyDescriptor(desc.Set ?? Jint.Native.Undefined.Instance, true, true, true), throwOnError: false);
			}
			objectInstance.DefineOwnProperty("enumerable", new PropertyDescriptor(desc.Enumerable.HasValue && desc.Enumerable.Value, true, true, true), throwOnError: false);
			objectInstance.DefineOwnProperty("configurable", new PropertyDescriptor(desc.Configurable.HasValue && desc.Configurable.Value, true, true, true), throwOnError: false);
			return objectInstance;
		}
	}
}
