using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TShockAPI;

namespace Wolfje.Plugins.Jist.Extensions
{
	public static class TSPlayerExtensions
	{
		public static void SendMessageFormat(this TSPlayer Player, Color Colour, string MessageFormat, params object[] args)
		{
			Player.SendMessage(string.Format(MessageFormat, args), Colour);
		}

		public static bool PermissionlessInvoke(this TSPlayer player, string text, bool silent = false)
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
					Action<CommandArgs> action = player.AwaitingResponse[cmdName];
					player.AwaitingResponse.Remove(cmdName);
					action(new CommandArgs(text2, player, list));
					return true;
				}
				player.SendErrorMessage("�����������Ч.����/help��ȡ��Ч�����б�.");
				return true;
			}
			foreach (Command item in enumerable)
			{
				if (!item.AllowServer && !player.RealPlayer)
				{
					player.SendErrorMessage("���������Ϸ��ʹ���������.");
					continue;
				}
				if (item.DoLog && !silent)
				{
					TShock.Utils.SendLogs(player.Name + " ִ��: /" + text2 + ".", Color.Red);
				}
				item.RunWithoutPermissions(text2, player, list);
			}
			return true;
		}
	}
}
