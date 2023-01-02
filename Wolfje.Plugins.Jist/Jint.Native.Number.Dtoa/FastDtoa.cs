#define DEBUG
using System;
using System.Diagnostics;

namespace Jint.Native.Number.Dtoa
{
	public class FastDtoa
	{
		public const int KFastDtoaMaximalLength = 17;

		private const int MinimalTargetExponent = -60;

		private const int MaximalTargetExponent = -32;

		private const int KTen4 = 10000;

		private const int KTen5 = 100000;

		private const int KTen6 = 1000000;

		private const int KTen7 = 10000000;

		private const int KTen8 = 100000000;

		private const int KTen9 = 1000000000;

		private static bool RoundWeed(FastDtoaBuilder buffer, long distanceTooHighW, long unsafeInterval, long rest, long tenKappa, long unit)
		{
			long num = distanceTooHighW - unit;
			long num2 = distanceTooHighW + unit;
			while (rest < num && unsafeInterval - rest >= tenKappa && (rest + tenKappa < num || num - rest >= rest + tenKappa - num))
			{
				buffer.DecreaseLast();
				rest += tenKappa;
			}
			if (rest < num2 && unsafeInterval - rest >= tenKappa && (rest + tenKappa < num2 || num2 - rest > rest + tenKappa - num2))
			{
				return false;
			}
			if (2 * unit <= rest)
			{
				return rest <= unsafeInterval - 4 * unit;
			}
			return false;
		}

		private static long BiggestPowerTen(int number, int numberBits)
		{
			int num;
			int num2;
			switch (numberBits)
			{
			case 30:
			case 31:
			case 32:
				if (1000000000 <= number)
				{
					num = 1000000000;
					num2 = 9;
					break;
				}
				goto case 27;
			case 27:
			case 28:
			case 29:
				if (100000000 <= number)
				{
					num = 100000000;
					num2 = 8;
					break;
				}
				goto case 24;
			case 24:
			case 25:
			case 26:
				if (10000000 <= number)
				{
					num = 10000000;
					num2 = 7;
					break;
				}
				goto case 20;
			case 20:
			case 21:
			case 22:
			case 23:
				if (1000000 <= number)
				{
					num = 1000000;
					num2 = 6;
					break;
				}
				goto case 17;
			case 17:
			case 18:
			case 19:
				if (100000 <= number)
				{
					num = 100000;
					num2 = 5;
					break;
				}
				goto case 14;
			case 14:
			case 15:
			case 16:
				if (10000 <= number)
				{
					num = 10000;
					num2 = 4;
					break;
				}
				goto case 10;
			case 10:
			case 11:
			case 12:
			case 13:
				if (1000 <= number)
				{
					num = 1000;
					num2 = 3;
					break;
				}
				goto case 7;
			case 7:
			case 8:
			case 9:
				if (100 <= number)
				{
					num = 100;
					num2 = 2;
					break;
				}
				goto case 4;
			case 4:
			case 5:
			case 6:
				if (10 <= number)
				{
					num = 10;
					num2 = 1;
					break;
				}
				goto case 1;
			case 1:
			case 2:
			case 3:
				if (1 <= number)
				{
					num = 1;
					num2 = 0;
					break;
				}
				goto case 0;
			case 0:
				num = 0;
				num2 = -1;
				break;
			default:
				num = 0;
				num2 = 0;
				break;
			}
			return ((long)num << 32) | (0xFFFFFFFFu & num2);
		}

		private static bool DigitGen(DiyFp low, DiyFp w, DiyFp high, FastDtoaBuilder buffer, int mk)
		{
			long num = 1L;
			DiyFp b = new DiyFp(low.F - num, low.E);
			DiyFp diyFp = new DiyFp(high.F + num, high.E);
			DiyFp diyFp2 = DiyFp.Minus(diyFp, b);
			DiyFp diyFp3 = new DiyFp(1L << -w.E, w.E);
			int num2 = (int)(diyFp.F.UnsignedShift(-diyFp3.E) & 0xFFFFFFFFu);
			long num3 = diyFp.F & (diyFp3.F - 1);
			long num4 = BiggestPowerTen(num2, 64 - -diyFp3.E);
			int num5 = (int)(num4.UnsignedShift(32) & 0xFFFFFFFFu);
			int num6 = (int)(num4 & 0xFFFFFFFFu);
			int num7 = num6 + 1;
			while (num7 > 0)
			{
				int num8 = num2 / num5;
				buffer.Append((char)(48 + num8));
				num2 %= num5;
				num7--;
				long num9 = ((long)num2 << -diyFp3.E) + num3;
				if (num9 < diyFp2.F)
				{
					buffer.Point = buffer.End - mk + num7;
					return RoundWeed(buffer, DiyFp.Minus(diyFp, w).F, diyFp2.F, num9, (long)num5 << -diyFp3.E, num);
				}
				num5 /= 10;
			}
			do
			{
				num3 *= 5;
				num *= 5;
				diyFp2.F *= 5L;
				diyFp2.E++;
				diyFp3.F = diyFp3.F.UnsignedShift(1);
				diyFp3.E++;
				int num10 = (int)(num3.UnsignedShift(-diyFp3.E) & 0xFFFFFFFFu);
				buffer.Append((char)(48 + num10));
				num3 &= diyFp3.F - 1;
				num7--;
			}
			while (num3 >= diyFp2.F);
			buffer.Point = buffer.End - mk + num7;
			return RoundWeed(buffer, DiyFp.Minus(diyFp, w).F * num, diyFp2.F, num3, diyFp3.F, num);
		}

		private static bool Grisu3(double v, FastDtoaBuilder buffer)
		{
			long d = BitConverter.DoubleToInt64Bits(v);
			DiyFp diyFp = DoubleHelper.AsNormalizedDiyFp(d);
			DiyFp diyFp2 = new DiyFp();
			DiyFp diyFp3 = new DiyFp();
			DoubleHelper.NormalizedBoundaries(d, diyFp2, diyFp3);
			Debug.Assert(diyFp3.E == diyFp.E);
			DiyFp diyFp4 = new DiyFp();
			int cachedPower = CachedPowers.GetCachedPower(diyFp.E + 64, -60, -32, diyFp4);
			Debug.Assert(-60 <= diyFp.E + diyFp4.E + 64 && -32 >= diyFp.E + diyFp4.E + 64);
			DiyFp diyFp5 = DiyFp.Times(diyFp, diyFp4);
			Debug.Assert(diyFp5.E == diyFp3.E + diyFp4.E + 64);
			DiyFp low = DiyFp.Times(diyFp2, diyFp4);
			DiyFp high = DiyFp.Times(diyFp3, diyFp4);
			return DigitGen(low, diyFp5, high, buffer, cachedPower);
		}

		public static bool Dtoa(double v, FastDtoaBuilder buffer)
		{
			Debug.Assert(v > 0.0);
			Debug.Assert(!double.IsNaN(v));
			Debug.Assert(!double.IsInfinity(v));
			return Grisu3(v, buffer);
		}

		public static string NumberToString(double v)
		{
			FastDtoaBuilder fastDtoaBuilder = new FastDtoaBuilder();
			if (!NumberToString(v, fastDtoaBuilder))
			{
				return null;
			}
			return fastDtoaBuilder.Format();
		}

		public static bool NumberToString(double v, FastDtoaBuilder buffer)
		{
			buffer.Reset();
			if (v < 0.0)
			{
				buffer.Append('-');
				v = 0.0 - v;
			}
			return Dtoa(v, buffer);
		}
	}
}
