using System;
using System.Collections.Generic;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Descriptors.Specialized;
using Jint.Runtime.Environments;

namespace Jint.Native.Argument
{
	public class ArgumentsInstance : ObjectInstance
	{
		private Action<ArgumentsInstance> _initializer;

		private bool _initialized;

		public bool Strict { get; set; }

		public ObjectInstance ParameterMap { get; set; }

		public override string Class => "Arguments";

		private ArgumentsInstance(Engine engine, Action<ArgumentsInstance> initializer)
			: base(engine)
		{
			_initializer = initializer;
			_initialized = false;
		}

		protected override void EnsureInitialized()
		{
			if (!_initialized)
			{
				_initialized = true;
				_initializer(this);
			}
		}

		public static ArgumentsInstance CreateArgumentsObject(Engine engine, FunctionInstance func, string[] names, JsValue[] args, EnvironmentRecord env, bool strict)
		{
			ArgumentsInstance argumentsInstance = new ArgumentsInstance(engine, delegate(ArgumentsInstance self)
			{
				int num = args.Length;
				self.FastAddProperty("length", num, writable: true, enumerable: false, configurable: true);
				ObjectInstance objectInstance = engine.Object.Construct(Arguments.Empty);
				List<string> list = new List<string>();
				string name = null;
				for (int i = 0; i <= num - 1; i++)
				{
					string text = TypeConverter.ToString(i);
					JsValue value = args[i];
					self.FastAddProperty(text, value, writable: true, enumerable: true, configurable: true);
					if (i < names.Length)
					{
						name = names[i];
						if (!strict && !list.Contains(name))
						{
							list.Add(name);
							Func<JsValue, JsValue> get = (JsValue n) => env.GetBindingValue(name, strict: false);
							Action<JsValue, JsValue> set = delegate(JsValue n, JsValue o)
							{
								env.SetMutableBinding(name, o, strict: true);
							};
							objectInstance.DefineOwnProperty(text, new ClrAccessDescriptor(engine, get, set)
							{
								Configurable = true
							}, throwOnError: false);
						}
					}
				}
				if (list.Count > 0)
				{
					self.ParameterMap = objectInstance;
				}
				if (!strict)
				{
					self.FastAddProperty("callee", func, writable: true, enumerable: false, configurable: true);
				}
				else
				{
					FunctionInstance throwTypeError = engine.Function.ThrowTypeError;
					self.DefineOwnProperty("caller", new PropertyDescriptor(throwTypeError, throwTypeError, false, false), throwOnError: false);
					self.DefineOwnProperty("callee", new PropertyDescriptor(throwTypeError, throwTypeError, false, false), throwOnError: false);
				}
			});
			argumentsInstance.Prototype = engine.Object.PrototypeObject;
			argumentsInstance.Extensible = true;
			argumentsInstance.Strict = strict;
			return argumentsInstance;
		}

		public override PropertyDescriptor GetOwnProperty(string propertyName)
		{
			EnsureInitialized();
			if (!Strict && ParameterMap != null)
			{
				PropertyDescriptor ownProperty = base.GetOwnProperty(propertyName);
				if (ownProperty == PropertyDescriptor.Undefined)
				{
					return ownProperty;
				}
				PropertyDescriptor ownProperty2 = ParameterMap.GetOwnProperty(propertyName);
				if (ownProperty2 != PropertyDescriptor.Undefined)
				{
					ownProperty.Value = ParameterMap.Get(propertyName);
				}
				return ownProperty;
			}
			return base.GetOwnProperty(propertyName);
		}

		public override void Put(string propertyName, JsValue value, bool throwOnError)
		{
			EnsureInitialized();
			if (!CanPut(propertyName))
			{
				if (throwOnError)
				{
					throw new JavaScriptException(base.Engine.TypeError);
				}
				return;
			}
			PropertyDescriptor ownProperty = GetOwnProperty(propertyName);
			if (ownProperty.IsDataDescriptor())
			{
				PropertyDescriptor desc = new PropertyDescriptor(value, null, null, null);
				DefineOwnProperty(propertyName, desc, throwOnError);
				return;
			}
			PropertyDescriptor property = GetProperty(propertyName);
			if (property.IsAccessorDescriptor())
			{
				ICallable callable = property.Set.Value.TryCast<ICallable>();
				callable.Call(new JsValue(this), new JsValue[1] { value });
			}
			else
			{
				PropertyDescriptor desc2 = new PropertyDescriptor(value, true, true, true);
				DefineOwnProperty(propertyName, desc2, throwOnError);
			}
		}

		public override bool DefineOwnProperty(string propertyName, PropertyDescriptor desc, bool throwOnError)
		{
			EnsureInitialized();
			if (!Strict && ParameterMap != null)
			{
				ObjectInstance parameterMap = ParameterMap;
				PropertyDescriptor ownProperty = parameterMap.GetOwnProperty(propertyName);
				if (!base.DefineOwnProperty(propertyName, desc, throwOnError: false) && throwOnError)
				{
					throw new JavaScriptException(base.Engine.TypeError);
				}
				if (ownProperty != PropertyDescriptor.Undefined)
				{
					if (desc.IsAccessorDescriptor())
					{
						parameterMap.Delete(propertyName, throwOnError: false);
					}
					else
					{
						if (desc.Value.HasValue && desc.Value.Value != Undefined.Instance)
						{
							parameterMap.Put(propertyName, desc.Value.Value, throwOnError);
						}
						if (desc.Writable.HasValue && !desc.Writable.Value)
						{
							parameterMap.Delete(propertyName, throwOnError: false);
						}
					}
				}
				return true;
			}
			return base.DefineOwnProperty(propertyName, desc, throwOnError);
		}

		public override bool Delete(string propertyName, bool throwOnError)
		{
			EnsureInitialized();
			if (!Strict && ParameterMap != null)
			{
				ObjectInstance parameterMap = ParameterMap;
				PropertyDescriptor ownProperty = parameterMap.GetOwnProperty(propertyName);
				bool flag = base.Delete(propertyName, throwOnError);
				if (flag && ownProperty != PropertyDescriptor.Undefined)
				{
					parameterMap.Delete(propertyName, throwOnError: false);
				}
				return flag;
			}
			return base.Delete(propertyName, throwOnError);
		}
	}
}
