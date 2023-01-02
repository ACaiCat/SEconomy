using System;
using System.Collections.Generic;
using System.Linq;

namespace Wolfje.Plugins.SEconomy.Journal.XMLJournal
{
	public class XmlTransaction : ITransaction
	{
		private IBankAccount owningBankAccount;

		private Dictionary<string, object> customValues;

		public const string kXmlTransactionOldTransactonK = "kXmlTransactionOldTransactonK";

		public const string kXmlTransactionOldTransactonFK = "kXmlTransactionOldTransactonK";

		public long BankAccountTransactionK { get; set; }

		public long BankAccountFK { get; set; }

		public Money Amount { get; set; }

		public string Message { get; set; }

		public BankAccountTransactionFlags Flags { get; set; }

		public BankAccountTransactionFlags Flags2 { get; set; }

		public DateTime TransactionDateUtc { get; set; }

		public long BankAccountTransactionFK { get; set; }

		public IBankAccount BankAccount => owningBankAccount;

		public ITransaction OppositeTransaction => owningBankAccount.OwningJournal.Transactions.FirstOrDefault((ITransaction i) => i.BankAccountTransactionK == BankAccountTransactionFK);

		public Dictionary<string, object> CustomValues
		{
			get
			{
				return customValues;
			}
			set
			{
				customValues = value;
			}
		}

		public XmlTransaction(IBankAccount OwningAccount)
		{
			if (OwningAccount == null)
			{
				throw new ArgumentNullException("OwningAccount is null");
			}
			owningBankAccount = OwningAccount;
			BankAccountFK = OwningAccount.BankAccountK;
			customValues = new Dictionary<string, object>();
		}
	}
}
