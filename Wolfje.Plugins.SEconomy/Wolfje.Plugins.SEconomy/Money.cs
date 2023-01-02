using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Wolfje.Plugins.SEconomy
{
	public struct Money
	{
		private long _moneyValue;

		private const int ONE_PLATINUM = 1000000;

		private const int ONE_GOLD = 10000;

		private const int ONE_SILVER = 100;

		private const int ONE_COPPER = 1;

		private static readonly Regex moneyRegex = new Regex("(-)?((\\d*)" + SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant4Abbreviation + ")?((\\d*)" + SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant3Abbreviation + ")?((\\d*)" + SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant2Abbreviation + ")?((\\d*)" + SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant1Abbreviation + ")?", RegexOptions.IgnoreCase);

		private static readonly Regex numberRegex = new Regex("(\\d*)", RegexOptions.IgnoreCase);

		public long Platinum
		{
			get
			{
				if (SEconomyPlugin.Instance.Configuration.MoneyConfiguration.UseQuadrantNotation)
				{
					return (long)Math.Floor((decimal)(_moneyValue / 1000000));
				}
				return _moneyValue;
			}
		}

		public long Gold
		{
			get
			{
				if (SEconomyPlugin.Instance.Configuration.MoneyConfiguration.UseQuadrantNotation)
				{
					return (_moneyValue % 1000000 - _moneyValue % 10000) / 10000;
				}
				return _moneyValue;
			}
		}

		public long Silver
		{
			get
			{
				if (SEconomyPlugin.Instance.Configuration.MoneyConfiguration.UseQuadrantNotation)
				{
					return (_moneyValue % 10000 - _moneyValue % 100) / 100;
				}
				return _moneyValue;
			}
		}

		public long Copper
		{
			get
			{
				if (SEconomyPlugin.Instance.Configuration.MoneyConfiguration.UseQuadrantNotation)
				{
					return _moneyValue % 100;
				}
				return _moneyValue;
			}
		}

		public long Value => _moneyValue;

		public static string CurrencyName
		{
			get
			{
				if (!SEconomyPlugin.Instance.Configuration.MoneyConfiguration.UseQuadrantNotation)
				{
					return SEconomyPlugin.Instance.Configuration.MoneyConfiguration.MoneyName.ToLowerInvariant();
				}
				return "";
			}
		}

		public Money(Money money)
		{
			_moneyValue = money._moneyValue;
		}

		public Money(long money)
		{
			_moneyValue = money;
		}

		public Money(uint Platinum, uint Gold, int Silver, int Copper)
		{
			_moneyValue = 0L;
			if (Gold > 99 || Silver > 99 || Copper > 99)
			{
				throw new ArgumentException("提供的金币,银币和铜币的价值不能超过99.");
			}
			_moneyValue += (long)Math.Pow(Platinum, 6.0);
			_moneyValue += (long)Math.Pow(Gold, 4.0);
			_moneyValue += (long)Math.Pow(Silver, 2.0);
			_moneyValue += Copper;
		}

		public static implicit operator Money(long money)
		{
			return new Money(money);
		}

		public static implicit operator long(Money money)
		{
			return money._moneyValue;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			Money money = this;
			if (SEconomyPlugin.Instance.Configuration.MoneyConfiguration.UseQuadrantNotation)
			{
				if ((long)money < 0)
				{
					stringBuilder.Append("-");
					money = (long)money * -1;
				}
				if (money.Platinum > 0)
				{
					stringBuilder.AppendFormat("{0}{1}", money.Platinum, SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant4Abbreviation);
				}
				if (money.Gold > 0)
				{
					stringBuilder.AppendFormat("{0}{1}", money.Gold, SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant3Abbreviation);
				}
				if (money.Silver > 0)
				{
					stringBuilder.AppendFormat("{0}{1}", money.Silver, SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant2Abbreviation);
				}
				if (money.Copper > 0)
				{
					stringBuilder.AppendFormat("{0}{1}", money.Copper, SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant1Abbreviation);
				}
				else if ((long)money == 0L)
				{
					stringBuilder.AppendFormat("{0}{1}", money.Copper, SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant1Abbreviation);
				}
			}
			else
			{
				stringBuilder.AppendFormat("{0} {1}", Value.ToString(SEconomyPlugin.Instance.Configuration.MoneyConfiguration.SingularDisplayFormat, new CultureInfo(SEconomyPlugin.Instance.Configuration.MoneyConfiguration.SingularDisplayCulture)), (Value != 1 || Value != -1) ? SEconomyPlugin.Instance.Configuration.MoneyConfiguration.MoneyNamePlural : SEconomyPlugin.Instance.Configuration.MoneyConfiguration.MoneyName);
			}
			return stringBuilder.ToString();
		}

		public string ToLongString(bool ShowNegativeSign = false)
		{
			StringBuilder stringBuilder = new StringBuilder();
			long location = 0L;
			Interlocked.Exchange(ref location, this);
			if (SEconomyPlugin.Instance.Configuration.MoneyConfiguration.UseQuadrantNotation)
			{
				if (location < 0)
				{
					if (ShowNegativeSign)
					{
						stringBuilder.Append("-");
					}
					Interlocked.Exchange(ref location, location * -1);
				}
				if (((Money)location).Platinum > 0)
				{
					stringBuilder.AppendFormat("{0} {1}", ((Money)location).Platinum, SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant4FullName);
				}
				if (((Money)location).Gold > 0)
				{
					stringBuilder.AppendFormat("{1}{0} {2}", ((Money)location).Gold, (stringBuilder.Length > 0) ? " " : "", SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant3FullName);
				}
				if (((Money)location).Silver > 0)
				{
					stringBuilder.AppendFormat("{1}{0} {2}", ((Money)location).Silver, (stringBuilder.Length > 0) ? " " : "", SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant2FullName);
				}
				if (((Money)location).Copper > 0 || ((Money)location)._moneyValue == 0L)
				{
					stringBuilder.AppendFormat("{1}{0} {2}", ((Money)location).Copper, (stringBuilder.Length > 0) ? " " : "", SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant1FullName);
				}
			}
			else
			{
				stringBuilder.AppendFormat("{0} {1}", Value.ToString(SEconomyPlugin.Instance.Configuration.MoneyConfiguration.SingularDisplayFormat, new CultureInfo(SEconomyPlugin.Instance.Configuration.MoneyConfiguration.SingularDisplayCulture)), (Value == 1 || Value == -1) ? SEconomyPlugin.Instance.Configuration.MoneyConfiguration.MoneyName.ToLowerInvariant() : SEconomyPlugin.Instance.Configuration.MoneyConfiguration.MoneyNamePlural.ToLowerInvariant());
				if (!ShowNegativeSign)
				{
					stringBuilder = stringBuilder.Replace("-", "");
				}
			}
			return stringBuilder.ToString();
		}

		public static bool TryParse(string MoneyRepresentation, out Money Money)
		{
			try
			{
				Money = Parse(MoneyRepresentation);
			}
			catch
			{
				Money = 0L;
				return false;
			}
			return true;
		}

		public static Money Parse(string MoneyRepresentation)
		{
			long result = 0L;
			if (!string.IsNullOrWhiteSpace(MoneyRepresentation) && new Regex(SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant4Abbreviation + "|" + SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant3Abbreviation + "|" + SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant2Abbreviation + "|" + SEconomyPlugin.Instance.Configuration.MoneyConfiguration.Quadrant1Abbreviation).IsMatch(MoneyRepresentation))
			{
				Match match = moneyRegex.Match(MoneyRepresentation);
				long num = 0L;
				long num2 = 0L;
				long num3 = 0L;
				long num4 = 0L;
				string value = "";
				if (!string.IsNullOrWhiteSpace(match.Groups[1].Value))
				{
					value = match.Groups[1].Value;
				}
				if (!string.IsNullOrWhiteSpace(match.Groups[2].Value))
				{
					num = long.Parse(match.Groups[3].Value);
				}
				if (!string.IsNullOrWhiteSpace(match.Groups[4].Value))
				{
					num2 = long.Parse(match.Groups[5].Value);
				}
				if (!string.IsNullOrWhiteSpace(match.Groups[6].Value))
				{
					num3 = long.Parse(match.Groups[7].Value);
				}
				if (!string.IsNullOrWhiteSpace(match.Groups[8].Value))
				{
					num4 = long.Parse(match.Groups[9].Value);
				}
				result += num * 1000000;
				result += num2 * 10000;
				result += num3 * 100;
				result += num4;
				if (!string.IsNullOrWhiteSpace(value))
				{
					result = -result;
				}
			}
			else if (numberRegex.IsMatch(MoneyRepresentation))
			{
				Match match2 = numberRegex.Match(MoneyRepresentation);
				if (match2.Groups.Count > 1)
				{
					long.TryParse(match2.Groups[1].Value, out result);
				}
			}
			return result;
		}
	}
}
