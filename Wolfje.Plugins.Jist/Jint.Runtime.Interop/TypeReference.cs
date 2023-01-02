using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Descriptors.Specialized;

namespace Jint.Runtime.Interop
{
	public class TypeReference : FunctionInstance, IConstructor, IObjectWrapper
	{
		public Type Type { get; set; }

		public object Target => Type;

		public override string Class => "TypeReference";

		private TypeReference(Engine engine)
			: base(engine, null, null, strict: false)
		{
		}

		public static TypeReference CreateTypeReference(Engine engine, Type type)
		{
			TypeReference typeReference = new TypeReference(engine);
			typeReference.Extensible = false;
			typeReference.Type = type;
			typeReference.Prototype = engine.Function.PrototypeObject;
			typeReference.FastAddProperty("length", 0.0, writable: false, enumerable: false, configurable: false);
			typeReference.FastAddProperty("prototype", engine.Object.PrototypeObject, writable: false, enumerable: false, configurable: false);
			return typeReference;
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			return Construct(arguments);
		}

		public ObjectInstance Construct(JsValue[] arguments)
		{
			if (arguments.Length == 0 && Type.IsValueType)
			{
				object value = Activator.CreateInstance(Type);
				return TypeConverter.ToObject(base.Engine, JsValue.FromObject(base.Engine, value));
			}
			ConstructorInfo[] constructors = Type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
			Engine engine = base.Engine;
			MethodBase[] methods = constructors;
			List<MethodBase> list = TypeConverter.FindBestMatch(engine, methods, arguments).ToList();
			foreach (MethodBase item in list)
			{
				object[] array = new object[arguments.Length];
				try
				{
					for (int i = 0; i < arguments.Length; i++)
					{
						Type parameterType = item.GetParameters()[i].ParameterType;
						if (parameterType == typeof(JsValue))
						{
							array[i] = arguments[i];
						}
						else
						{
							array[i] = base.Engine.ClrTypeConverter.Convert(arguments[i].ToObject(), parameterType, CultureInfo.InvariantCulture);
						}
					}
					ConstructorInfo constructorInfo = (ConstructorInfo)item;
					object value2 = constructorInfo.Invoke(array.ToArray());
					return TypeConverter.ToObject(base.Engine, JsValue.FromObject(base.Engine, value2));
				}
				catch
				{
				}
			}
			throw new JavaScriptException(base.Engine.TypeError, "No public methods with the specified arguments were found.");
		}

		public override bool DefineOwnProperty(string propertyName, PropertyDescriptor desc, bool throwOnError)
		{
			if (throwOnError)
			{
				throw new JavaScriptException(base.Engine.TypeError, "Can't define a property of a TypeReference");
			}
			return false;
		}

		public override bool Delete(string propertyName, bool throwOnError)
		{
			if (throwOnError)
			{
				throw new JavaScriptException(base.Engine.TypeError, "Can't delete a property of a TypeReference");
			}
			return false;
		}

		public override void Put(string propertyName, JsValue value, bool throwOnError)
		{
			if (!CanPut(propertyName))
			{
				if (throwOnError)
				{
					throw new JavaScriptException(base.Engine.TypeError);
				}
				return;
			}
			PropertyDescriptor ownProperty = GetOwnProperty(propertyName);
			if (ownProperty == null)
			{
				if (throwOnError)
				{
					throw new JavaScriptException(base.Engine.TypeError, "Unknown member: " + propertyName);
				}
			}
			else
			{
				ownProperty.Value = value;
			}
		}

		public override PropertyDescriptor GetOwnProperty(string propertyName)
		{
			if (Type.IsEnum)
			{
				Array values = Enum.GetValues(Type);
				Array names = Enum.GetNames(Type);
				for (int i = 0; i < values.Length; i++)
				{
					if (names.GetValue(i) as string== propertyName)
					{
						return new PropertyDescriptor((int)values.GetValue(i), false, false, false);
					}
				}
				return PropertyDescriptor.Undefined;
			}
			PropertyInfo property = Type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public);
			if (property != null)
			{
				return new PropertyInfoDescriptor(base.Engine, property, Type);
			}
			FieldInfo field = Type.GetField(propertyName, BindingFlags.Static | BindingFlags.Public);
			if (field != null)
			{
				return new FieldInfoDescriptor(base.Engine, field, Type);
			}
			MethodInfo[] array = (from mi in Type.GetMethods(BindingFlags.Static | BindingFlags.Public)
				where mi.Name == propertyName
				select mi).ToArray();
			if (array.Length == 0)
			{
				return PropertyDescriptor.Undefined;
			}
			return new PropertyDescriptor(new MethodInfoFunctionInstance(base.Engine, array), false, false, false);
		}
	}
}
