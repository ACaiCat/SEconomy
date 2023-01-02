using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Function;
using Jint.Native.Object;

namespace Jint.Runtime.Interop
{
	public sealed class MethodInfoFunctionInstance : FunctionInstance
	{
		private readonly MethodInfo[] _methods;

		public MethodInfoFunctionInstance(Engine engine, MethodInfo[] methods)
			: base(engine, null, null, strict: false)
		{
			_methods = methods;
			base.Prototype = engine.Function.PrototypeObject;
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			return Invoke(_methods, thisObject, arguments);
		}

		public JsValue Invoke(MethodInfo[] methodInfos, JsValue thisObject, JsValue[] jsArguments)
		{
			JsValue[] array = ProcessParamsArrays(jsArguments, methodInfos);
			List<MethodBase> list = TypeConverter.FindBestMatch(base.Engine, methodInfos, array).ToList();
			ITypeConverter clrTypeConverter = base.Engine.ClrTypeConverter;
			foreach (MethodBase item in list)
			{
				object[] array2 = new object[array.Length];
				bool flag = true;
				for (int i = 0; i < array.Length; i++)
				{
					Type parameterType = item.GetParameters()[i].ParameterType;
					if (parameterType == typeof(JsValue))
					{
						array2[i] = array[i];
					}
					else if (parameterType == typeof(JsValue[]) && array[i].IsArray())
					{
						ArrayInstance arrayInstance = array[i].AsArray();
						int num = TypeConverter.ToInt32(arrayInstance.Get("length"));
						JsValue[] array3 = new JsValue[num];
						for (int j = 0; j < num; j++)
						{
							string propertyName = j.ToString();
							array3[j] = (arrayInstance.HasProperty(propertyName) ? arrayInstance.Get(propertyName) : JsValue.Undefined);
						}
						array2[i] = array3;
					}
					else
					{
						if (!clrTypeConverter.TryConvert(array[i].ToObject(), parameterType, CultureInfo.InvariantCulture, out array2[i]))
						{
							flag = false;
							break;
						}
						if (array2[i] is LambdaExpression lambdaExpression)
						{
							array2[i] = lambdaExpression.Compile();
						}
					}
				}
				if (flag)
				{
					return JsValue.FromObject(base.Engine, item.Invoke(thisObject.ToObject(), array2.ToArray()));
				}
			}
			throw new JavaScriptException(base.Engine.TypeError, "No public methods with the specified arguments were found.");
		}

		private JsValue[] ProcessParamsArrays(JsValue[] jsArguments, IEnumerable<MethodInfo> methodInfos)
		{
			foreach (MethodInfo methodInfo in methodInfos)
			{
				ParameterInfo[] parameters = methodInfo.GetParameters();
				if (!parameters.Any((ParameterInfo p) => Attribute.IsDefined(p, typeof(ParamArrayAttribute))))
				{
					continue;
				}
				int num = parameters.Length - 1;
				if (jsArguments.Length >= num)
				{
					List<JsValue> list = jsArguments.Take(num).ToList();
					List<JsValue> list2 = jsArguments.Skip(num).ToList();
					if (list2.Count != 1 || !list2.FirstOrDefault().IsArray())
					{
						ObjectInstance objectInstance = base.Engine.Array.Construct(Arguments.Empty);
						base.Engine.Array.PrototypeObject.Push(objectInstance, list2.ToArray());
						list.Add(new JsValue(objectInstance));
						return list.ToArray();
					}
				}
			}
			return jsArguments;
		}
	}
}
