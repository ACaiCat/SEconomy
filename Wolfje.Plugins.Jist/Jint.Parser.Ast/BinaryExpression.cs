using System;

namespace Jint.Parser.Ast
{
	public class BinaryExpression : Expression
	{
		public BinaryOperator Operator;

		public Expression Left;

		public Expression Right;

		public static BinaryOperator ParseBinaryOperator(string op)
		{
			return op switch
			{
				"+" => BinaryOperator.Plus, 
				"-" => BinaryOperator.Minus, 
				"*" => BinaryOperator.Times, 
				"/" => BinaryOperator.Divide, 
				"%" => BinaryOperator.Modulo, 
				"==" => BinaryOperator.Equal, 
				"!=" => BinaryOperator.NotEqual, 
				">" => BinaryOperator.Greater, 
				">=" => BinaryOperator.GreaterOrEqual, 
				"<" => BinaryOperator.Less, 
				"<=" => BinaryOperator.LessOrEqual, 
				"===" => BinaryOperator.StrictlyEqual, 
				"!==" => BinaryOperator.StricltyNotEqual, 
				"&" => BinaryOperator.BitwiseAnd, 
				"|" => BinaryOperator.BitwiseOr, 
				"^" => BinaryOperator.BitwiseXOr, 
				"<<" => BinaryOperator.LeftShift, 
				">>" => BinaryOperator.RightShift, 
				">>>" => BinaryOperator.UnsignedRightShift, 
				"instanceof" => BinaryOperator.InstanceOf, 
				"in" => BinaryOperator.In, 
				_ => throw new ArgumentOutOfRangeException("Invalid binary operator: " + op), 
			};
		}
	}
}
