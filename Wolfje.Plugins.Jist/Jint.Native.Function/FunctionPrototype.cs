using System;
using System.Collections.Generic;
using System.Linq;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Jint.Native.Function
{
	public sealed class FunctionPrototype : FunctionInstance
	{
		private FunctionPrototype(Engine engine)
			: base(engine, null, null, strict: false)
		{
		}

		public static FunctionPrototype CreatePrototypeObject(Engine engine)
		{
			FunctionPrototype functionPrototype = new FunctionPrototype(engine);
			functionPrototype.Extensible = true;
			functionPrototype.Prototype = engine.Object.PrototypeObject;
			functionPrototype.FastAddProperty("length", 0.0, writable: false, enumerable: false, configurable: false);
			return functionPrototype;
		}

		public void Configure()
		{
			FastAddProperty("constructor", base.Engine.Function, writable: true, enumerable: false, configurable: true);
			FastAddProperty("toString", new ClrFunctionInstance(base.Engine, ToString), writable: true, enumerable: false, configurable: true);
			FastAddProperty("apply", new ClrFunctionInstance(base.Engine, Apply, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("call", new ClrFunctionInstance(base.Engine, CallImpl, 1), writable: true, enumerable: false, configurable: true);
			FastAddProperty("bind", new ClrFunctionInstance(base.Engine, Bind, 1), writable: true, enumerable: false, configurable: true);
		}

		private JsValue Bind(JsValue thisObj, JsValue[] arguments)
		{
			ICallable callable = thisObj.TryCast<ICallable>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError);
			});
			JsValue boundThis = arguments.At(0);
			BindFunctionInstance bindFunctionInstance = new BindFunctionInstance(base.Engine)
			{
				Extensible = true
			};
			bindFunctionInstance.TargetFunction = thisObj;
			bindFunctionInstance.BoundThis = boundThis;
			bindFunctionInstance.BoundArgs = arguments.Skip(1).ToArray();
			bindFunctionInstance.Prototype = base.Engine.Function.PrototypeObject;
			if (callable is FunctionInstance functionInstance)
			{
				double val = TypeConverter.ToNumber(functionInstance.Get("length")) - (double)(arguments.Length - 1);
				bindFunctionInstance.FastAddProperty("length", System.Math.Max(val, 0.0), writable: false, enumerable: false, configurable: false);
			}
			else
			{
				bindFunctionInstance.FastAddProperty("length", 0.0, writable: false, enumerable: false, configurable: false);
			}
			FunctionInstance throwTypeError = base.Engine.Function.ThrowTypeError;
			bindFunctionInstance.DefineOwnProperty("caller", new PropertyDescriptor(throwTypeError, throwTypeError, false, false), throwOnError: false);
			bindFunctionInstance.DefineOwnProperty("arguments", new PropertyDescriptor(throwTypeError, throwTypeError, false, false), throwOnError: false);
			return bindFunctionInstance;
		}

		private JsValue ToString(JsValue thisObj, JsValue[] arguments)
		{
			FunctionInstance functionInstance = thisObj.TryCast<FunctionInstance>();
			if (functionInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError, "Function object expected.");
			}
			return "function() { ... }";
		}

		public JsValue Apply(JsValue thisObject, JsValue[] arguments)
		{
			ICallable callable = thisObject.TryCast<ICallable>();
			JsValue thisObject2 = arguments.At(0);
			JsValue jsValue = arguments.At(1);
			if (callable == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			if (jsValue == Null.Instance || jsValue == Undefined.Instance)
			{
				return callable.Call(thisObject2, Arguments.Empty);
			}
			ObjectInstance objectInstance = jsValue.TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			double num = objectInstance.Get("length").AsNumber();
			uint num2 = TypeConverter.ToUint32(num);
			List<JsValue> list = new List<JsValue>();
			for (int i = 0; i < num2; i++)
			{
				string propertyName = i.ToString();
				JsValue item = objectInstance.Get(propertyName);
				list.Add(item);
			}
			return callable.Call(thisObject2, list.ToArray());
		}

		public JsValue CallImpl(JsValue thisObject, JsValue[] arguments)
		{
			ICallable callable = thisObject.TryCast<ICallable>();
			if (callable == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			return callable.Call(arguments.At(0), (arguments.Length == 0) ? arguments : arguments.Skip(1).ToArray());
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			return Undefined.Instance;
		}
	}
}
