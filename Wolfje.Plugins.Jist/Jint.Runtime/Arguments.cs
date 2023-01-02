using Jint.Native;

namespace Jint.Runtime
{
	public static class Arguments
	{
		public static JsValue[] Empty = new JsValue[0];

		public static JsValue[] From(params JsValue[] o)
		{
			return o;
		}

		public static JsValue At(this JsValue[] args, int index, JsValue undefinedValue)
		{
			if (args.Length <= index)
			{
				return undefinedValue;
			}
			return args[index];
		}

		public static JsValue At(this JsValue[] args, int index)
		{
			return args.At(index, Undefined.Instance);
		}
	}
}
