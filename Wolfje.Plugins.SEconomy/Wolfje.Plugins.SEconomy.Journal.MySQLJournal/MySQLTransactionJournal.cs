using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using Wolfje.Plugins.SEconomy.Configuration;
using Wolfje.Plugins.SEconomy.Extensions;

namespace Wolfje.Plugins.SEconomy.Journal.MySQLJournal
{
	public class MySQLTransactionJournal : ITransactionJournal, IDisposable
	{
		protected string connectionString;

		protected SQLConnectionProperties sqlProperties;

		protected List<IBankAccount> bankAccounts;

		protected MySqlConnection mysqlConnection;

		protected SEconomy instance;

		public SEconomy SEconomyInstance { get; set; }

		public bool JournalSaving { get; set; }

		public bool BackupsEnabled { get; set; }

		public List<IBankAccount> BankAccounts => bankAccounts;

		public IEnumerable<ITransaction> Transactions => null;

		public MySqlConnection Connection => new MySqlConnection(connectionString + "database=" + sqlProperties.DbName);

		public MySqlConnection ConnectionNoCatalog => new MySqlConnection(connectionString);

		public event EventHandler<BankTransferEventArgs> BankTransferCompleted;

		public event EventHandler<PendingTransactionEventArgs> BankTransactionPending;

		public event EventHandler<JournalLoadingPercentChangedEventArgs> JournalLoadingPercentChanged;

		public MySQLTransactionJournal(SEconomy instance, SQLConnectionProperties sqlProperties)
		{
			if (!string.IsNullOrEmpty(sqlProperties.DbOverrideConnectionString))
			{
				connectionString = sqlProperties.DbOverrideConnectionString;
			}
			this.instance = instance;
			this.sqlProperties = sqlProperties;
			connectionString = "server=" + sqlProperties.DbHost + ";user id=" + sqlProperties.DbUsername + ";password=" + sqlProperties.DbPassword + ";connect timeout=60;";
			SEconomyInstance = instance;
			mysqlConnection = new MySqlConnection(connectionString);
		}

		public IBankAccount AddBankAccount(string UserAccountName, long WorldID, BankAccountFlags Flags, string iDonoLol)
		{
			return AddBankAccount(new MySQLBankAccount(this)
			{
				UserAccountName = UserAccountName,
				Description = iDonoLol,
				WorldID = WorldID,
				Flags = Flags
			});
		}

		public IBankAccount AddBankAccount(IBankAccount Account)
		{
			long identity = 0L;
			string query = "INSERT INTO `bank_account` \n\t\t\t\t\t\t\t\t\t(user_account_name, world_id, flags, flags2, description)\n\t\t\t\t\t\t\t\t  VALUES (@0, @1, @2, @3, @4);";
			if (string.IsNullOrEmpty(Account.UserAccountName))
			{
				return null;
			}
			try
			{
				if (Connection.QueryIdentity(query, out identity, Account.UserAccountName, Account.WorldID, (int)Account.Flags, 0, Account.Description) < 0)
				{
					return null;
				}
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError(" seconomy mysql: sql error adding bank account: " + ex.ToString());
				return null;
			}
			Account.BankAccountK = identity;
			lock (BankAccounts)
			{
				BankAccounts.Add(Account);
				return Account;
			}
		}

		public IBankAccount GetBankAccountByName(string UserAccountName)
		{
			if (bankAccounts == null)
			{
				return null;
			}
			lock (BankAccounts)
			{
				return bankAccounts.FirstOrDefault((IBankAccount i) => i.UserAccountName == UserAccountName);
			}
		}

		public IBankAccount GetBankAccount(long BankAccountK)
		{
			if (BankAccounts == null)
			{
				return null;
			}
			lock (BankAccounts)
			{
				return BankAccounts.FirstOrDefault((IBankAccount i) => i.BankAccountK == BankAccountK);
			}
		}

		public IEnumerable<IBankAccount> GetBankAccountList(long BankAccountK)
		{
			if (bankAccounts == null)
			{
				return null;
			}
			lock (BankAccounts)
			{
				return BankAccounts.Where((IBankAccount i) => i.BankAccountK == BankAccountK);
			}
		}

