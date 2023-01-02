using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Jint.Native;
using Jint.Native.Function;

namespace Jint.Runtime.Interop
{
	public sealed class DelegateWrapper : FunctionInstance
	{
		private readonly Delegate _d;

		public DelegateWrapper(Engine engine, Delegate d)
			: base(engine, null, null, strict: false)
		{
			_d = d;
		}

		public override JsValue Call(JsValue thisObject, JsValue[] jsArguments)
		{
			ParameterInfo[] parameters = _d.Method.GetParameters();
			bool flag = parameters.Any((ParameterInfo p) => Attribute.IsDefined(p, typeof(ParamArrayAttribute)));
			int num = parameters.Length;
			int num2 = (flag ? (num - 1) : num);
			int num3 = jsArguments.Length;
			int num4 = Math.Min(num3, num2);
			object[] array = new object[num];
			for (int i = 0; i < num4; i++)
			{
				Type parameterType = parameters[i].ParameterType;
				if (parameterType == typeof(JsValue))
				{
					array[i] = jsArguments[i];
				}
				else
				{
					array[i] = base.Engine.ClrTypeConverter.Convert(jsArguments[i].ToObject(), parameterType, CultureInfo.InvariantCulture);
				}
			}
			for (int j = num4; j < num2; j++)
			{
				if (parameters[j].ParameterType.IsValueType)
				{
					array[j] = Activator.CreateInstance(parameters[j].ParameterType);
				}
				else
				{
					array[j] = null;
				}
			}
			if (flag)
			{
				int num5 = num - 1;
				int num6 = Math.Max(0, num3 - num2);
				object[] array2 = new object[num6];
				Type elementType = parameters[num5].ParameterType.GetElementType();
				for (int k = num5; k < num3; k++)
				{
					int num7 = k - num5;
					if (elementType == typeof(JsValue))
					{
						array2[num7] = jsArguments[k];
					}
					else
					{
						array2[num7] = base.Engine.ClrTypeConverter.Convert(jsArguments[k].ToObject(), elementType, CultureInfo.InvariantCulture);
					}
				}
				array[num5] = array2;
			}
			return JsValue.FromObject(base.Engine, _d.DynamicInvoke(array));
		}
	}
}
