using System;
using System.Collections.Generic;
using Jint.Native;
using Jint.Parser.Ast;

namespace Jint.Runtime.Debugger
{
	public class DebugInformation : EventArgs
	{
		public Stack<string> CallStack { get; set; }

		public Statement CurrentStatement { get; set; }

		public Dictionary<string, JsValue> Locals { get; set; }

		public Dictionary<string, JsValue> Globals { get; set; }
	}
}
