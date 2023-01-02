using System.Collections.Generic;
using Jint.Parser.Ast;

namespace Jint.Parser
{
	public interface IVariableScope
	{
		IList<VariableDeclaration> VariableDeclarations { get; set; }
	}
}
