using System.Collections.Generic;
using System.Linq;
using Jint.Native.Function;
using Jint.Native.String;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Jint.Native.Object
{
	public sealed class ObjectConstructor : FunctionInstance, IConstructor
	{
		private readonly Engine _engine;

		public ObjectPrototype PrototypeObject { get; private set; }

		private ObjectConstructor(Engine engine)
			: base(engine, null, null, strict: false)
		{
			_engine = engine;
		}

		public static ObjectConstructor CreateObjectConstructor(Engine engine)
		{
			ObjectConstructor objectConstructor = new ObjectConstructor(engine);
			objectConstructor.Extensible = true;
			objectConstructor.PrototypeObject = ObjectPrototype.CreatePrototypeObject(engine, objectConstructor);
			objectConstructor.FastAddProperty("length", 1.0, writable: false, enumerable: false, configurable: false);
			objectConstructor.FastAddProperty("prototype", objectConstructor.PrototypeObject, writable: false, enumerable: false, configurable: false);
			return objectConstructor;
		}

		public void Configure()
		{
			base.Prototype = base.Engine.Function.PrototypeObject;
			FastAddProperty("getPrototypeOf", new ClrFunctionInstance(base.Engine, GetPrototypeOf, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getOwnPropertyDescriptor", new ClrFunctionInstance(base.Engine, GetOwnPropertyDescriptor, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("getOwnPropertyNames", new ClrFunctionInstance(base.Engine, GetOwnPropertyNames, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("create", new ClrFunctionInstance(base.Engine, Create, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("defineProperty", new ClrFunctionInstance(base.Engine, DefineProperty, 3), writable: true, enumerable: false, configurable: true);
			FastAddProperty("defineProperties", new ClrFunctionInstance(base.Engine, DefineProperties, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("seal", new ClrFunctionInstance(base.Engine, Seal, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("freeze", new ClrFunctionInstance(base.Engine, Freeze, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("preventExtensions", new ClrFunctionInstance(base.Engine, PreventExtensions, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("isSealed", new ClrFunctionInstance(base.Engine, IsSealed, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("isFrozen", new ClrFunctionInstance(base.Engine, IsFrozen, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("isExtensible", new ClrFunctionInstance(base.Engine, IsExtensible, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("keys", new ClrFunctionInstance(base.Engine, Keys, 1), writable: true, enumerable: false, configurable: true);
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			if (arguments.Length == 0)
			{
				return Construct(arguments);
			}
			if (arguments[0] == Null.Instance || arguments[0] == Undefined.Instance)
			{
				return Construct(arguments);
			}
			return TypeConverter.ToObject(_engine, arguments[0]);
		}

		public ObjectInstance Construct(JsValue[] arguments)
		{
			if (arguments.Length != 0)
			{
				JsValue value = arguments[0];
				ObjectInstance objectInstance = value.TryCast<ObjectInstance>();
				if (objectInstance != null)
				{
					return objectInstance;
				}
				Types type = value.Type;
				if (type == Types.String || type == Types.Number || type == Types.Boolean)
				{
					return TypeConverter.ToObject(_engine, value);
				}
			}
			return new ObjectInstance(_engine)
			{
				Extensible = true,
				Prototype = base.Engine.Object.PrototypeObject
			};
		}

		public JsValue GetPrototypeOf(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = arguments.At(0).TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			ObjectInstance prototype = objectInstance.Prototype;
			if (prototype == null)
			{
				return Null.Instance;
			}
			return prototype;
		}

		public JsValue GetOwnPropertyDescriptor(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = arguments.At(0).TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			JsValue jsValue = arguments.At(1);
			string propertyName = TypeConverter.ToString(jsValue);
			PropertyDescriptor ownProperty = objectInstance.GetOwnProperty(propertyName);
			return PropertyDescriptor.FromPropertyDescriptor(base.Engine, ownProperty);
		}

		public JsValue GetOwnPropertyNames(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = arguments.At(0).TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			ObjectInstance objectInstance2 = base.Engine.Array.Construct(Arguments.Empty);
			int num = 0;
			if (objectInstance is StringInstance stringInstance)
			{
				for (int i = 0; i < stringInstance.PrimitiveValue.AsString().Length; i++)
				{
					objectInstance2.DefineOwnProperty(num.ToString(), new PropertyDescriptor(i.ToString(), true, true, true), throwOnError: false);
					num++;
				}
			}
			using (IEnumerator<KeyValuePair<string, PropertyDescriptor>> enumerator = objectInstance.GetOwnProperties().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					objectInstance2.DefineOwnProperty(desc: new PropertyDescriptor(enumerator.Current.Key, true, true, true), propertyName: num.ToString(), throwOnError: false);
					num++;
				}
			}
			return objectInstance2;
		}

		public JsValue Create(JsValue thisObject, JsValue[] arguments)
		{
			JsValue jsValue = arguments.At(0);
			ObjectInstance objectInstance = jsValue.TryCast<ObjectInstance>();
			if (objectInstance == null && jsValue != Null.Instance)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			ObjectInstance objectInstance2 = base.Engine.Object.Construct(Arguments.Empty);
			objectInstance2.Prototype = objectInstance;
			JsValue jsValue2 = arguments.At(1);
			if (jsValue2 != Undefined.Instance)
			{
				DefineProperties(thisObject, new JsValue[2] { objectInstance2, jsValue2 });
			}
			return objectInstance2;
		}

		public JsValue DefineProperty(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = arguments.At(0).TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			JsValue jsValue = arguments.At(1);
			string propertyName = TypeConverter.ToString(jsValue);
			JsValue o = arguments.At(2);
			PropertyDescriptor desc = PropertyDescriptor.ToPropertyDescriptor(base.Engine, o);
			objectInstance.DefineOwnProperty(propertyName, desc, throwOnError: true);
			return objectInstance;
		}

		public JsValue DefineProperties(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = arguments.At(0).TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			JsValue value = arguments.At(1);
			ObjectInstance objectInstance2 = TypeConverter.ToObject(base.Engine, value);
			List<KeyValuePair<string, PropertyDescriptor>> list = new List<KeyValuePair<string, PropertyDescriptor>>();
			foreach (KeyValuePair<string, PropertyDescriptor> ownProperty in objectInstance2.GetOwnProperties())
			{
				if (ownProperty.Value.Enumerable.HasValue && ownProperty.Value.Enumerable.Value)
				{
					JsValue o = objectInstance2.Get(ownProperty.Key);
					PropertyDescriptor value2 = PropertyDescriptor.ToPropertyDescriptor(base.Engine, o);
					list.Add(new KeyValuePair<string, PropertyDescriptor>(ownProperty.Key, value2));
				}
			}
			foreach (KeyValuePair<string, PropertyDescriptor> item in list)
			{
				objectInstance.DefineOwnProperty(item.Key, item.Value, throwOnError: true);
			}
			return objectInstance;
		}

		public JsValue Seal(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = arguments.At(0).TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			foreach (KeyValuePair<string, PropertyDescriptor> ownProperty in objectInstance.GetOwnProperties())
			{
				if (ownProperty.Value.Configurable.HasValue && ownProperty.Value.Configurable.Value)
				{
					ownProperty.Value.Configurable = false;
				}
				objectInstance.DefineOwnProperty(ownProperty.Key, ownProperty.Value, throwOnError: true);
			}
			objectInstance.Extensible = false;
			return objectInstance;
		}

		public JsValue Freeze(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = arguments.At(0).TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			IEnumerable<string> enumerable = objectInstance.GetOwnProperties().Select(delegate(KeyValuePair<string, PropertyDescriptor> x)
			{
				KeyValuePair<string, PropertyDescriptor> keyValuePair = x;
				return keyValuePair.Key;
			});
			foreach (string item in enumerable)
			{
				PropertyDescriptor ownProperty = objectInstance.GetOwnProperty(item);
				if (ownProperty.IsDataDescriptor() && ownProperty.Writable.HasValue && ownProperty.Writable.Value)
				{
					ownProperty.Writable = false;
				}
				if (ownProperty.Configurable.HasValue && ownProperty.Configurable.Value)
				{
					ownProperty.Configurable = false;
				}
				objectInstance.DefineOwnProperty(item, ownProperty, throwOnError: true);
			}
			objectInstance.Extensible = false;
			return objectInstance;
		}

		public JsValue PreventExtensions(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = arguments.At(0).TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			objectInstance.Extensible = false;
			return objectInstance;
		}

		public JsValue IsSealed(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = arguments.At(0).TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			foreach (KeyValuePair<string, PropertyDescriptor> ownProperty in objectInstance.GetOwnProperties())
			{
				if (ownProperty.Value.Configurable.Value)
				{
					return false;
				}
			}
			if (!objectInstance.Extensible)
			{
				return true;
			}
			return false;
		}

		public JsValue IsFrozen(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = arguments.At(0).TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			foreach (string item in objectInstance.GetOwnProperties().Select(delegate(KeyValuePair<string, PropertyDescriptor> x)
			{
				KeyValuePair<string, PropertyDescriptor> keyValuePair = x;
				return keyValuePair.Key;
			}))
			{
				PropertyDescriptor ownProperty = objectInstance.GetOwnProperty(item);
				if (ownProperty.IsDataDescriptor() && ownProperty.Writable.HasValue && ownProperty.Writable.Value)
				{
					return false;
				}
				if (ownProperty.Configurable.HasValue && ownProperty.Configurable.Value)
				{
					return false;
				}
			}
			if (!objectInstance.Extensible)
			{
				return true;
			}
			return false;
		}

		public JsValue IsExtensible(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = arguments.At(0).TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			return objectInstance.Extensible;
		}

		public JsValue Keys(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = arguments.At(0).TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			KeyValuePair<string, PropertyDescriptor>[] array = objectInstance.GetOwnProperties().Where(delegate(KeyValuePair<string, PropertyDescriptor> x)
			{
				KeyValuePair<string, PropertyDescriptor> keyValuePair2 = x;
				if (keyValuePair2.Value.Enumerable.HasValue)
				{
					keyValuePair2 = x;
					return keyValuePair2.Value.Enumerable.Value;
				}
				return false;
			}).ToArray();
			int num = array.Length;
			ObjectInstance objectInstance2 = base.Engine.Array.Construct(new JsValue[1] { num });
			int num2 = 0;
			KeyValuePair<string, PropertyDescriptor>[] array2 = array;
			KeyValuePair<string, PropertyDescriptor>[] array3 = array2;
			foreach (KeyValuePair<string, PropertyDescriptor> keyValuePair in array3)
			{
				string key = keyValuePair.Key;
				objectInstance2.DefineOwnProperty(TypeConverter.ToString(num2), new PropertyDescriptor(key, true, true, true), throwOnError: false);
				num2++;
			}
			return objectInstance2;
		}
	}
}
