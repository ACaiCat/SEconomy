#define DEBUG
using System.Diagnostics;

namespace Jint.Native.Number.Dtoa
{
	public class DoubleHelper
	{
		private const long KExponentMask = 9218868437227405312L;

		private const long KSignificandMask = 4503599627370495L;

		private const long KHiddenBit = 4503599627370496L;

		private const int KSignificandSize = 52;

		private const int KExponentBias = 1075;

		private const int KDenormalExponent = -1074;

		private static DiyFp AsDiyFp(long d64)
		{
			Debug.Assert(!IsSpecial(d64));
			return new DiyFp(Significand(d64), Exponent(d64));
		}

		internal static DiyFp AsNormalizedDiyFp(long d64)
		{
			long num = Significand(d64);
			int num2 = Exponent(d64);
			Debug.Assert(num != 0);
			while ((num & 0x10000000000000L) == 0L)
			{
				num <<= 1;
				num2--;
			}
			num <<= 11;
			num2 -= 11;
			return new DiyFp(num, num2);
		}

		private static int Exponent(long d64)
		{
			if (IsDenormal(d64))
			{
				return -1074;
			}
			int num = (int)((d64 & 0x7FF0000000000000L).UnsignedShift(52) & 0xFFFFFFFFu);
			return num - 1075;
		}

		private static long Significand(long d64)
		{
			long num = d64 & 0xFFFFFFFFFFFFFL;
			if (!IsDenormal(d64))
			{
				return num + 4503599627370496L;
			}
			return num;
		}

		private static bool IsDenormal(long d64)
		{
			return (d64 & 0x7FF0000000000000L) == 0;
		}

		private static bool IsSpecial(long d64)
		{
			return (d64 & 0x7FF0000000000000L) == 9218868437227405312L;
		}

		internal static void NormalizedBoundaries(long d64, DiyFp mMinus, DiyFp mPlus)
		{
			DiyFp diyFp = AsDiyFp(d64);
			bool flag = diyFp.F == 4503599627370496L;
			mPlus.F = (diyFp.F << 1) + 1;
			mPlus.E = diyFp.E - 1;
			mPlus.Normalize();
			if (flag && diyFp.E != -1074)
			{
				mMinus.F = (diyFp.F << 2) - 1;
				mMinus.E = diyFp.E - 2;
			}
			else
			{
				mMinus.F = (diyFp.F << 1) - 1;
				mMinus.E = diyFp.E - 1;
			}
			mMinus.F <<= mMinus.E - mPlus.E;
			mMinus.E = mPlus.E;
		}
	}
}
