using System.Collections.Generic;
using Jint.Parser.Ast;

namespace Jint.Parser
{
	public interface IFunctionScope : IVariableScope
	{
		IList<FunctionDeclaration> FunctionDeclarations { get; set; }
	}
}
