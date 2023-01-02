using System;
using System.Collections.Generic;

namespace Wolfje.Plugins.SEconomy.Journal.MySQLJournal
{
	public class MySQLTransaction : ITransaction
	{
		protected IBankAccount bankAccount;

		public long BankAccountTransactionK { get; set; }

		public long BankAccountFK { get; set; }

		public Money Amount { get; set; }

		public string Message { get; set; }

		public BankAccountTransactionFlags Flags { get; set; }

		public BankAccountTransactionFlags Flags2 { get; set; }

		public DateTime TransactionDateUtc { get; set; }

		public long BankAccountTransactionFK { get; set; }

		public IBankAccount BankAccount => bankAccount;

		public ITransaction OppositeTransaction => null;

		public Dictionary<string, object> CustomValues => null;

		public MySQLTransaction(IBankAccount bankAccount)
		{
			this.bankAccount = bankAccount;
		}
	}
}
