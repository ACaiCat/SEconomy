using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TShockAPI;
using Wolfje.Plugins.SEconomy.Extensions;

namespace Wolfje.Plugins.SEconomy.CmdAliasModule.Extensions
{
	public static class TSPlayerExtensions
	{
		public static bool PermissionlessInvoke(this TSPlayer player, string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			string text2 = text.Remove(0, 1);
			List<string> list = typeof(Commands).CallPrivateMethod<List<string>>(StaticMember: true, "ParseParameters", new object[1] { text2 });
			if (list.Count < 1)
			{
				return false;
			}
			string cmdName = list[0].ToLower();
			list.RemoveAt(0);
			IEnumerable<Command> enumerable = Commands.ChatCommands.Where((Command c) => c.HasAlias(cmdName));
			if (enumerable.Count() == 0)
			{
				if (player.AwaitingResponse.ContainsKey(cmdName))
				{
					Action<object> action = player.AwaitingResponse[cmdName];
					player.AwaitingResponse.Remove(cmdName);
					action(new CommandArgs(text2, player, list));
					return true;
				}
				player.SendErrorMessage("Invalid command entered. Type /help for a list of valid commands.");
				return true;
			}
			foreach (Command item in enumerable)
			{
				if (!item.AllowServer && !player.RealPlayer)
				{
					player.SendErrorMessage("You must use this command in-game.");
					continue;
				}
				if (item.DoLog)
				{
					TShock.Utils.SendLogs(player.Name + " executed: /" + text2 + ".", Color.Red);
				}
				item.RunWithoutPermissions(text2, player, list);
			}
			return true;
		}
	}
}
