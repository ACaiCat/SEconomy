using System.Collections.Generic;
using Jint.Parser.Ast;

namespace Jint.Parser
{
	public class FunctionScope : IFunctionScope, IVariableScope
	{
		public IList<FunctionDeclaration> FunctionDeclarations { get; set; }

		public IList<VariableDeclaration> VariableDeclarations { get; set; }

		public FunctionScope()
		{
			FunctionDeclarations = new List<FunctionDeclaration>();
		}
	}
}
