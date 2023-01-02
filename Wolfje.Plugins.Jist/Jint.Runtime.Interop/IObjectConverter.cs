using Jint.Native;

namespace Jint.Runtime.Interop
{
	public interface IObjectConverter
	{
		bool TryConvert(object value, out JsValue result);
	}
}