		public async Task DeleteBankAccountAsync(long BankAccountK)
		{
			IBankAccount bankAccount = GetBankAccount(BankAccountK);
			int affected = 0;
			try
			{
				bool flag = bankAccount == null;
				if (!flag)
				{
					int num;
					affected = (num = await Connection.QueryAsync("DELETE FROM `bank_account` WHERE `bank_account_id` = @0", BankAccountK));
					flag = num == 0;
				}
				if (flag)
				{
					return;
				}
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError("seconomy mysql: DeleteBankAccount failed: {0}", ex.Message);
			}
			if (affected != 1)
			{
				TShock.Log.ConsoleError("seconomy mysql: DeleteBankAccount affected {0} rows where it should have only been 1.", affected);
				return;
			}
			lock (BankAccounts)
			{
				BankAccounts.RemoveAll((IBankAccount i) => i.BankAccountK == BankAccountK);
			}
		}

		public void SaveJournal()
		{
		}

		public async Task SaveJournalAsync()
		{
			await Task.FromResult<object>(null);
		}

		protected bool DatabaseExists()
		{
			string query = "select count(`schema_name`) \n\t\t\t\t\t\t\tfrom `information_schema`.`schemata`\n\t\t\t\t\t\t\twhere `schema_name` = @0";
			if (ConnectionNoCatalog.QueryScalar<long>(query, new object[1] { sqlProperties.DbName }) > 0)
			{
				return true;
			}
			return false;
		}

		protected void CreateDatabase()
		{
			Regex regex = new Regex("create\\$(\\d+)\\.sql");
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			Match match = null;
			int result = 0;
			string[] manifestResourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
			string[] array = manifestResourceNames;
			foreach (string text in array)
			{
				if ((match = regex.Match(text)).Success && int.TryParse(match.Groups[1].Value, out result) && !dictionary.ContainsKey(result))
				{
					using StreamReader streamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(text));
					dictionary[result] = streamReader.ReadToEnd();
				}
			}
			foreach (string item in dictionary.OrderBy(delegate(KeyValuePair<int, string> i)
			{
				KeyValuePair<int, string> keyValuePair2 = i;
				return keyValuePair2.Key;
			}).Select(delegate(KeyValuePair<int, string> i)
			{
				KeyValuePair<int, string> keyValuePair = i;
				return keyValuePair.Value;
			}))
			{
				string query = item.Replace("$CATALOG", sqlProperties.DbName);
				ConnectionNoCatalog.Query(query);
			}
		}

		public bool LoadJournal()
		{
			string text = null;
			ConsoleEx.WriteLineColour(ConsoleColor.Cyan, " Using MySQL journal - mysql://{0}@{1}/{2}\r\n", sqlProperties.DbUsername, sqlProperties.DbHost, sqlProperties.DbName);
			while (!DatabaseExists())
			{
				TShock.Log.ConsoleInfo("The database {0} on MySQL server {1} does not exist or cannot be accessed.", sqlProperties.DbName, sqlProperties.DbHost);
				TShock.Log.ConsoleInfo("If the schema does exist, make sure the SQL user has access to it.");
				Console.Write("New database {0} on MySQL Server {1}, retry or cancel? (n/r/c) ", sqlProperties.DbName, sqlProperties.DbHost);
				text = Console.ReadLine();
				if (text.Equals("n", StringComparison.CurrentCultureIgnoreCase))
				{
					try
					{
						CreateSchema();
					}
					catch
					{
						continue;
					}
					break;
				}
				if (text.Equals("c", StringComparison.CurrentCultureIgnoreCase))
				{
					return false;
				}
			}
			LoadBankAccounts();
			return true;
		}

		protected void CreateSchema()
		{
			try
			{
				CreateDatabase();
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError(" Your SEconomy database does not exist and it couldn't be created.");
				TShock.Log.ConsoleError(" Check your SQL server is on, and the credentials you supplied have");
				TShock.Log.ConsoleError(" permissions to CREATE DATABASE.");
				TShock.Log.ConsoleError(" The error was: {0}", ex.Message);
				throw;
			}
		}

		public Task<bool> LoadJournalAsync()
		{
			return Task.Run(() => LoadJournal());
		}

