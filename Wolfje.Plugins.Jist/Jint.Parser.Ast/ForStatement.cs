namespace Jint.Parser.Ast
{
	public class ForStatement : Statement
	{
		public SyntaxNode Init;

		public Expression Test;

		public Expression Update;

		public Statement Body;
	}
}
