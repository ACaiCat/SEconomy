using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using Jint.Native;
using Microsoft.Xna.Framework;
using Org.BouncyCastle.Utilities.IO;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using Wolfje.Plugins.Jist.Extensions;
using Wolfje.Plugins.Jist.Framework;

namespace Wolfje.Plugins.Jist.stdlib
{
	public class tshock : stdlib_base
	{
		protected readonly Regex htmlColourRegex = new("#([0-9a-f][0-9a-f])([0-9a-f][0-9a-f])([0-9a-f][0-9a-f])", RegexOptions.IgnoreCase);

		protected readonly Regex htmlColourRegexShort = new("#([0-9a-f])([0-9a-f])([0-9a-f])", RegexOptions.IgnoreCase);

		protected readonly Regex rgbColourRegex = new("((\\d*),(\\d*),(\\d*))", RegexOptions.IgnoreCase);

		public tshock(JistEngine engine)
			: base(engine)
		{
			base.Provides = "tshock";
		}

		[JavascriptFunction(new string[] { "tshock_sql_query" })]
		public void SQLQuery(string query, object[] parameters, JsValue func)
		{
			using QueryResult queryResult = TShock.DB.QueryReader(query, parameters ?? new object[0]);
			while (queryResult.Read())
			{
				dynamic val = new ExpandoObject();
				for (int i = 0; i < queryResult.Reader.FieldCount; i++)
				{
					(val as IDictionary<string, object>).Add(queryResult.Reader.GetName(i).Replace(' ', '_'), queryResult.Reader.GetValue(i));
				}
				engine.CallFunction(func, queryResult, val);
			}
		}

		[JavascriptFunction(new string[] { "tshock_sql_execute" })]
		public int SQLQuery(string query, object[] parameters)
		{
			return TShock.DB.Query(query, parameters ?? new object[0]);
		}

		[JavascriptFunction(new string[] { "tshock_get_region" })]
		public Region GetRegion(object region)
		{
			if (region == null)
			{
				return null;
			}
			if (region is Region)
			{
				return region as Region;
			}
			if (region is string)
			{
				try
				{
					return TShock.Regions.GetRegionByName(region as string);
				}
				catch
				{
					return null;
				}
			}
			return null;
		}

		[JavascriptFunction(new string[] { "tshock_player_regions" })]
		public Region[] PlayerInRegions(object PlayerRef)
		{
			List<Region> list = new();
			TSPlayer player;
			if ((player = GetPlayer(PlayerRef)) == null)
			{
				return null;
			}
			foreach (Region item in TShock.Regions.ListAllRegions(Main.worldID.ToString()))
			{
				if (IsPlayerInRegion(player, item))
				{
					list.Add(item);
				}
			}
			if (list.Count != 0)
			{
				return list.ToArray();
			}
			return null;
		}

		[JavascriptFunction(new string[] { "tshock_player_in_region" })]
		public bool IsPlayerInRegion(object playerRef, object regionRef)
		{
			TSPlayer player;
			Region region;
			if (playerRef == null || regionRef == null || (player = GetPlayer(playerRef)) == null || (region = GetRegion(regionRef)) == null)
			{
				return false;
			}
			return region.InArea(player.TileX, player.TileY);
		}

		[JavascriptFunction(new string[] { "get_player", "tshock_get_player" })]
		public TSPlayer GetPlayer(object PlayerRef)
		{
			if (PlayerRef == null)
			{
				return null;
			}
			if (PlayerRef is TSPlayer)
			{
				return PlayerRef as TSPlayer;
			}
			if (PlayerRef is string)
			{
				string playerString = PlayerRef as string;
				if (playerString.Equals("server", StringComparison.CurrentCultureIgnoreCase))
				{
					return TSPlayer.Server;
				}
				TSPlayer result;
				if ((result = TShock.Players.FirstOrDefault((TSPlayer i) => i != null && i.Name == playerString.ToString())) != null)
				{
					return result;
				}
				return TShock.Players.FirstOrDefault((TSPlayer i) => i != null && i.Account.Name == PlayerRef.ToString());
			}
			return null;
		}