		protected void LoadBankAccounts()
		{
			long bankAccountCount = 0L;
			long num = 0L;
			int location = 0;
			int oldPercent = 0;
			double percentComplete = 0.0;
			JournalLoadingPercentChangedEventArgs parsingArgs = new JournalLoadingPercentChangedEventArgs
			{
				Label = "Loading"
			};
			try
			{
				if (this.JournalLoadingPercentChanged != null)
				{
					this.JournalLoadingPercentChanged(this, parsingArgs);
				}
				bankAccounts = new List<IBankAccount>();
				bankAccountCount = Connection.QueryScalar<long>("select count(*) from `bank_account`;", new object[0]);
				num = Connection.QueryScalar<long>("select count(*) from `bank_account_transaction`;", new object[0]);
				QueryResult res = Connection.QueryReader("select bank_account.*, sum(bank_account_transaction.amount) as balance\n                                                                         from bank_account \n                                                                             inner join bank_account_transaction on bank_account_transaction.bank_account_fk = bank_account.bank_account_id \n                                                                         group by bank_account.bank_account_id;");
				Action<int> action = delegate(int i)
				{
					percentComplete = (double)i / (double)bankAccountCount * 100.0;
					if (oldPercent != (int)percentComplete)
					{
						parsingArgs.Percent = (int)percentComplete;
						if (this.JournalLoadingPercentChanged != null)
						{
							this.JournalLoadingPercentChanged(this, parsingArgs);
						}
						oldPercent = (int)percentComplete;
					}
				};
				foreach (IDataReader item in res.AsEnumerable())
				{
					MySQLBankAccount mySQLBankAccount = null;
					MySQLBankAccount mySQLBankAccount2 = new MySQLBankAccount(this);
					mySQLBankAccount2.BankAccountK = item.Get<long>("bank_account_id");
					mySQLBankAccount2.Description = item.Get<string>("description");
					mySQLBankAccount2.Flags = (BankAccountFlags)Enum.Parse(typeof(BankAccountFlags), item.Get<int>("flags").ToString());
					mySQLBankAccount2.UserAccountName = item.Get<string>("user_account_name");
					mySQLBankAccount2.WorldID = item.Get<long>("world_id");
					mySQLBankAccount2.Balance = item.Get<long>("balance");
					mySQLBankAccount = mySQLBankAccount2;
					lock (BankAccounts)
					{
						BankAccounts.Add(mySQLBankAccount);
					}
					Interlocked.Increment(ref location);
					action(location);
				}
				parsingArgs.Percent = 100;
				if (this.JournalLoadingPercentChanged != null)
				{
					this.JournalLoadingPercentChanged(this, parsingArgs);
				}
				Console.WriteLine("\r\n");
				ConsoleEx.WriteLineColour(ConsoleColor.Cyan, " Journal clean: {0} accounts, {1} transactions", BankAccounts.Count(), num);
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError(" seconomy mysql: db error in LoadJournal: " + ex.Message);
				throw;
			}
		}

		public void BackupJournal()
		{
		}

		public async Task BackupJournalAsync()
		{
			await Task.FromResult<object>(null);
		}

		public async Task SquashJournalAsync()
		{
			TShock.Log.ConsoleInfo("seconomy mysql: squashing accounts.");
			if (await Connection.QueryAsync("CALL seconomy_squash();") < 0)
			{
				TShock.Log.ConsoleError("seconomy mysql: squashing failed.");
			}
			TShock.Log.ConsoleInfo("seconomy mysql: re-syncing online accounts");
			TSPlayer[] players = TShock.Players;
			TSPlayer[] array = players;
			foreach (TSPlayer tSPlayer in array)
			{
				IBankAccount bankAccount;
				if (tSPlayer != null && tSPlayer.Name != null && (bankAccount = instance.GetBankAccount(tSPlayer)) != null)
				{
					await bankAccount.SyncBalanceAsync();
				}
			}
			TShock.Log.ConsoleInfo("seconomy mysql: squash complete.");
		}

		private bool TransferMaySucceed(IBankAccount FromAccount, IBankAccount ToAccount, Money MoneyNeeded, BankAccountTransferOptions Options)
		{
			if (FromAccount == null || ToAccount == null)
			{
				return false;
			}
			if (!FromAccount.IsSystemAccount && !FromAccount.IsPluginAccount && (Options & BankAccountTransferOptions.AllowDeficitOnNormalAccount) != BankAccountTransferOptions.AllowDeficitOnNormalAccount)
			{
				if ((long)FromAccount.Balance >= (long)MoneyNeeded)
				{
					return (long)MoneyNeeded > 0;
				}
				return false;
			}
			return true;
		}

