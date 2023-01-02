using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace Wolfje.Plugins.SEconomy.Journal.XMLJournal
{
	public class XmlTransactionJournal : ITransactionJournal, IDisposable
	{
		private readonly List<IBankAccount> bankAccounts = new List<IBankAccount>();

		internal static long _bankAccountSeed;

		internal static long _transactionSeed;

		internal string path;

		public readonly Version XmlSchemaVersion = new Version(1, 3, 0);

		public static readonly object __writeLock = new object();

		private static readonly Random _rng = new Random();

		private const string _chars = "1234567890abcdefghijklmnopqrstuvwxyz";

		public SEconomy SEconomyInstance { get; set; }

		internal System.Timers.Timer JournalBackupTimer { get; set; }

		public bool JournalSaving { get; set; }

		public bool BackupsEnabled { get; set; }

		public List<IBankAccount> BankAccounts => bankAccounts;

		public IEnumerable<ITransaction> Transactions => bankAccounts.SelectMany((IBankAccount i) => i.Transactions);

		public event EventHandler<PendingTransactionEventArgs> BankTransactionPending;

		public event EventHandler<BankTransferEventArgs> BankTransferCompleted;

		public event EventHandler<JournalLoadingPercentChangedEventArgs> JournalLoadingPercentChanged;

		public XmlTransactionJournal(SEconomy Parent, string JournalSavePath)
		{
			SEconomyInstance = Parent;
			path = JournalSavePath;
			if (Parent != null && Parent.Configuration.JournalBackupMinutes > 0)
			{
				JournalBackupTimer = new System.Timers.Timer(Parent.Configuration.JournalBackupMinutes * 60000);
				JournalBackupTimer.Elapsed += JournalBackupTimer_Elapsed;
				JournalBackupTimer.Start();
				BackupsEnabled = true;
			}
		}

		protected async void JournalBackupTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (BackupsEnabled && !JournalSaving)
			{
				await SaveJournalAsync();
			}
		}

		public IBankAccount GetWorldAccount()
		{
			IBankAccount bankAccount = null;
			if (SEconomyInstance.WorldAccount != null && SEconomyInstance.WorldAccount.WorldID == Main.worldID)
			{
				return null;
			}
			if (Main.worldID > 0)
			{
				bankAccount = bankAccounts.Where((IBankAccount i) => (i.Flags & BankAccountFlags.SystemAccount) == BankAccountFlags.SystemAccount && (i.Flags & BankAccountFlags.PluginAccount) == 0 && i.WorldID == Main.worldID).FirstOrDefault();
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
			}
			return bankAccount;
		}

		private MemoryStream GZipDecompress(byte[] CompressedData)
		{
			MemoryStream memoryStream = new MemoryStream();
			using (GZipStream gZipStream = new GZipStream(new MemoryStream(CompressedData), CompressionMode.Decompress, leaveOpen: false))
			{
				byte[] buffer = new byte[4096];
				int num = 0;
				do
				{
					if ((num = gZipStream.Read(buffer, 0, 4096)) > 0)
					{
						memoryStream.Write(buffer, 0, num);
					}
				}
				while (num > 0);
			}
			memoryStream.Seek(0L, SeekOrigin.Begin);
			return memoryStream;
		}

		public static string RandomString(int Size)
		{
			char[] array = new char[Size];
			for (int i = 0; i < Size; i++)
			{
				int index;
				lock (_rng)
				{
					index = _rng.Next("1234567890abcdefghijklmnopqrstuvwxyz".Length);
				}
				array[i] = "1234567890abcdefghijklmnopqrstuvwxyz"[index];
			}
			return new string(array);
		}

		private XDocument NewJournal()
		{
			string value = SEconomyPlugin.Locale.StringOrDefault(62);
			string value2 = SEconomyPlugin.Locale.StringOrDefault(63);
			SEconomyPlugin.Locale.StringOrDefault(64);
			return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XComment(value), new XElement("Journal", new XAttribute("Schema", new Version(1, 3, 0).ToString()), new XElement("BankAccounts", new XComment(value2))));
		}

		public IBankAccount AddBankAccount(string UserAccountName, long WorldID, BankAccountFlags Flags, string Description)
		{
			return AddBankAccount(new XmlBankAccount(this)
			{
				UserAccountName = UserAccountName,
				WorldID = WorldID,
				Flags = Flags,
				Description = Description
			});
		}

		public IBankAccount AddBankAccount(IBankAccount Account)
		{
			Account.BankAccountK = Interlocked.Increment(ref _bankAccountSeed);
			bankAccounts.Add(Account);
			return Account;
		}

		public IBankAccount GetBankAccountByName(string UserAccountName)
		{
			if (bankAccounts == null)
			{
				return null;
			}
			return bankAccounts.FirstOrDefault((IBankAccount i) => i.UserAccountName == UserAccountName);
		}

		public IBankAccount GetBankAccount(long BankAccountK)
		{
			if (bankAccounts == null)
			{
				return null;
			}
			for (int i = 0; i < bankAccounts.Count; i++)
			{
				IBankAccount bankAccount = bankAccounts[i];
				if (bankAccount.BankAccountK == BankAccountK)
				{
					return bankAccount;
				}
			}
			return null;
		}

		public IEnumerable<IBankAccount> GetBankAccountList(long BankAccountK)
		{
			if (bankAccounts == null)
			{
				return null;
			}
			return BankAccounts.Where((IBankAccount i) => i.BankAccountK == BankAccountK);
		}

		public void DumpSummary()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (var item in from i in bankAccounts
				group i by i.BankAccountK into g
				select new
				{
					name = g.Key,
					count = g.Count()
				} into i
				orderby i.count descending
				select i)
			{
				stringBuilder.AppendLine($"{item.name},{item.count}");
			}
			File.WriteAllText(Config.BaseDirectory + Path.DirectorySeparatorChar + "test.csv", stringBuilder.ToString());
		}

		public async Task DeleteBankAccountAsync(long BankAccountK)
		{
			await Task.Run(delegate
			{
				DeleteBankAccount(BankAccountK);
			});
		}

		public void DeleteBankAccount(long BankAccountK)
		{
			if (bankAccounts != null)
			{
				bankAccounts.RemoveAll((IBankAccount i) => i.BankAccountK == BankAccountK);
			}
		}

		public void SaveJournal()
		{
			try
			{
				XDocument xDocument = NewJournal();
				XElement xElement = xDocument.Element("Journal").Element("BankAccounts");
				JournalSaving = true;
				foreach (IBankAccount bankAccount in BankAccounts)
				{
					XElement xElement2 = new XElement("BankAccount");
					xElement2.Add(new XElement("Transactions"));
					xElement2.SetAttributeValue("UserAccountName", bankAccount.UserAccountName);
					xElement2.SetAttributeValue("WorldID", bankAccount.WorldID);
					xElement2.SetAttributeValue("Flags", bankAccount.Flags);
					xElement2.SetAttributeValue("Description", bankAccount.Description);
					xElement2.SetAttributeValue("BankAccountK", bankAccount.BankAccountK);
					lock (bankAccount.Transactions)
					{
						foreach (ITransaction transaction in bankAccount.Transactions)
						{
							XElement xElement3 = new XElement("Transaction");
							xElement3.SetAttributeValue("BankAccountTransactionK", transaction.BankAccountTransactionK);
							xElement3.SetAttributeValue("BankAccountTransactionFK", transaction.BankAccountTransactionFK);
							xElement3.SetAttributeValue("Flags", transaction.Flags);
							xElement3.SetAttributeValue("Flags2", transaction.Flags2);
							xElement3.SetAttributeValue("Message", transaction.Message);
							xElement3.SetAttributeValue("TransactionDateUtc", transaction.TransactionDateUtc.ToString("s", CultureInfo.InvariantCulture));
							xElement3.SetAttributeValue("Amount", transaction.Amount.Value);
							xElement2.Element("Transactions").Add(xElement3);
						}
					}
					xElement.Add(xElement2);
				}
				lock (__writeLock)
				{
					try
					{
						File.Delete(path + ".bak");
					}
					catch
					{
					}
					try
					{
						if (File.Exists(path))
						{
							File.Move(path, path + ".bak");
						}
					}
					catch
					{
						TShock.Log.ConsoleError(SEconomyPlugin.Locale.StringOrDefault(65, "seconomy backup: Cannot copy {0} to {1}, shadow backups will not work!"), path, path + ".bak");
					}
					Console.WriteLine(SEconomyPlugin.Locale.StringOrDefault(66, "seconomy journal: writing to disk"));
					try
					{
						using FileStream fileStream = new FileStream(path + ".tmp", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
						fileStream.SetLength(0L);
						using GZipStream w = new GZipStream(fileStream, CompressionMode.Compress);
						using XmlTextWriter xmlTextWriter = new XmlTextWriter(w, Encoding.UTF8);
						xmlTextWriter.Formatting = Formatting.None;
						xDocument.WriteTo(xmlTextWriter);
					}
					catch
					{
						TShock.Log.ConsoleError(SEconomyPlugin.Locale.StringOrDefault(67, "seconomy journal: Saving your journal failed!"));
						if (File.Exists(path + ".tmp"))
						{
							try
							{
								File.Delete(path + ".tmp");
							}
							catch
							{
								TShock.Log.ConsoleError(SEconomyPlugin.Locale.StringOrDefault(68, "seconomy journal: Cannot delete temporary file!"));
								throw;
							}
						}
					}
					if (File.Exists(path + ".tmp"))
					{
						try
						{
							File.Move(path + ".tmp", path);
						}
						catch
						{
							TShock.Log.ConsoleError(SEconomyPlugin.Locale.StringOrDefault(68, "seconomy journal: Cannot delete temporary file!"));
							throw;
						}
					}
					Console.WriteLine(SEconomyPlugin.Locale.StringOrDefault(69, "seconomy journal: finished backing up."));
				}
			}
			catch
			{
				Console.WriteLine(SEconomyPlugin.Locale.StringOrDefault(70, "seconomy journal: There was an error saving your journal.  Make sure you have backups."));
			}
			finally
			{
				JournalSaving = false;
			}
		}

		public Task SaveJournalAsync()
		{
			return Task.Factory.StartNew(delegate
			{
				SaveJournal();
			});
		}

		public bool LoadJournal()
		{
			JournalLoadingPercentChangedEventArgs journalLoadingPercentChangedEventArgs = new JournalLoadingPercentChangedEventArgs
			{
				Label = "Loading"
			};
			JournalLoadingPercentChangedEventArgs journalLoadingPercentChangedEventArgs2 = new JournalLoadingPercentChangedEventArgs
			{
				Label = "Verify"
			};
			ConsoleEx.WriteLineColour(ConsoleColor.Cyan, " Using XML journal - {0}", path);
			try
			{
				byte[] compressedData = new byte[0];
				long[] duplicateAccounts;
				while (true)
				{
					try
					{
						compressedData = File.ReadAllBytes(path);
					}
					catch (Exception ex)
					{
						Console.ForegroundColor = ConsoleColor.DarkCyan;
						if (ex is FileNotFoundException || ex is DirectoryNotFoundException)
						{
							TShock.Log.ConsoleInfo(" * It appears you do not have a journal yet, one will be created for you.");
							SaveJournal();
							continue;
						}
						if (ex is SecurityException)
						{
							TShock.Log.ConsoleError(" * Access denied to the journal file.  Check permissions.");
						}
						else
						{
							TShock.Log.ConsoleError(" * Loading your journal failed: " + ex.Message);
						}
					}
					MemoryStream input;
					try
					{
						input = GZipDecompress(compressedData);
					}
					catch
					{
						TShock.Log.ConsoleError(" * Decompression failed.");
						return false;
					}
					if (this.JournalLoadingPercentChanged != null)
					{
						this.JournalLoadingPercentChanged(this, journalLoadingPercentChangedEventArgs);
					}
					try
					{
						Hashtable hashtable = new Hashtable();
						Hashtable hashtable2 = new Hashtable();
						using XmlTextReader reader = new XmlTextReader(input);
						XDocument node = XDocument.Load(reader);
						IEnumerable<XElement> enumerable = node.XPathSelectElements("/Journal/BankAccounts/BankAccount");
						int num = enumerable.Count();
						int location = 0;
						int num2 = 0;
						foreach (XElement item in enumerable)
						{
							long result = 0L;
							double num3 = (double)location / (double)num * 100.0;
							XmlBankAccount xmlBankAccount = new XmlBankAccount(this);
							xmlBankAccount.Description = item.Attribute("Description").Value;
							xmlBankAccount.UserAccountName = item.Attribute("UserAccountName").Value;
							xmlBankAccount.WorldID = long.Parse(item.Attribute("WorldID").Value);
							xmlBankAccount.Flags = (BankAccountFlags)Enum.Parse(typeof(BankAccountFlags), item.Attribute("Flags").Value);
							XmlBankAccount xmlBankAccount2 = xmlBankAccount;
							if (!long.TryParse(item.Attribute("BankAccountK").Value, out result))
							{
								result = Interlocked.Increment(ref _bankAccountSeed);
								hashtable.Add(item.Attribute("BankAccountK").Value, result);
							}
							xmlBankAccount2.BankAccountK = result;
							if (item.Element("Transactions") != null)
							{
								foreach (XElement item2 in item.Element("Transactions").Elements("Transaction"))
								{
									long result2 = 0L;
									long result3 = 0L;
									long num4 = long.Parse(item2.Attribute("Amount").Value);
									long.TryParse(item2.Attribute("BankAccountTransactionK").Value, out result2);
									long.TryParse(item2.Attribute("BankAccountTransactionFK").Value, out result3);
									DateTime.TryParse(item2.Attribute("TransactionDateUtc").Value, out var result4);
									Enum.TryParse<BankAccountTransactionFlags>(item2.Attribute("Flags").Value, out var result5);
									XmlTransaction xmlTransaction = new XmlTransaction(xmlBankAccount2)
									{
										Amount = num4,
										BankAccountTransactionK = result2,
										BankAccountTransactionFK = result3,
										TransactionDateUtc = result4,
										Flags = result5
									};
									xmlTransaction.Message = ((item2.Attribute("Message") != null) ? item2.Attribute("Message").Value : null);
									xmlBankAccount2.AddTransaction(xmlTransaction);
								}
							}
							xmlBankAccount2.SyncBalance();
							if (num2 != (int)num3)
							{
								journalLoadingPercentChangedEventArgs.Percent = (int)num3;
								if (this.JournalLoadingPercentChanged != null)
								{
									this.JournalLoadingPercentChanged(this, journalLoadingPercentChangedEventArgs);
								}
								num2 = (int)num3;
							}
							Interlocked.Increment(ref location);
							bankAccounts.Add(xmlBankAccount2);
						}
						Interlocked.Exchange(ref _bankAccountSeed, (bankAccounts.Count() > 0) ? bankAccounts.Max((IBankAccount sum) => sum.BankAccountK) : 0);
						var source = from summary in bankAccounts
							group summary by summary.BankAccountK into g
							where g.Count() > 1
							select new
							{
								name = g.Key,
								count = g.Count()
							};
						duplicateAccounts = source.Select(pred => pred.name).ToArray();
						int num5 = bankAccounts.RemoveAll((IBankAccount pred) => duplicateAccounts.Contains(pred.BankAccountK));
						if (num5 > 0)
						{
							TShock.Log.Warn("seconomy journal: removed " + num5 + " accounts with duplicate IDs.");
						}
						int num6 = node.XPathSelectElements("/Journal/Transactions/Transaction").Count();
						location = 0;
						foreach (XElement item3 in node.XPathSelectElements("/Journal/Transactions/Transaction"))
						{
							_ = (double)location / (double)num6;
							long result6 = 0L;
							long result7 = 0L;
							long num7 = long.Parse(item3.Attribute("Amount").Value);
							if (!long.TryParse(item3.Attribute("BankAccountFK").Value, out result6) && hashtable.ContainsKey(item3.Attribute("BankAccountFK").Value))
							{
								Interlocked.Exchange(ref result6, (long)hashtable[item3.Attribute("BankAccountFK").Value]);
							}
							IBankAccount bankAccount = GetBankAccount(result6);
							long.TryParse(item3.Attribute("BankAccountTransactionK").Value, out result7);
							if (bankAccount != null)
							{
								XmlTransaction xmlTransaction2 = new XmlTransaction(bankAccount)
								{
									Amount = num7,
									BankAccountTransactionK = result7
								};
								if (item3.Attribute("BankAccountTransactionFK") != null)
								{
									xmlTransaction2.CustomValues.Add("kXmlTransactionOldTransactonK", item3.Attribute("BankAccountTransactionFK").Value);
								}
								xmlTransaction2.Message = ((item3.Attribute("Message") != null) ? item3.Attribute("Message").Value : null);
								bankAccount.AddTransaction(xmlTransaction2);
								hashtable2.Add(item3.Attribute("BankAccountTransactionK").Value, xmlTransaction2.BankAccountTransactionK);
							}
							Interlocked.Increment(ref location);
						}
						int num8 = Transactions.Count();
						int location2 = 0;
						journalLoadingPercentChangedEventArgs2.Percent = 0;
						if (this.JournalLoadingPercentChanged != null)
						{
							this.JournalLoadingPercentChanged(this, journalLoadingPercentChangedEventArgs2);
						}
						foreach (IBankAccount bankAccount2 in bankAccounts)
						{
							foreach (XmlTransaction transaction in bankAccount2.Transactions)
							{
								double num9 = (double)location2 / (double)num8 * 100.0;
								if (transaction.CustomValues.ContainsKey("kXmlTransactionOldTransactonK"))
								{
									object obj2 = hashtable2[transaction.CustomValues["kXmlTransactionOldTransactonK"]];
									transaction.BankAccountTransactionFK = ((obj2 != null) ? ((long)obj2) : (-1));
								}
								if (num2 != (int)num9 && this.JournalLoadingPercentChanged != null)
								{
									journalLoadingPercentChangedEventArgs2.Percent = (int)num9;
									this.JournalLoadingPercentChanged(this, journalLoadingPercentChangedEventArgs2);
									num2 = (int)num9;
								}
								Interlocked.Increment(ref location2);
								transaction.CustomValues.Clear();
								transaction.CustomValues = null;
							}
						}
						hashtable = null;
						hashtable2 = null;
						int count = bankAccounts.Count;
						Console.WriteLine();
						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine("\r\n Journal clean: {0} accounts and {1} transactions.", count, Transactions.Count());
						Console.ResetColor();
					}
					catch (Exception ex2)
					{
						ConsoleEx.WriteAtEnd(2, ConsoleColor.Red, "[{0}]\r\n", SEconomyPlugin.Locale.StringOrDefault(79, "corrupt"));
						TShock.Log.ConsoleError(ex2.ToString());
						Console.WriteLine(SEconomyPlugin.Locale.StringOrDefault(80, "Your transaction journal appears to be corrupt and transactions have been lost.\n\nYou will start with a clean journal.\nYour old journal file has been move to SEconomy.journal.xml.gz.corrupt"));
						File.Move(path, path + "." + DateTime.Now.ToFileTime() + ".corrupt");
						SaveJournal();
						continue;
					}
					break;
				}
			}
			finally
			{
				Console.WriteLine();
			}
			return true;
		}

		public Task<bool> LoadJournalAsync()
		{
			return Task.Factory.StartNew(() => LoadJournal());
		}

		public void BackupJournal()
		{
			SaveJournal();
		}

		public async Task BackupJournalAsync()
		{
			await Task.Factory.StartNew(delegate
			{
				BackupJournal();
			});
		}

		public async Task SquashJournalAsync()
		{
			int num = BankAccounts.Count();
			bool responsibleForTurningBackupsBackOn = false;
			Console.WriteLine(SEconomyPlugin.Locale.StringOrDefault(81, "seconomy xml: beginning Squash"));
			if (SEconomyInstance.RunningJournal.BackupsEnabled)
			{
				SEconomyInstance.RunningJournal.BackupsEnabled = false;
				responsibleForTurningBackupsBackOn = true;
			}
			for (int i = 0; i < num; i++)
			{
				IBankAccount bankAccount = BankAccounts.ElementAtOrDefault(i);
				if (bankAccount != null)
				{
					ITransaction transaction = new XmlTransaction(bankAccount)
					{
						Amount = bankAccount.Transactions.Sum((ITransaction x) => x.Amount),
						Flags = (BankAccountTransactionFlags.FundsAvailable | BankAccountTransactionFlags.Squashed),
						TransactionDateUtc = DateTime.UtcNow,
						Message = "Transaction squash"
					};
					bankAccount.Transactions.Clear();
					bankAccount.AddTransaction(transaction);
				}
			}
			Console.WriteLine(SEconomyPlugin.Locale.StringOrDefault(82, "re-syncing online accounts."));
			TSPlayer[] players = TShock.Players;
			TSPlayer[] array = players;
			foreach (TSPlayer tSPlayer in array)
			{
				IBankAccount bankAccount2;
				if (tSPlayer == null || SEconomyPlugin.Instance == null || (bankAccount2 = SEconomyPlugin.Instance.GetBankAccount(tSPlayer)) == null)
				{
					return;
				}
				Console.WriteLine("re-syncing {0}", tSPlayer.Name);
				await bankAccount2.SyncBalanceAsync();
			}
			await SaveJournalAsync();
			if (responsibleForTurningBackupsBackOn)
			{
				SEconomyInstance.RunningJournal.BackupsEnabled = true;
			}
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

		private ITransaction BeginSourceTransaction(long BankAccountK, Money Amount, string Message)
		{
			IBankAccount bankAccount = GetBankAccount(BankAccountK);
			ITransaction transaction = new XmlTransaction(bankAccount)
			{
				Flags = BankAccountTransactionFlags.FundsAvailable,
				TransactionDateUtc = DateTime.UtcNow,
				Amount = (long)Amount * -1
			};
			if (!string.IsNullOrEmpty(Message))
			{
				transaction.Message = Message;
			}
			return bankAccount.AddTransaction(transaction);
		}

		private ITransaction FinishEndTransaction(long SourceBankTransactionKey, IBankAccount ToAccount, Money Amount, string Message)
		{
			ITransaction transaction = new XmlTransaction(ToAccount);
			transaction.BankAccountFK = ToAccount.BankAccountK;
			transaction.Flags = BankAccountTransactionFlags.FundsAvailable;
			transaction.TransactionDateUtc = DateTime.UtcNow;
			transaction.Amount = Amount;
			transaction.BankAccountTransactionFK = SourceBankTransactionKey;
			if (!string.IsNullOrEmpty(Message))
			{
				transaction.Message = Message;
			}
			return ToAccount.AddTransaction(transaction);
		}

		private void BindTransactions(ref ITransaction SourceTransaction, ref ITransaction DestTransaction)
		{
			SourceTransaction.BankAccountTransactionFK = DestTransaction.BankAccountTransactionK;
			DestTransaction.BankAccountTransactionFK = SourceTransaction.BankAccountTransactionK;
		}

		public Task<BankTransferEventArgs> TransferBetweenAsync(IBankAccount FromAccount, IBankAccount ToAccount, Money Amount, BankAccountTransferOptions Options, string TransactionMessage, string JournalMessage)
		{
			return Task.Factory.StartNew(() => TransferBetween(FromAccount, ToAccount, Amount, Options, TransactionMessage, JournalMessage));
		}

		public BankTransferEventArgs TransferBetween(IBankAccount FromAccount, IBankAccount ToAccount, Money Amount, BankAccountTransferOptions Options, string TransactionMessage, string JournalMessage)
		{
			BankTransferEventArgs bankTransferEventArgs = new BankTransferEventArgs();
			_ = Guid.Empty;
			try
			{
				if (ToAccount != null && TransferMaySucceed(FromAccount, ToAccount, Amount, Options))
				{
					PendingTransactionEventArgs pendingTransactionEventArgs = new PendingTransactionEventArgs(FromAccount, ToAccount, Amount, Options, TransactionMessage, JournalMessage);
					if (this.BankTransactionPending != null)
					{
						this.BankTransactionPending(this, pendingTransactionEventArgs);
					}
					if (pendingTransactionEventArgs == null)
					{
						return bankTransferEventArgs;
					}
					bankTransferEventArgs.Amount = pendingTransactionEventArgs.Amount;
					bankTransferEventArgs.SenderAccount = pendingTransactionEventArgs.FromAccount;
					bankTransferEventArgs.ReceiverAccount = pendingTransactionEventArgs.ToAccount;
					bankTransferEventArgs.TransferOptions = Options;
					bankTransferEventArgs.TransferSucceeded = false;
					bankTransferEventArgs.TransactionMessage = pendingTransactionEventArgs.TransactionMessage;
					if (pendingTransactionEventArgs.IsCancelled)
					{
						return bankTransferEventArgs;
					}
					ITransaction SourceTransaction = BeginSourceTransaction(FromAccount.BankAccountK, pendingTransactionEventArgs.Amount, pendingTransactionEventArgs.JournalLogMessage);
					if (SourceTransaction != null)
					{
						ITransaction DestTransaction = FinishEndTransaction(SourceTransaction.BankAccountTransactionK, ToAccount, pendingTransactionEventArgs.Amount, pendingTransactionEventArgs.JournalLogMessage);
						if (DestTransaction != null)
						{
							BindTransactions(ref SourceTransaction, ref DestTransaction);
							bankTransferEventArgs.TransactionID = SourceTransaction.BankAccountTransactionK;
							IBankAccount bankAccount = FromAccount;
							bankAccount.Balance = (long)bankAccount.Balance + (long)Amount * -1;
							ToAccount.Balance = (long)ToAccount.Balance + (long)Amount;
							bankTransferEventArgs.TransferSucceeded = true;
						}
					}
				}
				else
				{
					bankTransferEventArgs.TransferSucceeded = false;
					TSPlayer tSPlayer;
					if ((tSPlayer = TShock.Players.FirstOrDefault((TSPlayer i) => i.Name == FromAccount.UserAccountName)) == null)
					{
						return bankTransferEventArgs;
					}
					if (!ToAccount.IsSystemAccount && !ToAccount.IsPluginAccount)
					{
						if ((long)Amount < 0)
						{
							tSPlayer.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(83, "Invalid amount."));
						}
						else
						{
							tSPlayer.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(84, "You need {0} more to make this payment."), ((Money)((long)FromAccount.Balance - (long)Amount)).ToLongString());
						}
					}
				}
			}
			catch (Exception ex)
			{
				Exception ex3 = (bankTransferEventArgs.Exception = ex);
				Exception ex4 = ex3;
				bankTransferEventArgs.TransferSucceeded = false;
			}
			if (this.BankTransferCompleted != null)
			{
				this.BankTransferCompleted(this, bankTransferEventArgs);
			}
			return bankTransferEventArgs;
		}

		public void CleanJournal(PurgeOptions options)
		{
			long num = 0L;
			IEnumerable<string> source = from i in TShock.UserAccounts.GetUserAccounts()
				select i.Name;
			List<long> list = new List<long>();
			JournalLoadingPercentChangedEventArgs journalLoadingPercentChangedEventArgs = new JournalLoadingPercentChangedEventArgs
			{
				Label = "Purge",
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
				if ((options & PurgeOptions.RemoveOrphanedAccounts) == PurgeOptions.RemoveOrphanedAccounts && !source.Contains(bankAccount.UserAccountName) && !list.Contains(bankAccount.BankAccountK))
				{
					list.Add(bankAccount.BankAccountK);
				}
				else if ((options & PurgeOptions.RemoveZeroBalanceAccounts) == PurgeOptions.RemoveZeroBalanceAccounts && (long)bankAccount.Balance <= 0 && !bankAccount.IsSystemAccount && !list.Contains(bankAccount.BankAccountK))
				{
					list.Add(bankAccount.BankAccountK);
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
			if (list.Count <= 0)
			{
				return;
			}
			journalLoadingPercentChangedEventArgs.Label = "Clean";
			journalLoadingPercentChangedEventArgs.Percent = 0;
			for (int k = 0; k < list.Count; k++)
			{
				double num3 = (double)k / (double)list.Count * 100.0;
				DeleteBankAccount(list[k]);
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
			if (disposing && JournalBackupTimer != null)
			{
				JournalBackupTimer.Stop();
				JournalBackupTimer.Elapsed -= JournalBackupTimer_Elapsed;
				JournalBackupTimer.Dispose();
			}
		}
	}
}
