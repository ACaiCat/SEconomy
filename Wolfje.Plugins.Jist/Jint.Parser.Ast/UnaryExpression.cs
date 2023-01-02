using System;

namespace Jint.Parser.Ast
{
	public class UnaryExpression : Expression
	{
		public UnaryOperator Operator;

		public Expression Argument;

		public bool Prefix;

		public static UnaryOperator ParseUnaryOperator(string op)
		{
			return op switch
			{
				"+" => UnaryOperator.Plus, 
				"-" => UnaryOperator.Minus, 
				"++" => UnaryOperator.Increment, 
				"--" => UnaryOperator.Decrement, 
				"~" => UnaryOperator.BitwiseNot, 
				"!" => UnaryOperator.LogicalNot, 
				"delete" => UnaryOperator.Delete, 
				"void" => UnaryOperator.Void, 
				"typeof" => UnaryOperator.TypeOf, 
				_ => throw new ArgumentOutOfRangeException("Invalid unary operator: " + op), 
			};
		}
	}
}
