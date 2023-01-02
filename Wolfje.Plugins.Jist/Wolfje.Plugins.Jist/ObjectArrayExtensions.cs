using Jint;
using Jint.Native;

namespace Wolfje.Plugins.Jist
{
	public static class ObjectArrayExtensions
	{
		public static JsValue[] ToJsValueArray(this object[] args, Engine jsEngine)
		{
			JsValue[] array = null;
			if (args == null)
			{
				return new JsValue[1] { JsValue.Undefined };
			}
			array = new JsValue[args.Length];
			for (int i = 0; i < args.Length; i++)
			{
				array[i] = JsValue.FromObject(jsEngine, args[i]);
			}
			return array;
		}
	}
}
