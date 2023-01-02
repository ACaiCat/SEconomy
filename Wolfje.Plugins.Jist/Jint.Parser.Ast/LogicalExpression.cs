using System;

namespace Jint.Parser.Ast
{
	public class LogicalExpression : Expression
	{
		public LogicalOperator Operator;

		public Expression Left;

		public Expression Right;

		public static LogicalOperator ParseLogicalOperator(string op)
		{
			if (!(op == "&&"))
			{
				if (op == "||")
				{
					return LogicalOperator.LogicalOr;
				}
				throw new ArgumentOutOfRangeException("Invalid binary operator: " + op);
			}
			return LogicalOperator.LogicalAnd;
		}
	}
}
