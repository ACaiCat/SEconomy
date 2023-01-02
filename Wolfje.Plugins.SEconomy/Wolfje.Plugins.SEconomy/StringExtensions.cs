using System;

namespace Wolfje.Plugins.SEconomy
{
	public static class StringExtensions
	{
		public static bool ContainsInsensitive(this string source, string toCheck)
		{
			return source.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}
}
