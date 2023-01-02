using System;
using System.Collections.Generic;
using TShockAPI;

namespace Wolfje.Plugins.SEconomy
{
	public static class TShockCommandExtensions
	{
		public static bool RunWithoutPermissions(this Command cmd, string msg, TSPlayer ply, List<string> parms)
		{
			try
			{
				cmd.CommandDelegate(new CommandArgs(msg, ply, parms));
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
