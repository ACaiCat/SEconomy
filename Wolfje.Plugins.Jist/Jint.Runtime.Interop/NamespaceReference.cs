using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;

namespace Jint.Runtime.Interop
{
	public class NamespaceReference : ObjectInstance, ICallable
	{
		private readonly string _path;

		public NamespaceReference(Engine engine, string path)
			: base(engine)
		{
			_path = path;
		}

		public override bool DefineOwnProperty(string propertyName, PropertyDescriptor desc, bool throwOnError)
		{
			if (throwOnError)
			{
				throw new JavaScriptException(base.Engine.TypeError, "Can't define a property of a NamespaceReference");
			}
			return false;
		}

		public override bool Delete(string propertyName, bool throwOnError)
		{
			if (throwOnError)
			{
				throw new JavaScriptException(base.Engine.TypeError, "Can't delete a property of a NamespaceReference");
			}
			return false;
		}

		public JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			Type[] array = new Type[arguments.Length];
			for (int i = 0; i < arguments.Length; i++)
			{
				JsValue jsValue = arguments.At(i);
				if (jsValue == Undefined.Instance || !jsValue.IsObject() || jsValue.AsObject().Class != "TypeReference")
				{
					throw new JavaScriptException(base.Engine.TypeError, "Invalid generic type parameter");
				}
				array[i] = arguments.At(i).As<TypeReference>().Type;
			}
			TypeReference typeReference = GetPath(_path + "`" + arguments.Length.ToString(CultureInfo.InvariantCulture)).As<TypeReference>();
			if (typeReference == null)
			{
				return Undefined.Instance;
			}
			Type type = typeReference.Type.MakeGenericType(array);
			return TypeReference.CreateTypeReference(base.Engine, type);
		}

		public override JsValue Get(string propertyName)
		{
			string path = _path + "." + propertyName;
			return GetPath(path);
		}

		public JsValue GetPath(string path)
		{
			if (base.Engine.TypeCache.TryGetValue(path, out var value))
			{
				if (value == null)
				{
					return new NamespaceReference(base.Engine, path);
				}
				return TypeReference.CreateTypeReference(base.Engine, value);
			}
			value = Type.GetType(path);
			if (value != null)
			{
				base.Engine.TypeCache.Add(path, value);
				return TypeReference.CreateTypeReference(base.Engine, value);
			}
			foreach (Assembly item in new Assembly[2]
			{
				Assembly.GetCallingAssembly(),
				Assembly.GetExecutingAssembly()
			}.Distinct())
			{
				value = item.GetType(path);
				if (value != null)
				{
					base.Engine.TypeCache.Add(path, value);
					return TypeReference.CreateTypeReference(base.Engine, value);
				}
			}
			foreach (Assembly lookupAssembly in base.Engine.Options._LookupAssemblies)
			{
				value = lookupAssembly.GetType(path);
				if (value != null)
				{
					base.Engine.TypeCache.Add(path, value);
					return TypeReference.CreateTypeReference(base.Engine, value);
				}
				int length = path.LastIndexOf(".", StringComparison.Ordinal);
				string typeName = path.Substring(0, length);
				value = GetType(lookupAssembly, typeName);
				if (!(value != null))
				{
					continue;
				}
				foreach (Type allNestedType in GetAllNestedTypes(value))
				{
					if (allNestedType.FullName.Replace("+", ".").Equals(path.Replace("+", ".")))
					{
						base.Engine.TypeCache.Add(path.Replace("+", "."), allNestedType);
						return TypeReference.CreateTypeReference(base.Engine, allNestedType);
					}
				}
			}
			base.Engine.TypeCache.Add(path, null);
			return new NamespaceReference(base.Engine, path);
		}

		private static Type GetType(Assembly assembly, string typeName)
		{
			Type[] types = assembly.GetTypes();
			Type[] array = types;
			Type[] array2 = array;
			foreach (Type type in array2)
			{
				if (type.FullName.Replace("+", ".") == typeName.Replace("+", "."))
				{
					return type;
				}
			}
			return null;
		}

		private static IEnumerable<Type> GetAllNestedTypes(Type type)
		{
			List<Type> list = new List<Type>();
			AddNestedTypesRecursively(list, type);
			return list.ToArray();
		}

		private static void AddNestedTypesRecursively(List<Type> types, Type type)
		{
			Type[] nestedTypes = type.GetNestedTypes(BindingFlags.Public);
			Type[] array = nestedTypes;
			Type[] array2 = array;
			foreach (Type type2 in array2)
			{
				types.Add(type2);
				AddNestedTypesRecursively(types, type2);
			}
		}

		public override PropertyDescriptor GetOwnProperty(string propertyName)
		{
			return PropertyDescriptor.Undefined;
		}

		public override string ToString()
		{
			return "[Namespace: " + _path + "]";
		}
	}
}
