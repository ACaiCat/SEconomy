using System;
using TShockAPI;

namespace Wolfje.Plugins.SEconomy.CmdAliasModule
{
	public class AliasExecutedEventArgs : EventArgs
	{
		public string CommandIdentifier { get; set; }

		public CommandArgs CommandArgs { get; set; }
	}
}
