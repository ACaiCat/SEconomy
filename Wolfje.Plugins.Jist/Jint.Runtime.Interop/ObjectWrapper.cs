using System;
using System.Linq;
using System.Reflection;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Descriptors.Specialized;

namespace Jint.Runtime.Interop
{
	public sealed class ObjectWrapper : ObjectInstance, IObjectWrapper
	{
		public object Target { get; set; }

		public ObjectWrapper(Engine engine, object obj)
			: base(engine)
		{
			Target = obj;
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
			if (base.Properties.TryGetValue(propertyName, out var value))
			{
				return value;
			}
			Type type = Target.GetType();
			PropertyInfo propertyInfo = (from p in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
				where EqualsIgnoreCasing(p.Name, propertyName)
				select p).FirstOrDefault();
			if (propertyInfo != null)
			{
				PropertyInfoDescriptor propertyInfoDescriptor = new PropertyInfoDescriptor(base.Engine, propertyInfo, Target);
				base.Properties.Add(propertyName, propertyInfoDescriptor);
				return propertyInfoDescriptor;
			}
			FieldInfo fieldInfo = (from f in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
				where EqualsIgnoreCasing(f.Name, propertyName)
				select f).FirstOrDefault();
			if (fieldInfo != null)
			{
				FieldInfoDescriptor fieldInfoDescriptor = new FieldInfoDescriptor(base.Engine, fieldInfo, Target);
				base.Properties.Add(propertyName, fieldInfoDescriptor);
				return fieldInfoDescriptor;
			}
			MethodInfo[] array = (from m in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
				where EqualsIgnoreCasing(m.Name, propertyName)
				select m).ToArray();
			if (array.Any())
			{
				PropertyDescriptor propertyDescriptor = new PropertyDescriptor(new MethodInfoFunctionInstance(base.Engine, array), false, true, false);
				base.Properties.Add(propertyName, propertyDescriptor);
				return propertyDescriptor;
			}
			if ((from p in type.GetProperties()
				where p.GetIndexParameters().Length != 0
				select p).FirstOrDefault() != null)
			{
				return new IndexDescriptor(base.Engine, propertyName, Target);
			}
			Type[] interfaces = type.GetInterfaces();
			PropertyInfo[] array2 = (from iface in interfaces
				from iprop in iface.GetProperties()
				where EqualsIgnoreCasing(iprop.Name, propertyName)
				select iprop).ToArray();
			if (array2.Length == 1)
			{
				PropertyInfoDescriptor propertyInfoDescriptor2 = new PropertyInfoDescriptor(base.Engine, array2[0], Target);
				base.Properties.Add(propertyName, propertyInfoDescriptor2);
				return propertyInfoDescriptor2;
			}
			MethodInfo[] array3 = (from iface in interfaces
				from imethod in iface.GetMethods()
				where EqualsIgnoreCasing(imethod.Name, propertyName)
				select imethod).ToArray();
			if (array3.Length != 0)
			{
				PropertyDescriptor propertyDescriptor2 = new PropertyDescriptor(new MethodInfoFunctionInstance(base.Engine, array3), false, true, false);
				base.Properties.Add(propertyName, propertyDescriptor2);
				return propertyDescriptor2;
			}
			PropertyInfo[] array4 = (from iface in interfaces
				from iprop in iface.GetProperties()
				where iprop.GetIndexParameters().Length != 0
				select iprop).ToArray();
			if (array4.Length == 1)
			{
				return new IndexDescriptor(base.Engine, array4[0].DeclaringType, propertyName, Target);
			}
			return PropertyDescriptor.Undefined;
		}

		private bool EqualsIgnoreCasing(string s1, string s2)
		{
			bool flag = false;
			if (s1.Length == s2.Length)
			{
				if (s1.Length > 0 && s2.Length > 0)
				{
					flag = s1.ToLower()[0] == s2.ToLower()[0];
				}
				if (s1.Length > 1 && s2.Length > 1)
				{
					flag = flag && s1.Substring(1) == s2.Substring(1);
				}
			}
			return flag;
		}
	}
}