		private ITransaction BeginSourceTransaction(MySqlTransaction SQLTransaction, long BankAccountK, Money Amount, string Message)
		{
			MySQLTransaction mySQLTransaction = null;
			long identity = -1L;
			string query = "insert into `bank_account_transaction` \n\t\t\t\t\t\t\t\t(bank_account_fk, amount, message, flags, flags2, transaction_date_utc)\n\t\t\t\t\t\t\tvalues (@0, @1, @2, @3, @4, @5);";
			IBankAccount bankAccount = null;
			if ((bankAccount = GetBankAccount(BankAccountK)) == null)
			{
				return null;
			}
			mySQLTransaction = new MySQLTransaction(bankAccount)
			{
				Amount = -1 * (long)Amount,
				BankAccountFK = bankAccount.BankAccountK,
				Flags = BankAccountTransactionFlags.FundsAvailable,
				Message = Message,
				TransactionDateUtc = DateTime.UtcNow
			};
			try
			{
				SQLTransaction.Connection.QueryIdentityTransaction(SQLTransaction, query, out identity, mySQLTransaction.BankAccountFK, (long)mySQLTransaction.Amount, mySQLTransaction.Message, 1, 0, DateTime.UtcNow);
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError(" seconomy mysql: Database error in BeginSourceTransaction: " + ex.Message);
				return null;
			}
			mySQLTransaction.BankAccountTransactionK = identity;
			return mySQLTransaction;
		}

		private ITransaction FinishEndTransaction(MySqlTransaction SQLTransaction, IBankAccount ToAccount, Money Amount, string Message)
		{
			MySQLTransaction mySQLTransaction = null;
			IBankAccount bankAccount = null;
			long identity = -1L;
			string query = "insert into `bank_account_transaction` \n\t\t\t\t\t\t\t\t(bank_account_fk, amount, message, flags, flags2, transaction_date_utc)\n\t\t\t\t\t\t\tvalues (@0, @1, @2, @3, @4, @5);";
			if ((bankAccount = GetBankAccount(ToAccount.BankAccountK)) == null)
			{
				return null;
			}
			mySQLTransaction = new MySQLTransaction(bankAccount)
			{
				Amount = Amount,
				BankAccountFK = bankAccount.BankAccountK,
				Flags = BankAccountTransactionFlags.FundsAvailable,
				Message = Message,
				TransactionDateUtc = DateTime.UtcNow
			};
			try
			{
				SQLTransaction.Connection.QueryIdentityTransaction(SQLTransaction, query, out identity, mySQLTransaction.BankAccountFK, (long)mySQLTransaction.Amount, mySQLTransaction.Message, 1, 0, DateTime.UtcNow);
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError(" seconomy mysql: Database error in FinishEndTransaction: " + ex.Message);
				return null;
			}
			mySQLTransaction.BankAccountTransactionK = identity;
			return mySQLTransaction;
		}

		public void BindTransactions(MySqlTransaction SQLTransaction, long SourceBankTransactionK, long DestBankTransactionK)
		{
			int num = -1;
			string query = "update `bank_account_transaction` \n\t\t\t\t\t\t\t set `bank_account_transaction_fk` = @0\n\t\t\t\t\t\t\t where `bank_account_transaction_id` = @1";
			try
			{
				if ((num = SQLTransaction.Connection.QueryTransaction(SQLTransaction, query, SourceBankTransactionK, DestBankTransactionK)) != 1)
				{
					TShock.Log.ConsoleError(" seconomy mysql:  Error in BindTransactions: updated row count was " + num);
				}
				if ((num = SQLTransaction.Connection.QueryTransaction(SQLTransaction, query, DestBankTransactionK, SourceBankTransactionK)) != 1)
				{
					TShock.Log.ConsoleError(" seconomy mysql:  Error in BindTransactions: updated row count was " + num);
				}
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError(" seconomy mysql: Database error in BindTransactions: " + ex.Message);
			}
		}

