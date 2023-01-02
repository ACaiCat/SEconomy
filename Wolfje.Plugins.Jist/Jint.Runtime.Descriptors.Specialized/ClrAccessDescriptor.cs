using System;
using Jint.Native;
using Jint.Runtime.Interop;

namespace Jint.Runtime.Descriptors.Specialized
{
	public sealed class ClrAccessDescriptor : PropertyDescriptor
	{
		public ClrAccessDescriptor(Engine engine, Func<JsValue, JsValue> get)
			: this(engine, get, null)
		{
		}

		public ClrAccessDescriptor(Engine engine, Func<JsValue, JsValue> get, Action<JsValue, JsValue> set)
			: base(new GetterFunctionInstance(engine, get), (set == null) ? Jint.Native.Undefined.Instance : ((JsValue)new SetterFunctionInstance(engine, set)))
		{
		}
	}
}
