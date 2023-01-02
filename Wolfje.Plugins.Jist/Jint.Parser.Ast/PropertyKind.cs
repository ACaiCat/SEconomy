using System;

namespace Jint.Parser.Ast
{
	[Flags]
	public enum PropertyKind
	{
		Data = 1,
		Get = 2,
		Set = 4
	}
}
