namespace Jint.Parser.Ast
{
	public class Property : Expression
	{
		public PropertyKind Kind;

		public IPropertyKeyExpression Key;

		public Expression Value;
	}
}
