using System.Collections.Generic;
using Jint.Parser.Ast;

namespace Jint.Parser
{
	public interface IFunctionDeclaration : IFunctionScope, IVariableScope
	{
		Identifier Id { get; }

		IEnumerable<Identifier> Parameters { get; }

		Statement Body { get; }

		bool Strict { get; }
	}
}
