namespace Jint.Parser.Ast
{
	public class MemberExpression : Expression
	{
		public Expression Object;

		public Expression Property;

		public bool Computed;
	}
}
