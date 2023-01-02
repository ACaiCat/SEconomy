using System;

namespace Wolfje.Plugins.Jist.Framework
{
	[AttributeUsage(AttributeTargets.Method)]
	public class JavascriptFunctionAttribute : Attribute
	{
		private string[] _functionNames;

		public string[] FunctionNames => _functionNames;

		public JavascriptFunctionAttribute(params string[] names)
		{
			_functionNames = names;
		}
	}
}
