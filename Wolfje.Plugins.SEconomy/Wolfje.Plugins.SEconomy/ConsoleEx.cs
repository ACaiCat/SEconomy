using System;
using System.Text;
using Wolfje.Plugins.SEconomy.Journal;

namespace Wolfje.Plugins.SEconomy
{
	public static class ConsoleEx
	{
		private static readonly object __consoleWriteLock = new object();

		public static void WriteBar(JournalLoadingPercentChangedEventArgs args)
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
			num = Convert.ToInt32((decimal)args.Percent / 100m * 60m);
			for (int j = 0; j < 60; j++)
			{
				stringBuilder.Append((j <= num) ? c : c2);
			}
			stringBuilder.Append("] ");
			stringBuilder.Append(args.Percent + "%");
			lock (__consoleWriteLock)
			{
				Console.Write("\r");
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(stringBuilder.ToString());
				Console.ResetColor();
			}
		}

		public static void WriteColour(ConsoleColor colour, string MessageFormat, params object[] args)
		{
			lock (__consoleWriteLock)
			{
				string value = string.Format(MessageFormat, args);
				try
				{
					ConsoleColor foregroundColor = Console.ForegroundColor;
					Console.ForegroundColor = colour;
					Console.Write(value);
					Console.ForegroundColor = foregroundColor;
				}
				catch
				{
					Console.Write(value);
				}
			}
		}

		public static void WriteLineColour(ConsoleColor colour, string MessageFormat, params object[] args)
		{
			lock (__consoleWriteLock)
			{
				string value = string.Format(MessageFormat, args);
				try
				{
					ConsoleColor foregroundColor = Console.ForegroundColor;
					Console.ForegroundColor = colour;
					Console.WriteLine(value);
					Console.ForegroundColor = foregroundColor;
				}
				catch
				{
					Console.WriteLine(value);
				}
			}
		}

		public static void WriteAtEnd(int Padding, ConsoleColor Colour, string MessageFormat, params object[] args)
		{
			lock (__consoleWriteLock)
			{
				string text = string.Format(MessageFormat, args);
				try
				{
					ConsoleColor foregroundColor = Console.ForegroundColor;
					Console.ForegroundColor = Colour;
					Console.SetCursorPosition(Console.WindowWidth - text.Length - Padding, Console.CursorTop);
					Console.Write(text);
					Console.ForegroundColor = foregroundColor;
				}
				catch
				{
					Console.Write(text);
				}
			}
		}
	}
}
