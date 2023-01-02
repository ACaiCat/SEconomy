using System;

namespace Wolfje.Plugins.SEconomy.Journal
{
	[Flags]
	public enum BankAccountTransactionFlags
	{
		FundsAvailable = 1,
		Squashed = 2
	}
}