		[JavascriptFunction(new string[] { "tshock_has_permission" })]
		public bool PlayerHasPermission(object PlayerRef, object GroupName)
		{
			TSPlayer tSPlayer = null;
			if ((tSPlayer = GetPlayer(PlayerRef)) == null || GroupName == null)
			{
				return false;
			}
			return tSPlayer.Group.HasPermission(GroupName.ToString());
		}

		[JavascriptFunction(new string[] { "group_exists", "tshock_group_exists" })]
		public bool TShockGroupExists(object GroupName)
		{
			return TShock.Groups.Count((TShockAPI.Group i) => i.Name.Equals(GroupName.ToString(), StringComparison.CurrentCultureIgnoreCase)) > 0;
		}

		[JavascriptFunction(new string[] { "tshock_group" })]
		public TShockAPI.Group TShockGroup(object Group)
		{
			TShockAPI.Group group = null;
			if (Group == null)
			{
				return null;
			}
			return TShock.Groups.FirstOrDefault((TShockAPI.Group i) => i.Name.Equals(Group.ToString(), StringComparison.CurrentCultureIgnoreCase));
		}

		[JavascriptFunction(new string[] { "execute_command", "tshock_exec" })]
		public bool ExecuteCommand(object Player, object Command)
		{
			TSPlayer tSPlayer = null;
			string text = "";
			if ((tSPlayer = GetPlayer(Player)) == null)
			{
				return false;
			}
			try
			{
				if (Command is List<string>)
				{
					List<string> source = Command as List<string>;
					foreach (string item in source.Skip(1))
					{
						text = text + " " + item;
					}
				}
				else if (Command is string)
				{
					text = Command.ToString();
				}
				if (string.IsNullOrEmpty(text = text.Trim()))
				{
					return false;
				}
				tSPlayer.PermissionlessInvoke(text);
				return true;
			}
			catch (Exception)
			{
				ScriptLog.ErrorFormat("tshock_exec", "The command \"{0}\" failed.", text.Trim());
				return false;
			}
		}

		[JavascriptFunction(new string[] { "tshock_exec_silent" })]
		public bool ExecuteCommandSilent(object Player, object Command)
		{
			TSPlayer tSPlayer = null;
			string text = "";
			if ((tSPlayer = GetPlayer(Player)) == null)
			{
				return false;
			}
			try
			{
				if (Command is List<string>)
				{
					List<string> source = Command as List<string>;
					foreach (string item in source.Skip(1))
					{
						text = text + " " + item;
					}
				}
				else if (Command is string)
				{
					text = Command.ToString();
				}
				if (string.IsNullOrEmpty(text = text.Trim()))
				{
					return false;
				}
				tSPlayer.PermissionlessInvoke(text, silent: true);
				return true;
			}
			catch (Exception)
			{
				ScriptLog.ErrorFormat("tshock_exec_silent", "The command \"{0}\" failed.", text.Trim());
				return false;
			}
		}

		[JavascriptFunction(new string[] { "change_group", "tshock_change_group" })]
		public bool ChangeGroup(object Player, object Group)
		{
			TSPlayer tSPlayer = null;
			UserAccount userAccount = new();
			string text = "";
			if ((tSPlayer = GetPlayer(Player)) == null)
			{
				return false;
			}
			if (Group is string)
			{
				text = Group as string;
			}
			else if (Group is TShockAPI.Group)
			{
				text = (Group as TShockAPI.Group).Name;
			}
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			try
			{
				userAccount.Name = tSPlayer.Account.Name;
				TShock.UserAccounts.SetUserGroup(userAccount, text);
			}
			catch (Exception ex)
			{
				ScriptLog.ErrorFormat("tshock_change_group", "Group change failed: {0}", ex.Message);
				return false;
			}
			return true;
		}

		[JavascriptFunction(new string[] { "msg", "tshock_msg" })]
		public void Message(object PlayerRef, object Message)
		{
			string text = null;
			TSPlayer player;
			if ((player = GetPlayer(PlayerRef)) != null)
			{
				text = Message.ToString();
				if (!string.IsNullOrEmpty(text))
				{
					player.SendInfoMessage("{0}", Message);
				}
			}
		}

