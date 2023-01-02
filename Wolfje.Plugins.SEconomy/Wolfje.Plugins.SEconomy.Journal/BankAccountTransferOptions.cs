using System;

namespace Wolfje.Plugins.SEconomy.Journal
{
	[Flags]
	public enum BankAccountTransferOptions
	{
		None = 0,
		AnnounceToReceiver = 1,
		AnnounceToSender = 2,
		AllowDeficitOnNormalAccount = 4,
		PvP = 8,
		MoneyTakenOnDeath = 0x10,
		IsPlayerToPlayerTransfer = 0x20,
		IsPayment = 0x40,
		SuppressDefaultAnnounceMessages = 0x80
	}
}
