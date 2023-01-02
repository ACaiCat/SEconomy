using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wolfje.Plugins.SEconomy.Journal
{
	public interface ITransactionJournal : IDisposable
	{
		SEconomy SEconomyInstance { get; set; }

		List<IBankAccount> BankAccounts { get; }

		IEnumerable<ITransaction> Transactions { get; }

		bool JournalSaving { get; set; }

		bool BackupsEnabled { get; set; }

		event EventHandler<PendingTransactionEventArgs> BankTransactionPending;

		event EventHandler<BankTransferEventArgs> BankTransferCompleted;

		IBankAccount AddBankAccount(string UserAccountName, long WorldID, BankAccountFlags Flags, string iDonoLol);

		IBankAccount AddBankAccount(IBankAccount Account);

		IBankAccount GetBankAccountByName(string UserAccountName);

		IBankAccount GetBankAccount(long BankAccountK);

		IEnumerable<IBankAccount> GetBankAccountList(long BankAccountK);

		Task DeleteBankAccountAsync(long BankAccountK);

		void SaveJournal();

		Task SaveJournalAsync();

		bool LoadJournal();

		Task<bool> LoadJournalAsync();

		void BackupJournal();

		Task BackupJournalAsync();

		Task SquashJournalAsync();

		BankTransferEventArgs TransferBetween(IBankAccount FromAccount, IBankAccount ToAccount, Money Amount, BankAccountTransferOptions Options, string TransactionMessage, string JournalMessage);

		Task<BankTransferEventArgs> TransferBetweenAsync(IBankAccount FromAccount, IBankAccount ToAccount, Money Amount, BankAccountTransferOptions Options, string TransactionMessage, string JournalMessage);

		IBankAccount GetWorldAccount();

		void DumpSummary();

		void CleanJournal(PurgeOptions options);
	}
}
