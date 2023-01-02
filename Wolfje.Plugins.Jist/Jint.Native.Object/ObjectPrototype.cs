using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Jint.Native.Object
{
	public sealed class ObjectPrototype : ObjectInstance
	{
		private ObjectPrototype(Engine engine)
			: base(engine)
		{
		}

		public static ObjectPrototype CreatePrototypeObject(Engine engine, ObjectConstructor objectConstructor)
		{
			ObjectPrototype objectPrototype = new ObjectPrototype(engine)
			{
				Extensible = true
			};
			objectPrototype.FastAddProperty("constructor", objectConstructor, writable: true, enumerable: false, configurable: true);
			return objectPrototype;
		}

		public void Configure()
		{
			FastAddProperty("toString", new ClrFunctionInstance(base.Engine, ToObjectString), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toLocaleString", new ClrFunctionInstance(base.Engine, ToLocaleString), writable: true, enumerable: false, configurable: true);
			FastAddProperty("valueOf", new ClrFunctionInstance(base.Engine, ValueOf), writable: true, enumerable: false, configurable: true);
			FastAddProperty("hasOwnProperty", new ClrFunctionInstance(base.Engine, HasOwnProperty, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("isPrototypeOf", new ClrFunctionInstance(base.Engine, IsPrototypeOf, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("propertyIsEnumerable", new ClrFunctionInstance(base.Engine, PropertyIsEnumerable, 1), writable: true, enumerable: false, configurable: true);
		}

		private JsValue PropertyIsEnumerable(JsValue thisObject, JsValue[] arguments)
		{
			string propertyName = TypeConverter.ToString(arguments[0]);
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObject);
			PropertyDescriptor ownProperty = objectInstance.GetOwnProperty(propertyName);
			if (ownProperty == PropertyDescriptor.Undefined)
			{
				return false;
			}
			return ownProperty.Enumerable.HasValue && ownProperty.Enumerable.Value;
		}

		private JsValue ValueOf(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObject);
			return objectInstance;
		}

		private JsValue IsPrototypeOf(JsValue thisObject, JsValue[] arguments)
		{
			JsValue jsValue = arguments[0];
			if (!jsValue.IsObject())
			{
				return false;
			}
			ObjectInstance objectInstance = jsValue.AsObject();
			ObjectInstance objectInstance2 = TypeConverter.ToObject(base.Engine, thisObject);
			do
			{
				objectInstance = objectInstance.Prototype;
				if (objectInstance == null)
				{
					return false;
				}
			}
			while (objectInstance2 != objectInstance);
			return true;
		}

		private JsValue ToLocaleString(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObject);
			ICallable callable = objectInstance.Get("toString").TryCast<ICallable>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError);
			});
			return callable.Call(objectInstance, Arguments.Empty);
		}

		public JsValue ToObjectString(JsValue thisObject, JsValue[] arguments)
		{
			if (thisObject == Undefined.Instance)
			{
				return "[object Undefined]";
			}
			if (thisObject == Null.Instance)
			{
				return "[object Null]";
			}
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObject);
			return "[object " + objectInstance.Class + "]";
		}

		public JsValue HasOwnProperty(JsValue thisObject, JsValue[] arguments)
		{
			string propertyName = TypeConverter.ToString(arguments[0]);
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObject);
			PropertyDescriptor ownProperty = objectInstance.GetOwnProperty(propertyName);
			return ownProperty != PropertyDescriptor.Undefined;
		}
	}
}
