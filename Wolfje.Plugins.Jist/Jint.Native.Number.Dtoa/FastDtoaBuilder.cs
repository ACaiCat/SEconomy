using System;

namespace Jint.Native.Number.Dtoa
{
	public class FastDtoaBuilder
	{
		private readonly char[] _chars = new char[25];

		internal int End;

		internal int Point;

		private bool _formatted;

		private static readonly char[] Digits = new char[10] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

		internal void Append(char c)
		{
			_chars[End++] = c;
		}

		internal void DecreaseLast()
		{
			_chars[End - 1] -= '\u0001';
		}

		public void Reset()
		{
			End = 0;
			_formatted = false;
		}

		public override string ToString()
		{
			return "[chars:" + new string(_chars, 0, End) + ", point:" + Point + "]";
		}

		public string Format()
		{
			if (!_formatted)
			{
				int num = ((_chars[0] == '-') ? 1 : 0);
				int num2 = Point - num;
				if (num2 < -5 || num2 > 21)
				{
					ToExponentialFormat(num, num2);
				}
				else
				{
					ToFixedFormat(num, num2);
				}
				_formatted = true;
			}
			return new string(_chars, 0, End);
		}

		private void ToFixedFormat(int firstDigit, int decPoint)
		{
			if (Point < End)
			{
				if (decPoint > 0)
				{
					System.Array.Copy(_chars, Point, _chars, Point + 1, End - Point);
					_chars[Point] = '.';
					End++;
					return;
				}
				int num = firstDigit + 2 - decPoint;
				System.Array.Copy(_chars, firstDigit, _chars, num, End - firstDigit);
				_chars[firstDigit] = '0';
				_chars[firstDigit + 1] = '.';
				if (decPoint < 0)
				{
					Fill(_chars, firstDigit + 2, num, '0');
				}
				End += 2 - decPoint;
			}
			else if (Point > End)
			{
				Fill(_chars, End, Point, '0');
				End += Point - End;
			}
		}

		private void ToExponentialFormat(int firstDigit, int decPoint)
		{
			if (End - firstDigit > 1)
			{
				int num = firstDigit + 1;
				System.Array.Copy(_chars, num, _chars, num + 1, End - num);
				_chars[num] = '.';
				End++;
			}
			_chars[End++] = 'e';
			char c = '+';
			int num2 = decPoint - 1;
			if (num2 < 0)
			{
				c = '-';
				num2 = -num2;
			}
			_chars[End++] = c;
			int num3 = ((num2 > 99) ? (End + 2) : ((num2 > 9) ? (End + 1) : End));
			End = num3 + 1;
			do
			{
				int num4 = num2 % 10;
				_chars[num3--] = Digits[num4];
				num2 /= 10;
			}
			while (num2 != 0);
		}

		private void Fill<T>(T[] array, int fromIndex, int toIndex, T val)
		{
			for (int i = fromIndex; i < toIndex; i++)
			{
				array[i] = val;
			}
		}
	}
}
