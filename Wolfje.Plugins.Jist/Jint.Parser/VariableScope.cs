using System.Collections.Generic;
using Jint.Parser.Ast;

namespace Jint.Parser
{
	public class VariableScope : IVariableScope
	{
		public IList<VariableDeclaration> VariableDeclarations { get; set; }

		public VariableScope()
		{
			VariableDeclarations = new List<VariableDeclaration>();
		}
	}
}
