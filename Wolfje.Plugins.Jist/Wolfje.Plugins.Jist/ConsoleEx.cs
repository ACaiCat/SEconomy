using System;
using System.Text;

namespace Wolfje.Plugins.Jist
{
	internal static class ConsoleEx
	{
		private static readonly object __consoleWriteLock = new object();

		public static void WriteBar(PercentChangedEventArgs args)
		{
			StringBuilder stringBuilder = new StringBuilder();
			int num = 0;
			char c = '#';
			char c2 = ' ';
			stringBuilder.Append(" ");
			for (int i = 0; i < 10; i++)
			{
				char value = ((i < args.Label.Length) ? args.Label[i] : ' ');
				stringBuilder.Append(value);
			}
			stringBuilder.Append(" [");
			num = Convert.ToInt32(args.Percent / 100m * 60m);
			for (int j = 0; j < 60; j++)
			{
				stringBuilder.Append((j <= num) ? c : c2);
			}
			stringBuilder.Append("] ");
			stringBuilder.Append(args.Percent + "%");
			lock (__consoleWriteLock)
			{
				Console.Write("\r");
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write(stringBuilder.ToString());
				Console.ResetColor();
			}
		}
	}
}
