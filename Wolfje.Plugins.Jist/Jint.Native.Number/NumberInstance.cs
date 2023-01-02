using System;
using Jint.Native.Object;
using Jint.Runtime;

namespace Jint.Native.Number
{
	public class NumberInstance : ObjectInstance, IPrimitiveInstance
	{
		private static readonly long NegativeZeroBits = BitConverter.DoubleToInt64Bits(-0.0);

		public override string Class => "Number";

		Types IPrimitiveInstance.Type => Types.Number;

		JsValue IPrimitiveInstance.PrimitiveValue => PrimitiveValue;

		public JsValue PrimitiveValue { get; set; }

		public NumberInstance(Engine engine)
			: base(engine)
		{
		}

		public static bool IsNegativeZero(double x)
		{
			if (x == 0.0)
			{
				return BitConverter.DoubleToInt64Bits(x) == NegativeZeroBits;
			}
			return false;
		}

		public static bool IsPositiveZero(double x)
		{
			if (x == 0.0)
			{
				return BitConverter.DoubleToInt64Bits(x) != NegativeZeroBits;
			}
			return false;
		}
	}
}
