using System;
using System.Collections.Generic;
using System.Linq;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Jint.Native.Array
{
	public sealed class ArrayPrototype : ArrayInstance
	{
		private ArrayPrototype(Engine engine)
			: base(engine)
		{
		}

		public static ArrayPrototype CreatePrototypeObject(Engine engine, ArrayConstructor arrayConstructor)
		{
			ArrayPrototype arrayPrototype = new ArrayPrototype(engine)
			{
				Extensible = true,
				Prototype = engine.Object.PrototypeObject
			};
			arrayPrototype.FastAddProperty("length", 0.0, writable: true, enumerable: false, configurable: false);
			arrayPrototype.FastAddProperty("constructor", arrayConstructor, writable: true, enumerable: false, configurable: true);
			return arrayPrototype;
		}

		public void Configure()
		{
			FastAddProperty("toString", new ClrFunctionInstance(base.Engine, ToString, 0), writable: true, enumerable: false, configurable: true);
			FastAddProperty("toLocaleString", new ClrFunctionInstance(base.Engine, ToLocaleString), writable: true, enumerable: false, configurable: true);
			FastAddProperty("concat", new ClrFunctionInstance(base.Engine, Concat, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("join", new ClrFunctionInstance(base.Engine, Join, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("pop", new ClrFunctionInstance(base.Engine, Pop), writable: true, enumerable: false, configurable: true);
			FastAddProperty("push", new ClrFunctionInstance(base.Engine, Push, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("reverse", new ClrFunctionInstance(base.Engine, Reverse), writable: true, enumerable: false, configurable: true);
			FastAddProperty("shift", new ClrFunctionInstance(base.Engine, Shift), writable: true, enumerable: false, configurable: true);
			FastAddProperty("slice", new ClrFunctionInstance(base.Engine, Slice, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("sort", new ClrFunctionInstance(base.Engine, Sort, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("splice", new ClrFunctionInstance(base.Engine, Splice, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("unshift", new ClrFunctionInstance(base.Engine, Unshift, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("indexOf", new ClrFunctionInstance(base.Engine, IndexOf, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("lastIndexOf", new ClrFunctionInstance(base.Engine, LastIndexOf, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("every", new ClrFunctionInstance(base.Engine, Every, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("some", new ClrFunctionInstance(base.Engine, Some, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("forEach", new ClrFunctionInstance(base.Engine, ForEach, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("map", new ClrFunctionInstance(base.Engine, Map, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("filter", new ClrFunctionInstance(base.Engine, Filter, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("reduce", new ClrFunctionInstance(base.Engine, Reduce, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("reduceRight", new ClrFunctionInstance(base.Engine, ReduceRight, 1), writable: true, enumerable: false, configurable: true);
		}

		private JsValue LastIndexOf(JsValue thisObj, JsValue[] arguments)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			if (num == 0)
			{
				return -1.0;
			}
			double num2 = ((arguments.Length > 1) ? TypeConverter.ToInteger(arguments[1]) : ((double)(num - 1)));
			double num3 = ((!(num2 >= 0.0)) ? ((double)num - System.Math.Abs(num2)) : System.Math.Min(num2, num - 1));
			JsValue y = arguments.At(0);
			while (num3 >= 0.0)
			{
				string propertyName = TypeConverter.ToString(num3);
				if (objectInstance.HasProperty(propertyName))
				{
					JsValue x = objectInstance.Get(propertyName);
					if (ExpressionInterpreter.StrictlyEqual(x, y))
					{
						return num3;
					}
				}
				num3 -= 1.0;
			}
			return -1.0;
		}

		private JsValue Reduce(JsValue thisObj, JsValue[] arguments)
		{
			JsValue jsValue = arguments.At(0);
			JsValue jsValue2 = arguments.At(1);
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			ICallable callable = jsValue.TryCast<ICallable>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError, "Argument must be callable");
			});
			if (num == 0 && arguments.Length < 2)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			int i = 0;
			JsValue jsValue3 = Undefined.Instance;
			if (arguments.Length > 1)
			{
				jsValue3 = jsValue2;
			}
			else
			{
				bool flag = false;
				while (!flag && i < num)
				{
					string propertyName = i.ToString();
					flag = objectInstance.HasProperty(propertyName);
					if (flag)
					{
						jsValue3 = objectInstance.Get(propertyName);
					}
					i++;
				}
				if (!flag)
				{
					throw new JavaScriptException(base.Engine.TypeError);
				}
			}
			for (; i < num; i++)
			{
				string propertyName2 = i.ToString();
				if (objectInstance.HasProperty(propertyName2))
				{
					JsValue jsValue4 = objectInstance.Get(propertyName2);
					jsValue3 = callable.Call(Undefined.Instance, new JsValue[4] { jsValue3, jsValue4, i, objectInstance });
				}
			}
			return jsValue3;
		}

		private JsValue Filter(JsValue thisObj, JsValue[] arguments)
		{
			JsValue jsValue = arguments.At(0);
			JsValue thisObject = arguments.At(1);
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			ICallable callable = jsValue.TryCast<ICallable>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError, "Argument must be callable");
			});
			ArrayInstance arrayInstance = (ArrayInstance)base.Engine.Array.Construct(Arguments.Empty);
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				string propertyName = i.ToString();
				if (objectInstance.HasProperty(propertyName))
				{
					JsValue jsValue2 = objectInstance.Get(propertyName);
					JsValue o2 = callable.Call(thisObject, new JsValue[3] { jsValue2, i, objectInstance });
					if (TypeConverter.ToBoolean(o2))
					{
						arrayInstance.DefineOwnProperty(num2.ToString(), new PropertyDescriptor(jsValue2, true, true, true), throwOnError: false);
						num2++;
					}
				}
			}
			return arrayInstance;
		}

		private JsValue Map(JsValue thisObj, JsValue[] arguments)
		{
			JsValue jsValue = arguments.At(0);
			JsValue thisObject = arguments.At(1);
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			ICallable callable = jsValue.TryCast<ICallable>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError, "Argument must be callable");
			});
			ObjectInstance objectInstance2 = base.Engine.Array.Construct(new JsValue[1] { num });
			for (int i = 0; i < num; i++)
			{
				string propertyName = i.ToString();
				if (objectInstance.HasProperty(propertyName))
				{
					JsValue jsValue2 = objectInstance.Get(propertyName);
					JsValue value = callable.Call(thisObject, new JsValue[3] { jsValue2, i, objectInstance });
					objectInstance2.DefineOwnProperty(propertyName, new PropertyDescriptor(value, true, true, true), throwOnError: false);
				}
			}
			return objectInstance2;
		}

		private JsValue ForEach(JsValue thisObj, JsValue[] arguments)
		{
			JsValue jsValue = arguments.At(0);
			JsValue thisObject = arguments.At(1);
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			ICallable callable = jsValue.TryCast<ICallable>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError, "Argument must be callable");
			});
			for (int i = 0; i < num; i++)
			{
				string propertyName = i.ToString();
				if (objectInstance.HasProperty(propertyName))
				{
					JsValue jsValue2 = objectInstance.Get(propertyName);
					callable.Call(thisObject, new JsValue[3] { jsValue2, i, objectInstance });
				}
			}
			return Undefined.Instance;
		}

		private JsValue Some(JsValue thisObj, JsValue[] arguments)
		{
			JsValue jsValue = arguments.At(0);
			JsValue thisObject = arguments.At(1);
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			ICallable callable = jsValue.TryCast<ICallable>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError, "Argument must be callable");
			});
			for (int i = 0; i < num; i++)
			{
				string propertyName = i.ToString();
				if (objectInstance.HasProperty(propertyName))
				{
					JsValue jsValue2 = objectInstance.Get(propertyName);
					JsValue o2 = callable.Call(thisObject, new JsValue[3] { jsValue2, i, objectInstance });
					if (TypeConverter.ToBoolean(o2))
					{
						return true;
					}
				}
			}
			return false;
		}

		private JsValue Every(JsValue thisObj, JsValue[] arguments)
		{
			JsValue jsValue = arguments.At(0);
			JsValue thisObject = arguments.At(1);
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			ICallable callable = jsValue.TryCast<ICallable>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError, "Argument must be callable");
			});
			for (int i = 0; i < num; i++)
			{
				string propertyName = i.ToString();
				if (objectInstance.HasProperty(propertyName))
				{
					JsValue jsValue2 = objectInstance.Get(propertyName);
					JsValue o2 = callable.Call(thisObject, new JsValue[3] { jsValue2, i, objectInstance });
					if (!TypeConverter.ToBoolean(o2))
					{
						return JsValue.False;
					}
				}
			}
			return JsValue.True;
		}

		private JsValue IndexOf(JsValue thisObj, JsValue[] arguments)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			if (num == 0)
			{
				return -1.0;
			}
			double num2 = ((arguments.Length > 1) ? TypeConverter.ToInteger(arguments[1]) : 0.0);
			if (num2 >= (double)num)
			{
				return -1.0;
			}
			double num3;
			if (num2 >= 0.0)
			{
				num3 = num2;
			}
			else
			{
				num3 = (double)num - System.Math.Abs(num2);
				if (num3 < 0.0)
				{
					num3 = 0.0;
				}
			}
			JsValue y = arguments.At(0);
			for (; num3 < (double)num; num3 += 1.0)
			{
				string propertyName = TypeConverter.ToString(num3);
				if (objectInstance.HasProperty(propertyName))
				{
					JsValue x = objectInstance.Get(propertyName);
					if (ExpressionInterpreter.StrictlyEqual(x, y))
					{
						return num3;
					}
				}
			}
			return -1.0;
		}

		private JsValue Splice(JsValue thisObj, JsValue[] arguments)
		{
			JsValue o = arguments.At(0);
			JsValue o2 = arguments.At(1);
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			ObjectInstance objectInstance2 = base.Engine.Array.Construct(Arguments.Empty);
			JsValue o3 = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o3);
			double num2 = TypeConverter.ToInteger(o);
			uint num3 = ((!(num2 < 0.0)) ? ((uint)System.Math.Min(num2, num)) : ((uint)System.Math.Max((double)num + num2, 0.0)));
			double num4 = System.Math.Min(System.Math.Max(TypeConverter.ToInteger(o2), 0.0), num - num3);
			for (int i = 0; (double)i < num4; i++)
			{
				string propertyName = (num3 + i).ToString();
				if (objectInstance.HasProperty(propertyName))
				{
					JsValue value = objectInstance.Get(propertyName);
					objectInstance2.DefineOwnProperty(i.ToString(), new PropertyDescriptor(value, true, true, true), throwOnError: false);
				}
			}
			JsValue[] array = arguments.Skip(2).ToArray();
			if ((double)array.Length < num4)
			{
				for (uint num5 = num3; (double)num5 < (double)num - num4; num5++)
				{
					string propertyName2 = ((double)num5 + num4).ToString();
					string propertyName3 = (num5 + array.Length).ToString();
					if (objectInstance.HasProperty(propertyName2))
					{
						JsValue value2 = objectInstance.Get(propertyName2);
						objectInstance.Put(propertyName3, value2, throwOnError: true);
					}
					else
					{
						objectInstance.Delete(propertyName3, throwOnError: true);
					}
				}
				uint num6 = num;
				while ((double)num6 > (double)num - num4 + (double)array.Length)
				{
					objectInstance.Delete((num6 - 1).ToString(), throwOnError: true);
					num6--;
				}
			}
			else if ((double)array.Length > num4)
			{
				for (double num7 = (double)num - num4; num7 > (double)num3; num7 -= 1.0)
				{
					string propertyName4 = (num7 + num4 - 1.0).ToString();
					string propertyName5 = (num7 + (double)array.Length - 1.0).ToString();
					if (objectInstance.HasProperty(propertyName4))
					{
						JsValue value3 = objectInstance.Get(propertyName4);
						objectInstance.Put(propertyName5, value3, throwOnError: true);
					}
					else
					{
						objectInstance.Delete(propertyName5, throwOnError: true);
					}
				}
			}
			for (int j = 0; j < array.Length; j++)
			{
				JsValue value4 = array[j];
				objectInstance.Put((j + num3).ToString(), value4, throwOnError: true);
			}
			objectInstance.Put("length", (double)num - num4 + (double)array.Length, throwOnError: true);
			return objectInstance2;
		}

		private JsValue Unshift(JsValue thisObj, JsValue[] arguments)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			uint num2 = (uint)arguments.Length;
			for (uint num3 = num; num3 != 0; num3--)
			{
				string propertyName = (num3 - 1).ToString();
				string propertyName2 = (num3 + num2 - 1).ToString();
				if (objectInstance.HasProperty(propertyName))
				{
					JsValue value = objectInstance.Get(propertyName);
					objectInstance.Put(propertyName2, value, throwOnError: true);
				}
				else
				{
					objectInstance.Delete(propertyName2, throwOnError: true);
				}
			}
			for (int i = 0; i < num2; i++)
			{
				objectInstance.Put(i.ToString(), arguments[i], throwOnError: true);
			}
			objectInstance.Put("length", num + num2, throwOnError: true);
			return num + num2;
		}

		private JsValue Sort(JsValue thisObj, JsValue[] arguments)
		{
			if (!thisObj.IsObject())
			{
				throw new JavaScriptException(base.Engine.TypeError, "Array.prorotype.sort can only be applied on objects");
			}
			ObjectInstance obj = thisObj.AsObject();
			JsValue o = obj.Get("length");
			int num = TypeConverter.ToInt32(o);
			if (num <= 1)
			{
				return obj;
			}
			JsValue jsValue = arguments.At(0);
			ICallable compareFn = null;
			if (jsValue != Undefined.Instance)
			{
				compareFn = jsValue.TryCast<ICallable>(delegate
				{
					throw new JavaScriptException(base.Engine.TypeError, "The sort argument must be a function");
				});
			}
			Comparison<JsValue> comparison = delegate(JsValue x, JsValue y)
			{
				if (x == Undefined.Instance && y == Undefined.Instance)
				{
					return 0;
				}
				if (x == Undefined.Instance)
				{
					return 1;
				}
				if (y == Undefined.Instance)
				{
					return -1;
				}
				if (compareFn != null)
				{
					double num3 = TypeConverter.ToNumber(compareFn.Call(Undefined.Instance, new JsValue[2] { x, y }));
					if (num3 < 0.0)
					{
						return -1;
					}
					if (num3 > 0.0)
					{
						return 1;
					}
					return 0;
				}
				string strA = TypeConverter.ToString(x);
				string strB = TypeConverter.ToString(y);
				return string.CompareOrdinal(strA, strB);
			};
			JsValue[] array = Enumerable.Range(0, num).Select(delegate(int i)
			{
				ObjectInstance objectInstance = obj;
				int num2 = i;
				return objectInstance.Get(num2.ToString());
			}).ToArray();
			try
			{
				System.Array.Sort(array, comparison);
			}
			catch (InvalidOperationException ex)
			{
				throw ex.InnerException;
			}
			foreach (int item in Enumerable.Range(0, num))
			{
				obj.Put(item.ToString(), array[item], throwOnError: false);
			}
			return obj;
		}

		private JsValue Slice(JsValue thisObj, JsValue[] arguments)
		{
			JsValue o = arguments.At(0);
			JsValue jsValue = arguments.At(1);
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			ObjectInstance objectInstance2 = base.Engine.Array.Construct(Arguments.Empty);
			JsValue o2 = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o2);
			double num2 = TypeConverter.ToInteger(o);
			uint num3 = ((!(num2 < 0.0)) ? ((uint)System.Math.Min(TypeConverter.ToInteger(o), num)) : ((uint)System.Math.Max((double)num + num2, 0.0)));
			uint num4;
			if (jsValue == Undefined.Instance)
			{
				num4 = TypeConverter.ToUint32(num);
			}
			else
			{
				double num5 = TypeConverter.ToInteger(jsValue);
				num4 = ((!(num5 < 0.0)) ? ((uint)System.Math.Min(TypeConverter.ToInteger(num5), num)) : ((uint)System.Math.Max((double)num + num5, 0.0)));
			}
			int num6 = 0;
			for (; num3 < num4; num3++)
			{
				string propertyName = TypeConverter.ToString(num3);
				if (objectInstance.HasProperty(propertyName))
				{
					JsValue value = objectInstance.Get(propertyName);
					objectInstance2.DefineOwnProperty(TypeConverter.ToString(num6), new PropertyDescriptor(value, true, true, true), throwOnError: false);
				}
				num6++;
			}
			return objectInstance2;
		}

		private JsValue Shift(JsValue thisObj, JsValue[] arg2)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			if (num == 0)
			{
				objectInstance.Put("length", 0.0, throwOnError: true);
				return Undefined.Instance;
			}
			JsValue result = objectInstance.Get("0");
			for (int i = 1; i < num; i++)
			{
				string propertyName = TypeConverter.ToString(i);
				string propertyName2 = TypeConverter.ToString(i - 1);
				if (objectInstance.HasProperty(propertyName))
				{
					JsValue value = objectInstance.Get(propertyName);
					objectInstance.Put(propertyName2, value, throwOnError: true);
				}
				else
				{
					objectInstance.Delete(propertyName2, throwOnError: true);
				}
			}
			objectInstance.Delete(TypeConverter.ToString(num - 1), throwOnError: true);
			objectInstance.Put("length", num - 1, throwOnError: true);
			return result;
		}

		private JsValue Reverse(JsValue thisObj, JsValue[] arguments)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			uint num2 = (uint)System.Math.Floor((double)num / 2.0);
			for (uint num3 = 0u; num3 != num2; num3++)
			{
				uint num4 = num - num3 - 1;
				string propertyName = TypeConverter.ToString(num4);
				string propertyName2 = TypeConverter.ToString(num3);
				JsValue value = objectInstance.Get(propertyName2);
				JsValue value2 = objectInstance.Get(propertyName);
				bool flag = objectInstance.HasProperty(propertyName2);
				bool flag2 = objectInstance.HasProperty(propertyName);
				if (flag && flag2)
				{
					objectInstance.Put(propertyName2, value2, throwOnError: true);
					objectInstance.Put(propertyName, value, throwOnError: true);
				}
				if (!flag && flag2)
				{
					objectInstance.Put(propertyName2, value2, throwOnError: true);
					objectInstance.Delete(propertyName, throwOnError: true);
				}
				if (flag && !flag2)
				{
					objectInstance.Delete(propertyName2, throwOnError: true);
					objectInstance.Put(propertyName, value, throwOnError: true);
				}
			}
			return objectInstance;
		}

		private JsValue Join(JsValue thisObj, JsValue[] arguments)
		{
			JsValue jsValue = arguments.At(0);
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			if (jsValue == Undefined.Instance)
			{
				jsValue = ",";
			}
			string text = TypeConverter.ToString(jsValue);
			if (num == 0)
			{
				return "";
			}
			JsValue jsValue2 = objectInstance.Get("0");
			string text2 = ((jsValue2 == Undefined.Instance || jsValue2 == Null.Instance) ? "" : TypeConverter.ToString(jsValue2));
			for (int i = 1; i < num; i++)
			{
				string text3 = text2 + text;
				JsValue jsValue3 = objectInstance.Get(i.ToString());
				string text4 = ((jsValue3 == Undefined.Instance || jsValue3 == Null.Instance) ? "" : TypeConverter.ToString(jsValue3));
				text2 = text3 + text4;
			}
			return text2;
		}

		private JsValue ToLocaleString(JsValue thisObj, JsValue[] arguments)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			if (num == 0)
			{
				return "";
			}
			JsValue jsValue = objectInstance.Get("0");
			JsValue jsValue2;
			if (jsValue == Null.Instance || jsValue == Undefined.Instance)
			{
				jsValue2 = "";
			}
			else
			{
				ObjectInstance objectInstance2 = TypeConverter.ToObject(base.Engine, jsValue);
				ICallable callable = objectInstance2.Get("toLocaleString").TryCast<ICallable>(delegate
				{
					throw new JavaScriptException(base.Engine.TypeError);
				});
				jsValue2 = callable.Call(objectInstance2, Arguments.Empty);
			}
			for (int i = 1; i < num; i++)
			{
				JsValue jsValue3 = jsValue2;
				string text = jsValue3.ToString() + ",";
				JsValue jsValue4 = objectInstance.Get(i.ToString());
				if (jsValue4 == Undefined.Instance || jsValue4 == Null.Instance)
				{
					jsValue2 = "";
				}
				else
				{
					ObjectInstance objectInstance3 = TypeConverter.ToObject(base.Engine, jsValue4);
					ICallable callable2 = objectInstance3.Get("toLocaleString").TryCast<ICallable>(delegate
					{
						throw new JavaScriptException(base.Engine.TypeError);
					});
					jsValue2 = callable2.Call(objectInstance3, Arguments.Empty);
				}
				jsValue3 = jsValue2;
				jsValue2 = text + jsValue3.ToString();
			}
			return jsValue2;
		}

		private JsValue Concat(JsValue thisObj, JsValue[] arguments)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			ObjectInstance objectInstance2 = base.Engine.Array.Construct(Arguments.Empty);
			int num = 0;
			List<JsValue> list = new List<JsValue> { objectInstance };
			list.AddRange(arguments);
			foreach (JsValue item in list)
			{
				ArrayInstance arrayInstance = item.TryCast<ArrayInstance>();
				if (arrayInstance != null)
				{
					uint num2 = TypeConverter.ToUint32(arrayInstance.Get("length"));
					for (int i = 0; i < num2; i++)
					{
						string propertyName = i.ToString();
						if (arrayInstance.HasProperty(propertyName))
						{
							JsValue value = arrayInstance.Get(propertyName);
							objectInstance2.DefineOwnProperty(TypeConverter.ToString(num), new PropertyDescriptor(value, true, true, true), throwOnError: false);
						}
						num++;
					}
				}
				else
				{
					objectInstance2.DefineOwnProperty(TypeConverter.ToString(num), new PropertyDescriptor(item, true, true, true), throwOnError: false);
					num++;
				}
			}
			objectInstance2.DefineOwnProperty("length", new PropertyDescriptor(num, null, null, null), throwOnError: false);
			return objectInstance2;
		}

		private JsValue ToString(JsValue thisObj, JsValue[] arguments)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			ICallable func = objectInstance.Get("join").TryCast<ICallable>(delegate
			{
				func = base.Engine.Object.PrototypeObject.Get("toString").TryCast<ICallable>(delegate
				{
					throw new ArgumentException();
				});
			});
			return func.Call(objectInstance, Arguments.Empty);
		}

		private JsValue ReduceRight(JsValue thisObj, JsValue[] arguments)
		{
			JsValue jsValue = arguments.At(0);
			JsValue jsValue2 = arguments.At(1);
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObj);
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			ICallable callable = jsValue.TryCast<ICallable>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError, "Argument must be callable");
			});
			if (num == 0 && arguments.Length < 2)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			int num2 = (int)(num - 1);
			JsValue jsValue3 = Undefined.Instance;
			if (arguments.Length > 1)
			{
				jsValue3 = jsValue2;
			}
			else
			{
				bool flag = false;
				while (!flag && num2 >= 0)
				{
					string propertyName = num2.ToString();
					flag = objectInstance.HasProperty(propertyName);
					if (flag)
					{
						jsValue3 = objectInstance.Get(propertyName);
					}
					num2--;
				}
				if (!flag)
				{
					throw new JavaScriptException(base.Engine.TypeError);
				}
			}
			while (num2 >= 0)
			{
				string propertyName2 = num2.ToString();
				if (objectInstance.HasProperty(propertyName2))
				{
					JsValue jsValue4 = objectInstance.Get(propertyName2);
					jsValue3 = callable.Call(Undefined.Instance, new JsValue[4] { jsValue3, jsValue4, num2, objectInstance });
				}
				num2--;
			}
			return jsValue3;
		}

		public JsValue Push(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObject);
			double num = TypeConverter.ToNumber(objectInstance.Get("length"));
			double num2 = TypeConverter.ToUint32(num);
			foreach (JsValue value in arguments)
			{
				objectInstance.Put(TypeConverter.ToString(num2), value, throwOnError: true);
				num2 += 1.0;
			}
			objectInstance.Put("length", num2, throwOnError: true);
			return num2;
		}

		public JsValue Pop(JsValue thisObject, JsValue[] arguments)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(base.Engine, thisObject);
			double num = TypeConverter.ToNumber(objectInstance.Get("length"));
			uint num2 = TypeConverter.ToUint32(num);
			if (num2 == 0)
			{
				objectInstance.Put("length", 0.0, throwOnError: true);
				return Undefined.Instance;
			}
			num2--;
			string propertyName = TypeConverter.ToString(num2);
			JsValue result = objectInstance.Get(propertyName);
			objectInstance.Delete(propertyName, throwOnError: true);
			objectInstance.Put("length", num2, throwOnError: true);
			return result;
		}
	}
}
