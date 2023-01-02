using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TShockAPI;
using TShockAPI.DB;
using Wolfje.Plugins.SEconomy.Extensions;

namespace Wolfje.Plugins.SEconomy.Journal.MySQLJournal
{
	public class MySQLBankAccount : IBankAccount
	{
		protected MySQLTransactionJournal journal;

		public ITransactionJournal OwningJournal => journal;

		public long BankAccountK { get; set; }

		public string OldBankAccountK { get; set; }

		public string UserAccountName { get; set; }

		public long WorldID { get; set; }

		public BankAccountFlags Flags { get; set; }

		public string Description { get; set; }

		public Money Balance { get; set; }

		public bool IsAccountEnabled => (Flags & BankAccountFlags.Enabled) == BankAccountFlags.Enabled;

		public bool IsSystemAccount => (Flags & BankAccountFlags.SystemAccount) == BankAccountFlags.SystemAccount;

		public bool IsLockedToWorld => (Flags & BankAccountFlags.LockedToWorld) == BankAccountFlags.LockedToWorld;

		public bool IsPluginAccount => (Flags & BankAccountFlags.PluginAccount) == BankAccountFlags.PluginAccount;

		public List<ITransaction> Transactions
		{
			get
			{
				List<ITransaction> list = new List<ITransaction>();
				using QueryResult queryResult = journal.Connection.QueryReader("SELECT * FROM `bank_account_transaction` WHERE `bank_account_fk` = " + BankAccountK + ";");
				foreach (IDataReader item2 in queryResult.AsEnumerable())
				{
					MySQLTransaction item = new MySQLTransaction(this)
					{
						BankAccountFK = item2.Get<long>("bank_account_fk"),
						BankAccountTransactionK = item2.Get<long>("bank_account_transaction_id"),
						BankAccountTransactionFK = item2.Get<long?>("bank_account_transaction_fk").GetValueOrDefault(-1L),
						Flags = (BankAccountTransactionFlags)item2.Get<int>("flags"),
						TransactionDateUtc = item2.GetDateTime(queryResult.Reader.GetOrdinal("transaction_date_utc")),
						Amount = item2.Get<long>("amount"),
						Message = item2.Get<string>("message")
					};
					list.Add(item);
				}
				return list;
			}
		}

		public MySQLBankAccount(MySQLTransactionJournal journal)
		{
			this.journal = journal;
		}

		public ITransaction AddTransaction(ITransaction Transaction)
		{
			throw new NotSupportedException("AddTransaction via interface is not supported for SQL journals.  To transfer money between accounts, use the TransferTo methods instead.");
		}

		public void ResetAccountTransactions(long BankAccountK)
		{
			try
			{
				journal.Connection.Query("DELETE FROM `bank_account_transaction` WHERE `bank_account_fk` = " + this.BankAccountK + ";");
				Balance = 0L;
			}
			catch
			{
				TShock.Log.ConsoleError(" seconomy mysql: MySQL command error in ResetAccountTransactions");
			}
		}

		public async Task ResetAccountTransactionsAsync(long BankAccountK)
		{
			await Task.Run(delegate
			{
				ResetAccountTransactions(BankAccountK);
			});
		}

		public async Task SyncBalanceAsync()
		{
			await Task.Run(delegate
			{
				SyncBalance();
			});
		}

		public void SyncBalance(IDbConnection conn)
		{
			try
			{
				Balance = Convert.ToInt64(journal.Connection.QueryScalarExisting<decimal>("SELECT IFNULL(SUM(Amount), 0) FROM `bank_account_transaction` WHERE `bank_account_transaction`.`bank_account_fk` = " + BankAccountK + ";", new object[0]));
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError(" seconomy mysql: SQL error in SyncBalance: " + ex.Message);
			}
		}

		public async Task SyncBalanceAsync(IDbConnection conn)
		{
			await Task.Run(delegate
			{
				SyncBalance(conn);
			});
		}

		public void SyncBalance()
		{
			try
			{
				Balance = Convert.ToInt64(journal.Connection.QueryScalar<decimal>("SELECT IFNULL(SUM(Amount), 0) FROM `bank_account_transaction` WHERE `bank_account_transaction`.`bank_account_fk` = " + BankAccountK + ";", new object[0]));
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError(" seconomy mysql: SQL error in SyncBalance: " + ex.Message);
			}
		}

		public BankTransferEventArgs TransferTo(IBankAccount Account, Money Amount, BankAccountTransferOptions Options, string TransactionMessage, string JournalMessage)
		{
			return journal.TransferBetween(this, Account, Amount, Options, TransactionMessage, JournalMessage);
		}

		public async Task<BankTransferEventArgs> TransferToAsync(int Index, Money Amount, BankAccountTransferOptions Options, string TransactionMessage, string JournalMessage)
		{
			IBankAccount account;
			if (SEconomyPlugin.Instance == null || (account = SEconomyPlugin.Instance.GetBankAccount(Index)) == null)
			{
				return null;
			}
			return await Task.Factory.StartNew(() => TransferTo(account, Amount, Options, TransactionMessage, JournalMessage));
		}

		public async Task<BankTransferEventArgs> TransferToAsync(IBankAccount ToAccount, Money Amount, BankAccountTransferOptions Options, string TransactionMessage, string JournalMessage)
		{
			return await Task.Factory.StartNew(() => TransferTo(ToAccount, Amount, Options, TransactionMessage, JournalMessage));
		}

		public override string ToString()
		{
			return $"MySQLBankAccount {BankAccountK} UserAccountName={UserAccountName} Balance={BankAccountK}";
		}
	}
}
