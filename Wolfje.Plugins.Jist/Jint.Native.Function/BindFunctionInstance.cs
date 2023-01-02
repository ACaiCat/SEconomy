using System.Linq;
using Jint.Native.Object;
using Jint.Runtime;

namespace Jint.Native.Function
{
	public class BindFunctionInstance : FunctionInstance, IConstructor
	{
		public JsValue TargetFunction { get; set; }

		public JsValue BoundThis { get; set; }

		public JsValue[] BoundArgs { get; set; }

		public BindFunctionInstance(Engine engine)
			: base(engine, new string[0], null, strict: false)
		{
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			FunctionInstance functionInstance = TargetFunction.TryCast<FunctionInstance>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError);
			});
			return functionInstance.Call(BoundThis, BoundArgs.Union(arguments).ToArray());
		}

		public ObjectInstance Construct(JsValue[] arguments)
		{
			IConstructor constructor = TargetFunction.TryCast<IConstructor>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError);
			});
			return constructor.Construct(BoundArgs.Union(arguments).ToArray());
		}

		public override bool HasInstance(JsValue v)
		{
			FunctionInstance functionInstance = TargetFunction.TryCast<FunctionInstance>(delegate
			{
				throw new JavaScriptException(base.Engine.TypeError);
			});
			return functionInstance.HasInstance(v);
		}
	}
}
