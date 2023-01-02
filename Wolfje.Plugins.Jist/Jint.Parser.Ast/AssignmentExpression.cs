using System;

namespace Jint.Parser.Ast
{
	public class AssignmentExpression : Expression
	{
		public AssignmentOperator Operator;

		public Expression Left;

		public Expression Right;

		public static AssignmentOperator ParseAssignmentOperator(string op)
		{
			return op switch
			{
				"=" => AssignmentOperator.Assign, 
				"+=" => AssignmentOperator.PlusAssign, 
				"-=" => AssignmentOperator.MinusAssign, 
				"*=" => AssignmentOperator.TimesAssign, 
				"/=" => AssignmentOperator.DivideAssign, 
				"%=" => AssignmentOperator.ModuloAssign, 
				"&=" => AssignmentOperator.BitwiseAndAssign, 
				"|=" => AssignmentOperator.BitwiseOrAssign, 
				"^=" => AssignmentOperator.BitwiseXOrAssign, 
				"<<=" => AssignmentOperator.LeftShiftAssign, 
				">>=" => AssignmentOperator.RightShiftAssign, 
				">>>=" => AssignmentOperator.UnsignedRightShiftAssign, 
				_ => throw new ArgumentOutOfRangeException("Invalid assignment operator: " + op), 
			};
		}
	}
}
