using System;

namespace Wolfje.Plugins.Jist
{
	public class ScriptLog
	{
		private static readonly object __lockSyncLock = new object();

		public static void PrintSuccess(string MessageFormat, params object[] args)
		{
			lock (__lockSyncLock)
			{
				int num = 0;
				string text = string.Format(MessageFormat, args);
				ConsoleColor foregroundColor = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Green;
				try
				{
					num = Console.WindowWidth;
					Console.SetCursorPosition(num - text.Length - 3, Console.CursorTop);
				}
				catch
				{
					Console.Write('\t');
					num = 80;
				}
				Console.WriteLine(text);
				Console.ForegroundColor = foregroundColor;
			}
		}

		public static void PrintError(string MessageFormat, params object[] args)
		{
			lock (__lockSyncLock)
			{
				int num = 0;
				string text = string.Format(MessageFormat, args);
				ConsoleColor foregroundColor = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;
				try
				{
					num = Console.WindowWidth;
					Console.SetCursorPosition(num - text.Length - 3, Console.CursorTop);
				}
				catch
				{
					Console.Write('\t');
					num = 80;
				}
				Console.WriteLine(text);
				Console.ForegroundColor = foregroundColor;
			}
		}

		public static void InfoFormat(string ScriptName, string MessageFormat, params object[] args)
		{
			lock (__lockSyncLock)
			{
				ConsoleColor foregroundColor = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("[jist {0}] ", ScriptName);
				Console.ForegroundColor = foregroundColor;
				Console.Write(MessageFormat, args);
			}
		}

		public static void InfoLineFormat(string ScriptName, string MessageFormat, params object[] args)
		{
			lock (__lockSyncLock)
			{
				ConsoleColor foregroundColor = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("[jist {0}] ", ScriptName);
				Console.ForegroundColor = foregroundColor;
				Console.WriteLine(MessageFormat, args);
			}
		}

		public static void ErrorFormat(string ScriptName, string MessageFormat, params object[] args)
		{
			lock (__lockSyncLock)
			{
				ConsoleColor foregroundColor = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("[jist {0} error] ", ScriptName);
				Console.WriteLine(MessageFormat, args);
				Console.ForegroundColor = foregroundColor;
			}
		}
	}
}
