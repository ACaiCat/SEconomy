using System;
using System.Collections.Generic;
using TShockAPI;

namespace Wolfje.Plugins.Jist.Extensions
{
	public static class TShockCommandExtensions
	{
		public static bool RunWithoutPermissions(this Command cmd, string msg, TSPlayer ply, List<string> parms, bool silent = false)
		{
			try
			{
				CommandDelegate commandDelegate = cmd.CommandDelegate;
				commandDelegate(new CommandArgs(msg, silent, ply, parms));
			}
			catch (Exception ex)
			{
				ply.SendErrorMessage("Command failed, check logs for more details.");
				TShock.Log.Error(ex.ToString());
			}
			return true;
		}
	}
}
