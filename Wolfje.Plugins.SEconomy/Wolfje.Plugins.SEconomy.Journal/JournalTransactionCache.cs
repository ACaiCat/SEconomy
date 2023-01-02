using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TShockAPI;

namespace Wolfje.Plugins.SEconomy.Journal
{
	public class JournalTransactionCache : IDisposable
	{
		protected readonly System.Timers.Timer UncommittedFundTimer = new(1000.0);

		protected ConcurrentQueue<CachedTransaction> CachedTransactions { get; set; }

		public JournalTransactionCache()
		{
			CachedTransactions = new ConcurrentQueue<CachedTransaction>();
			UncommittedFundTimer.Elapsed += UncommittedFundTimer_Elapsed;
			UncommittedFundTimer.Start();
		}

		protected async void UncommittedFundTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			await ProcessQueueAsync();
		}

		public void AddCachedTransaction(CachedTransaction Fund)
		{
			CachedTransactions.Enqueue(Fund);
		}

		protected async Task ProcessQueueAsync()
		{
			List<CachedTransaction> list = new List<CachedTransaction>();
			CachedTransaction fund;
			while (CachedTransactions.TryDequeue(out fund))
			{
				CachedTransaction cachedTransaction = list.FirstOrDefault((CachedTransaction i) => i.Message == fund.Message && i.SourceBankAccountK == fund.SourceBankAccountK && i.DestinationBankAccountK == fund.DestinationBankAccountK);
				if (cachedTransaction != null)
				{
					cachedTransaction.Amount = (long)cachedTransaction.Amount + (long)fund.Amount;
					cachedTransaction.Aggregations++;
				}
				else
				{
					list.Add(fund);
				}
			}
			foreach (CachedTransaction aggregatedFund in list)
			{
				IBankAccount bankAccount = SEconomyPlugin.Instance.RunningJournal.GetBankAccount(aggregatedFund.SourceBankAccountK);
				IBankAccount bankAccount2 = SEconomyPlugin.Instance.RunningJournal.GetBankAccount(aggregatedFund.DestinationBankAccountK);
				if (bankAccount != null && bankAccount2 != null)
				{
					StringBuilder stringBuilder = new StringBuilder(aggregatedFund.Message);
					if (aggregatedFund.Aggregations > 1)
					{
						stringBuilder.Insert(0, aggregatedFund.Aggregations + " ");
						stringBuilder.Append("s");
					}
					BankTransferEventArgs bankTransferEventArgs = await bankAccount.TransferToAsync(bankAccount2, aggregatedFund.Amount, aggregatedFund.Options, stringBuilder.ToString(), stringBuilder.ToString());
					if (!bankTransferEventArgs.TransferSucceeded && bankTransferEventArgs.Exception != null)
					{
						TShock.Log.ConsoleError($"seconomy缓存: 错误 source={aggregatedFund.SourceBankAccountK} dest={aggregatedFund.DestinationBankAccountK}: {bankTransferEventArgs.Exception}");
					}
				}
				else
				{
					TShock.Log.ConsoleError($"seconomy缓存: 事务缓存没有源或目标. source key={aggregatedFund.SourceBankAccountK} dest key={aggregatedFund.DestinationBankAccountK}");
				}
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				UncommittedFundTimer.Elapsed -= UncommittedFundTimer_Elapsed;
				UncommittedFundTimer.Stop();
				UncommittedFundTimer.Dispose();
				ProcessQueueAsync().Wait();
			}
		}
	}
}
