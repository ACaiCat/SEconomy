using System;
using Jint.Native;
using Jint.Native.Function;

namespace Jint.Runtime.Interop
{
	public sealed class ClrFunctionInstance : FunctionInstance
	{
		private readonly Func<JsValue, JsValue[], JsValue> _func;

		public ClrFunctionInstance(Engine engine, Func<JsValue, JsValue[], JsValue> func, int length)
			: base(engine, null, null, strict: false)
		{
			_func = func;
			base.Prototype = engine.Function.PrototypeObject;
			FastAddProperty("length", length, writable: false, enumerable: false, configurable: false);
			base.Extensible = true;
		}

		public ClrFunctionInstance(Engine engine, Func<JsValue, JsValue[], JsValue> func)
			: this(engine, func, 0)
		{
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			try
			{
				return _func(thisObject, arguments);
			}
			catch (InvalidCastException)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
		}
	}
}
