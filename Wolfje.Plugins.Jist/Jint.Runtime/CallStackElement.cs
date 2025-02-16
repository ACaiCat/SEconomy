using Jint.Native;
using Jint.Parser.Ast;

namespace Jint.Runtime
{
	public class CallStackElement
	{
		private string _shortDescription;

		public CallExpression CallExpression { get; private set; }

		public JsValue Function { get; private set; }

		public CallStackElement(CallExpression callExpression, JsValue function, string shortDescription)
		{
			_shortDescription = shortDescription;
			CallExpression = callExpression;
			Function = function;
		}

		public override string ToString()
		{
			return _shortDescription;
		}
	}
}
