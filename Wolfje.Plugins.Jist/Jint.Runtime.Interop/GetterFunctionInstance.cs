using System;
using Jint.Native;
using Jint.Native.Function;

namespace Jint.Runtime.Interop
{
	public sealed class GetterFunctionInstance : FunctionInstance
	{
		private readonly Func<JsValue, JsValue> _getter;

		public GetterFunctionInstance(Engine engine, Func<JsValue, JsValue> getter)
			: base(engine, null, null, strict: false)
		{
			_getter = getter;
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			return _getter(thisObject);
		}
	}
}
