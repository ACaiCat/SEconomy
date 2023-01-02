using System;

namespace Wolfje.Plugins.SEconomy.Journal
{
	[Flags]
	public enum BankAccountFlags
	{
		Enabled = 1,
		SystemAccount = 2,
		LockedToWorld = 4,
		PluginAccount = 8
	}
}
