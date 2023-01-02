using System.Collections.Generic;

namespace Jint.Parser.Ast
{
	public class FunctionDeclaration : Statement, IFunctionDeclaration, IFunctionScope, IVariableScope
	{
		public IEnumerable<Expression> Defaults;

		public SyntaxNode Rest;

		public bool Generator;

		public bool Expression;

		public Identifier Id { get; set; }

		public IEnumerable<Identifier> Parameters { get; set; }

		public Statement Body { get; set; }

		public bool Strict { get; set; }

		public IList<VariableDeclaration> VariableDeclarations { get; set; }

		public IList<FunctionDeclaration> FunctionDeclarations { get; set; }

		public FunctionDeclaration()
		{
			VariableDeclarations = new List<VariableDeclaration>();
		}
	}
}
