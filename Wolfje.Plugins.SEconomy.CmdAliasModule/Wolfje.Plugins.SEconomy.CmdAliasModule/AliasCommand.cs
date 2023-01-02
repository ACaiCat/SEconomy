using System.Collections.Generic;

namespace Wolfje.Plugins.SEconomy.CmdAliasModule
{
	public class AliasCommand
	{
		public string CommandAlias = "";

		public List<string> CommandsToExecute = new List<string>();

		public string Cost = "0c";

		public string Permissions = "";

		public string UsageHelpText = "";

		public int CooldownSeconds;

		public static AliasCommand Create(string CommandAlias, string Permissions, string Cost, string HelpText, int CooldownSeconds, params string[] CommandsToRun)
		{
			AliasCommand aliasCommand = new AliasCommand();
			aliasCommand.CommandAlias = CommandAlias;
			aliasCommand.Permissions = Permissions;
			aliasCommand.UsageHelpText = HelpText;
			aliasCommand.Cost = Cost;
			aliasCommand.CooldownSeconds = CooldownSeconds;
			aliasCommand.CommandsToExecute.AddRange(CommandsToRun);
			return aliasCommand;
		}
	}
}
