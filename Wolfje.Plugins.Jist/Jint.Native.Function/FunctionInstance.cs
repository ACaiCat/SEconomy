using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Environments;

namespace Jint.Native.Function
{
	public abstract class FunctionInstance : ObjectInstance, ICallable
	{
		private readonly Engine _engine;

		public LexicalEnvironment Scope { get; private set; }

		public string[] FormalParameters { get; private set; }

		public bool Strict { get; private set; }

		public override string Class => "Function";

		protected FunctionInstance(Engine engine, string[] parameters, LexicalEnvironment scope, bool strict)
			: base(engine)
		{
			_engine = engine;
			FormalParameters = parameters;
			Scope = scope;
			Strict = strict;
		}

		public abstract JsValue Call(JsValue thisObject, JsValue[] arguments);

		public virtual bool HasInstance(JsValue v)
		{
			ObjectInstance objectInstance = v.TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				return false;
			}
			JsValue jsValue = Get("prototype");
			if (!jsValue.IsObject())
			{
				throw new JavaScriptException(_engine.TypeError, "Function has non-object prototype '" + TypeConverter.ToString(jsValue) + "' in instanceof check");
			}
			ObjectInstance objectInstance2 = jsValue.AsObject();
			if (objectInstance2 == null)
			{
				throw new JavaScriptException(_engine.TypeError);
			}
			do
			{
				objectInstance = objectInstance.Prototype;
				if (objectInstance == null)
				{
					return false;
				}
			}
			while (objectInstance != objectInstance2);
			return true;
		}

		public override JsValue Get(string propertyName)
		{
			JsValue result = base.Get(propertyName);
			FunctionInstance functionInstance = result.As<FunctionInstance>();
			if (propertyName == "caller" && functionInstance != null && functionInstance.Strict)
			{
				throw new JavaScriptException(_engine.TypeError);
			}
			return result;
		}
	}
}
