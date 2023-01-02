using Jint.Native.Object;
using Jint.Runtime.Interop;

namespace Jint.Native.Json
{
	public sealed class JsonInstance : ObjectInstance
	{
		private readonly Engine _engine;

		public override string Class => "JSON";

		private JsonInstance(Engine engine)
			: base(engine)
		{
			_engine = engine;
			base.Extensible = true;
		}

		public static JsonInstance CreateJsonObject(Engine engine)
		{
			JsonInstance jsonInstance = new JsonInstance(engine);
			jsonInstance.Prototype = engine.Object.PrototypeObject;
			return jsonInstance;
		}

		public void Configure()
		{
			FastAddProperty("parse", new ClrFunctionInstance(base.Engine, Parse, 2), writable: true, enumerable: false, configurable: true);
			FastAddProperty("stringify", new ClrFunctionInstance(base.Engine, Stringify, 3), writable: true, enumerable: false, configurable: true);
		}

		public JsValue Parse(JsValue thisObject, JsValue[] arguments)
		{
			JsonParser jsonParser = new JsonParser(_engine);
			return jsonParser.Parse(arguments[0].AsString());
		}

		public JsValue Stringify(JsValue thisObject, JsValue[] arguments)
		{
			JsValue jsValue = Undefined.Instance;
			JsValue jsValue2 = Undefined.Instance;
			JsValue space = Undefined.Instance;
			if (arguments.Length > 2)
			{
				space = arguments[2];
			}
			if (arguments.Length > 1)
			{
				jsValue2 = arguments[1];
			}
			if (arguments.Length != 0)
			{
				jsValue = arguments[0];
			}
			JsonSerializer jsonSerializer = new JsonSerializer(_engine);
			if (jsValue == Undefined.Instance && jsValue2 == Undefined.Instance)
			{
				return Undefined.Instance;
			}
			return jsonSerializer.Serialize(jsValue, jsValue2, space);
		}
	}
}