		public BankTransferEventArgs TransferBetween(IBankAccount FromAccount, IBankAccount ToAccount, Money Amount, BankAccountTransferOptions Options, string TransactionMessage, string JournalMessage)
		{
			PendingTransactionEventArgs pendingTransactionEventArgs = new PendingTransactionEventArgs(FromAccount, ToAccount, Amount, Options, TransactionMessage, JournalMessage);
			MySqlConnection mySqlConnection = null;
			MySqlTransaction mySqlTransaction = null;
			BankTransferEventArgs bankTransferEventArgs = new BankTransferEventArgs
			{
				TransferSucceeded = false
			};
			string query = "select count(*)\n\t\t\t\t\t\t\t\t\t\t  from `bank_account`\n\t\t\t\t\t\t\t\t\t\t  where\t`bank_account_id` = @0;";
			Stopwatch stopwatch = new Stopwatch();
			if (SEconomyInstance.Configuration.EnableProfiler)
			{
				stopwatch.Start();
			}
			if (ToAccount == null || !TransferMaySucceed(FromAccount, ToAccount, Amount, Options))
			{
				return bankTransferEventArgs;
			}
			if ((mySqlConnection = Connection) == null)
			{
				TShock.Log.ConsoleError(" seconomy mysql: Cannot connect to the SQL server");
				return bankTransferEventArgs;
			}
			mySqlConnection.Open();
			if (Connection.QueryScalar<long>(query, new object[1] { FromAccount.BankAccountK }) != 1)
			{
				TShock.Log.ConsoleError(" seconomy mysql: Source account " + FromAccount.BankAccountK + " does not exist.");
				mySqlConnection.Dispose();
				return bankTransferEventArgs;
			}
			if (Connection.QueryScalar<long>(query, new object[1] { ToAccount.BankAccountK }) != 1)
			{
				TShock.Log.ConsoleError(" seconomy mysql: Source account " + FromAccount.BankAccountK + " does not exist.");
				mySqlConnection.Dispose();
				return bankTransferEventArgs;
			}
			if (this.BankTransactionPending != null)
			{
				this.BankTransactionPending(this, pendingTransactionEventArgs);
			}
			if (pendingTransactionEventArgs == null || pendingTransactionEventArgs.IsCancelled)
			{
				return bankTransferEventArgs;
			}
			bankTransferEventArgs.Amount = pendingTransactionEventArgs.Amount;
			bankTransferEventArgs.SenderAccount = pendingTransactionEventArgs.FromAccount;
			bankTransferEventArgs.ReceiverAccount = pendingTransactionEventArgs.ToAccount;
			bankTransferEventArgs.TransferOptions = Options;
			bankTransferEventArgs.TransactionMessage = pendingTransactionEventArgs.TransactionMessage;
			try
			{
				mySqlTransaction = mySqlConnection.BeginTransaction();
				ITransaction transaction;
				if ((transaction = BeginSourceTransaction(mySqlTransaction, FromAccount.BankAccountK, pendingTransactionEventArgs.Amount, pendingTransactionEventArgs.JournalLogMessage)) == null)
				{
					throw new Exception("BeginSourceTransaction failed");
				}
				ITransaction transaction2;
				if ((transaction2 = FinishEndTransaction(mySqlTransaction, ToAccount, pendingTransactionEventArgs.Amount, pendingTransactionEventArgs.JournalLogMessage)) == null)
				{
					throw new Exception("FinishEndTransaction failed");
				}
				BindTransactions(mySqlTransaction, transaction.BankAccountTransactionK, transaction2.BankAccountTransactionK);
				mySqlTransaction.Commit();
			}
			catch (Exception ex)
			{
				if (mySqlConnection != null && mySqlConnection.State == ConnectionState.Open)
				{
					try
					{
						mySqlTransaction.Rollback();
					}
					catch
					{
						TShock.Log.ConsoleError(" seconomy mysql: error in rollback:" + ex.ToString());
					}
				}
				TShock.Log.ConsoleError(" seconomy mysql: database error in transfer:" + ex.ToString());
				bankTransferEventArgs.Exception = ex;
				return bankTransferEventArgs;
			}
			finally
			{
				mySqlConnection?.Dispose();
			}
			FromAccount.SyncBalance();
			ToAccount.SyncBalance();
			bankTransferEventArgs.TransferSucceeded = true;
			if (this.BankTransferCompleted != null)
			{
				this.BankTransferCompleted(this, bankTransferEventArgs);
			}
			if (SEconomyInstance.Configuration.EnableProfiler)
			{
				stopwatch.Stop();
				TShock.Log.ConsoleInfo("seconomy mysql: transfer took {0} ms", stopwatch.ElapsedMilliseconds);
			}
			return bankTransferEventArgs;
		}

		public async Task<BankTransferEventArgs> TransferBetweenAsync(IBankAccount FromAccount, IBankAccount ToAccount, Money Amount, BankAccountTransferOptions Options, string TransactionMessage, string JournalMessage)
		{
			return await Task.Run(() => TransferBetween(FromAccount, ToAccount, Amount, Options, TransactionMessage, JournalMessage));
		}

