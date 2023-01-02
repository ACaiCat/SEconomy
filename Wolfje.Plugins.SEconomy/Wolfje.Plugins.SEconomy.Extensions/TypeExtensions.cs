using System;
using System.Reflection;

namespace Wolfje.Plugins.SEconomy.Extensions
{
	public static class TypeExtensions
	{
		public static T CallPrivateMethod<T>(this Type _type, bool StaticMember, string Name, params object[] Params)
		{
			BindingFlags bindingFlags = BindingFlags.NonPublic;
			bindingFlags = ((!StaticMember) ? (bindingFlags | BindingFlags.Instance) : (bindingFlags | BindingFlags.Static));
			return (T)_type.GetMethod(Name, bindingFlags).Invoke(StaticMember ? null : _type, Params);
		}

		public static T GetPrivateField<T>(this Type type, object Instance, string Name, params object[] Param)
		{
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
			FieldInfo field = type.GetField(Name, bindingAttr);
			if (field == null)
			{
				return default(T);
			}
			return (T)field.GetValue(Instance);
		}
	}
}
