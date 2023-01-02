using System;

namespace Wolfje.Plugins.SEconomy.Journal
{
	public class PendingTransactionEventArgs : EventArgs
	{
		private IBankAccount fromAccount;

		private IBankAccount toAccount;

		public IBankAccount FromAccount
		{
			get
			{
				return fromAccount;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("FromAaccount cannot be set to null.");
				}
				fromAccount = value;
			}
		}

		public IBankAccount ToAccount
		{
			get
			{
				return toAccount;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("ToAccount cannot be set to null.");
				}
				toAccount = value;
			}
		}

		public Money Amount { get; set; }

		public BankAccountTransferOptions Options { get; private set; }

		public string TransactionMessage { get; set; }

		public string JournalLogMessage { get; set; }

		public bool IsCancelled { get; set; }

		public PendingTransactionEventArgs(IBankAccount From, IBankAccount To, Money Amount, BankAccountTransferOptions Options, string TransactionMessage, string LogMessage)
		{
			fromAccount = From;
			toAccount = To;
			this.Amount = Amount;
			this.Options = Options;
			this.TransactionMessage = TransactionMessage;
			JournalLogMessage = LogMessage;
			IsCancelled = false;
		}
	}
}