		[JavascriptFunction(new string[] { "msg_colour", "tshock_msg_colour" })]
		public void MessageWithColour(object Colour, object Player, object Message)
		{
			TSPlayer tSPlayer = null;
			string value = Message.ToString();
			Color colour = ParseColour(Colour);
			if ((tSPlayer = GetPlayer(Player)) != null && !string.IsNullOrEmpty(value))
			{
				tSPlayer.SendMessageFormat(colour, "{0}", Message);
			}
		}

		[JavascriptFunction(new string[] { "broadcast_colour", "tshock_broadcast_colour" })]
		public void BroadcastWithColour(object Colour, object Message)
		{
			Color color = ParseColour(Colour);
			if (Message != null)
			{
				TShock.Utils.Broadcast(Message.ToString(), color);
			}
		}

		[JavascriptFunction(new string[] { "broadcast", "tshock_broadcast" })]
		public void Broadcast(object Message)
		{
			BroadcastWithColour("#f00", Message);
		}

		[JavascriptFunction(new string[] { "tshock_server" })]
		public TSPlayer ServerPlayer()
		{
			return TSPlayer.Server;
		}

		[JavascriptFunction(new string[] { "tshock_create_npc" })]
		public KeyValuePair<int, NPC> CreateNPC(int x, int y, int type)
		{
			int num = NPC.NewNPC(new Terraria.DataStructures.EntitySource_DebugCommand(), x, y, type);
			NPC nPC;
			if ((nPC = Main.npc.ElementAtOrDefault(num)) == null)
			{
				return new KeyValuePair<int, NPC>(-1, null);
			}
			Main.npc[num].SetDefaults(nPC.type);
			return new KeyValuePair<int, NPC>(num, nPC);
		}

		[JavascriptFunction(new string[] { "tshock_clear_tile_in_range" })]
		public Point ClearTileInRange(int x, int y, int rx, int ry)
		{
			Point result = default(Point);
			TShock.Utils.GetRandomClearTileWithInRange(x, y, rx, ry, out result.X, out result.Y);
			return result;
		}

		[JavascriptFunction(new string[] { "tshock_set_team" })]
		public void SetTeam(object player, int team)
		{
			TSPlayer player2 = GetPlayer(player);
			player2.SetTeam(team);
		}

		[JavascriptFunction(new string[] { "tshock_warp_find" })]
		public Warp FindWarp(string warp)
		{
			return TShock.Warps.Find(warp);
		}

		[JavascriptFunction(new string[] { "tshock_teleport_player" })]
		public void WarpPlayer(object player, float x, float y)
		{
			TSPlayer player2 = GetPlayer(player);
			player2.Teleport(x, y, 1);
		}

		[JavascriptFunction(new string[] { "tshock_warp_player" })]
		public void WarpPlayer(object player, Warp warp)
		{
			TSPlayer player2 = GetPlayer(player);
			player2.Teleport(warp.Position.X * 16, warp.Position.Y * 16, 1);
		}

		protected Color ParseColour(object colour)
		{
			Color result = Color.Yellow;
			if (colour != null)
			{
				if (colour is Color)
				{
					result = (Color)colour;
				}
				else if (colour is string)
				{
					int result2 = 0;
					int result3 = 0;
					int result4 = 0;
					string input = colour as string;
					if (rgbColourRegex.IsMatch(input))
					{
						Match match = rgbColourRegex.Match(input);
						int.TryParse(match.Groups[2].Value, out result2);
						int.TryParse(match.Groups[3].Value, out result3);
						int.TryParse(match.Groups[4].Value, out result4);
					}
					else if (htmlColourRegex.IsMatch(input))
					{
						Match match2 = htmlColourRegex.Match(input);
						result2 = Convert.ToInt32(match2.Groups[1].Value, 16);
						result3 = Convert.ToInt32(match2.Groups[2].Value, 16);
						result4 = Convert.ToInt32(match2.Groups[3].Value, 16);
					}
					else if (htmlColourRegexShort.IsMatch(input))
					{
						Match match3 = htmlColourRegexShort.Match(input);
						result2 = Convert.ToInt32(match3.Groups[1].Value + match3.Groups[1].Value, 16);
						result3 = Convert.ToInt32(match3.Groups[2].Value + match3.Groups[2].Value, 16);
						result4 = Convert.ToInt32(match3.Groups[3].Value + match3.Groups[3].Value, 16);
					}
					result = new Color(result2, result3, result4);
				}
			}
			return result;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}
}
