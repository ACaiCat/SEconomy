using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Jint.Native;
using Terraria;
using TShockAPI;
using Wolfje.Plugins.Jist.Framework;

namespace Wolfje.Plugins.Jist.stdlib
{
	public class std : stdlib_base
	{
		protected readonly Random randomGenerator = new Random();

		protected readonly object __rndLock = new object();

		protected readonly Regex csvRegex = new Regex("(?<=^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)");

		public std(JistEngine engine)
			: base(engine)
		{
		}

		[JavascriptFunction(new string[] { "random", "jist_random" })]
		public double Random(double From, double To)
		{
			int minValue = Convert.ToInt32(From);
			int maxValue = Convert.ToInt32(To);
			lock (__rndLock)
			{
				return randomGenerator.Next(minValue, maxValue);
			}
		}

		[JavascriptFunction(new string[] { "jist_repeat" })]
		public void Repeat(int times, JsValue func)
		{
			for (int i = 0; i < times; i++)
			{
				engine.CallFunction(func, null, i);
			}
		}

		[JavascriptFunction(new string[] { "jist_execute" })]
		public void Execute(string file)
		{
		}

		[JavascriptFunction(new string[] { "jist_for_each_item" })]
		public void ForEachItem(JsValue func)
		{
			for (int i = -48; i < 3930; i++)
			{
				Item itemById = TShock.Utils.GetItemById(i);
				if (itemById != null)
				{
					engine.CallFunction(func, null, i, itemById);
				}
			}
		}

		[JavascriptFunction(new string[] { "jist_for_each_player" })]
		public void ForEachOnlinePlayer(JsValue func)
		{
			TSPlayer[] players = TShock.Players;
			TSPlayer[] array = players;
			foreach (TSPlayer tSPlayer in array)
			{
				if (tSPlayer != null)
				{
					engine.CallFunction(func, null, tSPlayer);
				}
			}
		}

		[JavascriptFunction(new string[] { "jist_for_each_command" })]
		public void ForEachComand(JsValue Func)
		{
			var enumerable = from i in Commands.ChatCommands
				let g = TShock.Groups.Where((TShockAPI.Group gr) => gr.HasPermission(i.Permissions.FirstOrDefault()))
				select new
				{
					name = i.Name,
					permissions = string.Join(",", i.Permissions),
					gr = string.Join(" ", g.Select((TShockAPI.Group asd) => asd.Name))
				};
			foreach (var item in enumerable)
			{
				engine.CallFunction(Func, null, item);
			}
		}

		[JavascriptFunction(new string[] { "jist_file_append" })]
		public void FileAppend(string fileName, string text)
		{
			File.AppendAllText(fileName, text + "\r\n");
		}

		[JavascriptFunction(new string[] { "jist_file_delete" })]
		public void FileDelete(string filePath)
		{
			File.Delete(filePath);
		}

		[JavascriptFunction(new string[] { "jist_cwd" })]
		public string CurrentWorkingDirectory()
		{
			return Environment.CurrentDirectory;
		}

		[JavascriptFunction(new string[] { "jist_file_read" })]
		public void FileRead(string filePath, JsValue func)
		{
			int num = 0;
			bool flag = false;
			string[] array;
			string[] array2 = (array = File.ReadAllLines(filePath));
			string[] array3 = array2;
			foreach (string text in array3)
			{
				string text2 = text.Trim();
				if (!string.IsNullOrEmpty(text.Trim()))
				{
					bool flag2 = false;
					try
					{
						engine.CallFunction(func, null, num, text.Trim(), flag2);
					}
					catch
					{
						break;
					}
					if (text.Trim() != text2)
					{
						flag = true;
					}
					if (flag2)
					{
						break;
					}
					num++;
				}
			}
			if (flag && array != null)
			{
				File.WriteAllLines(filePath, array);
			}
		}

		[JavascriptFunction(new string[] { "jist_player_count" })]
		public int OnlinePlayerCount()
		{
			try
			{
				return TShock.Players.Count((TSPlayer i) => i != null);
			}
			catch
			{
				return -1;
			}
		}

		[JavascriptFunction(new string[] { "jist_file_read_lines" })]
		public string[] FileReadLines(string path)
		{
			if (!File.Exists(path))
			{
				return null;
			}
			try
			{
				return File.ReadAllLines(path);
			}
			catch
			{
				return null;
			}
		}

		[JavascriptFunction(new string[] { "jist_parse_csv" })]
		public string[] ReadCSV(string line)
		{
			MatchCollection matchCollection;
			if (string.IsNullOrEmpty(line) || (matchCollection = csvRegex.Matches(line)) == null)
			{
				return null;
			}
			string[] array = new string[matchCollection.Count];
			for (int i = 0; i < array.Length; i++)
			{
				Match match;
				if ((match = matchCollection[i]) != null)
				{
					array[i] = match.Value;
				}
			}
			return array;
		}
	}
}
