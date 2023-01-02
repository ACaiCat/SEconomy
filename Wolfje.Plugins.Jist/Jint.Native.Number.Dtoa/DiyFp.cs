#define DEBUG
using System.Diagnostics;

namespace Jint.Native.Number.Dtoa
{
	internal class DiyFp
	{
		internal const int KSignificandSize = 64;

		private const ulong KUint64MSB = 9223372036854775808uL;

		public long F { get; set; }

		public int E { get; set; }

		internal DiyFp()
		{
			F = 0L;
			E = 0;
		}

		internal DiyFp(long f, int e)
		{
			F = f;
			E = e;
		}

		private static bool Uint64Gte(long a, long b)
		{
			if (a != b)
			{
				return (a > b) ^ (a < 0) ^ (b < 0);
			}
			return true;
		}

		private void Subtract(DiyFp other)
		{
			Debug.Assert(E == other.E);
			Debug.Assert(Uint64Gte(F, other.F));
			F -= other.F;
		}

		internal static DiyFp Minus(DiyFp a, DiyFp b)
		{
			DiyFp diyFp = new DiyFp(a.F, a.E);
			diyFp.Subtract(b);
			return diyFp;
		}

		private void Multiply(DiyFp other)
		{
			long num = F.UnsignedShift(32);
			long num2 = F & 0xFFFFFFFFu;
			long num3 = other.F.UnsignedShift(32);
			long num4 = other.F & 0xFFFFFFFFu;
			long num5 = num * num3;
			long num6 = num2 * num3;
			long num7 = num * num4;
			long l = num2 * num4;
			long num8 = l.UnsignedShift(32) + (num7 & 0xFFFFFFFFu) + (num6 & 0xFFFFFFFFu);
			num8 += 2147483648u;
			long f = num5 + num7.UnsignedShift(32) + num6.UnsignedShift(32) + num8.UnsignedShift(32);
			E += other.E + 64;
			F = f;
		}

		internal static DiyFp Times(DiyFp a, DiyFp b)
		{
			DiyFp diyFp = new DiyFp(a.F, a.E);
			diyFp.Multiply(b);
			return diyFp;
		}

		internal void Normalize()
		{
			long num = F;
			int num2 = E;
			while ((num & -18014398509481984L) == 0L)
			{
				num <<= 10;
				num2 -= 10;
			}
			while ((num & long.MinValue) == 0L)
			{
				num <<= 1;
				num2--;
			}
			F = num;
			E = num2;
		}

		public override string ToString()
		{
			return "[DiyFp f:" + F + ", e:" + E + "]";
		}
	}
}