		public IBankAccount GetWorldAccount()
		{
			IBankAccount bankAccount = null;
			if ((SEconomyInstance.WorldAccount != null && SEconomyInstance.WorldAccount.WorldID == Main.worldID) || Main.worldID == 0)
			{
				return null;
			}
			lock (BankAccounts)
			{
				bankAccount = bankAccounts.Where((IBankAccount i) => (i.Flags & BankAccountFlags.SystemAccount) == BankAccountFlags.SystemAccount && (i.Flags & BankAccountFlags.PluginAccount) == 0 && i.WorldID == Main.worldID).FirstOrDefault();
			}
			if (bankAccount == null)
			{
				bankAccount = AddBankAccount("SYSTEM", Main.worldID, BankAccountFlags.Enabled | BankAccountFlags.SystemAccount | BankAccountFlags.LockedToWorld, "World account for world " + Main.worldName);
			}
			if (bankAccount != null)
			{
				if ((bankAccount.Flags & BankAccountFlags.Enabled) != BankAccountFlags.Enabled)
				{
					TShock.Log.ConsoleError(string.Format(SEconomyPlugin.Locale.StringOrDefault(60, "The world account for world {0} is disabled.  Currency will not work for this game."), Main.worldName));
					return null;
				}
			}
			else
			{
				TShock.Log.ConsoleError(SEconomyPlugin.Locale.StringOrDefault(61, "There was an error loading the bank account for this world.  Currency will not work for this game."));
			}
			return bankAccount;
		}

		public void DumpSummary()
		{
			throw new NotImplementedException();
		}

		public void CleanJournal(PurgeOptions options)
		{
			long num = 0L;
			List<string> list = (from i in TShock.UserAccounts.GetUserAccounts()
				select i.Name).ToList();
			List<long> list2 = new List<long>();
			JournalLoadingPercentChangedEventArgs journalLoadingPercentChangedEventArgs = new JournalLoadingPercentChangedEventArgs
			{
				Label = "Scrub",
				Percent = 0
			};
			if (this.JournalLoadingPercentChanged != null)
			{
				this.JournalLoadingPercentChanged(this, journalLoadingPercentChangedEventArgs);
			}
			for (int j = 0; j < BankAccounts.Count; j++)
			{
				double num2 = (double)j / (double)BankAccounts.Count * 100.0;
				IBankAccount bankAccount = bankAccounts.ElementAtOrDefault(j);
				if ((options & PurgeOptions.RemoveOrphanedAccounts) == PurgeOptions.RemoveOrphanedAccounts && !list.Contains(bankAccount.UserAccountName) && !list2.Contains(bankAccount.BankAccountK))
				{
					list2.Add(bankAccount.BankAccountK);
					list.Remove(bankAccount.UserAccountName);
				}
				else if ((options & PurgeOptions.RemoveZeroBalanceAccounts) == PurgeOptions.RemoveZeroBalanceAccounts && (long)bankAccount.Balance <= 0 && !bankAccount.IsSystemAccount && !list2.Contains(bankAccount.BankAccountK))
				{
					list2.Add(bankAccount.BankAccountK);
				}
				else if (num != (int)num2)
				{
					journalLoadingPercentChangedEventArgs.Percent = (int)num2;
					if (this.JournalLoadingPercentChanged != null)
					{
						this.JournalLoadingPercentChanged(this, journalLoadingPercentChangedEventArgs);
					}
					num = (int)num2;
				}
			}
			if (list2.Count <= 0)
			{
				return;
			}
			journalLoadingPercentChangedEventArgs.Label = "Clean";
			journalLoadingPercentChangedEventArgs.Percent = 0;
			for (int k = 0; k < list2.Count; k++)
			{
				double num3 = (double)k / (double)list2.Count * 100.0;
				DeleteBankAccountAsync(list2[k]).Wait();
				if (num != (int)num3)
				{
					journalLoadingPercentChangedEventArgs.Percent = (int)num3;
					if (this.JournalLoadingPercentChanged != null)
					{
						this.JournalLoadingPercentChanged(this, journalLoadingPercentChangedEventArgs);
					}
					num = (int)num3;
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
				mysqlConnection.Dispose();
			}
		}
	}
}
