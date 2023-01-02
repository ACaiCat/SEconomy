using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;
using Wolfje.Plugins.SEconomy.Journal;
using Wolfje.Plugins.SEconomy.Journal.MySQLJournal;
using Wolfje.Plugins.SEconomy.Journal.XMLJournal;

namespace Wolfje.Plugins.SEconomy
{
	public class SEconomy : IDisposable
	{
		public ITransactionJournal RunningJournal { get; set; }

		public Config Configuration { get; set; }

		public SEconomyPlugin PluginInstance { get; set; }

		public JournalTransactionCache TransactionCache { get; set; }

		public IBankAccount WorldAccount { get; internal set; }

		public WorldEconomy WorldEc { get; internal set; }

		internal EventHandlers EventHandlers { get; set; }

		internal ChatCommands ChatCommands { get; set; }

		public Dictionary<Player, DateTime> IdleCache { get; protected set; }

		public SEconomy(SEconomyPlugin PluginInstance)
		{
			IdleCache = new Dictionary<Player, DateTime>();
			this.PluginInstance = PluginInstance;
		}

		public bool IsNet45OrNewer()
		{
			return Type.GetType("System.Reflection.ReflectionContext", throwOnError: false) != null;
		}

		public int LoadSEconomy()
		{
			if (!IsNet45OrNewer())
			{
				TShock.Log.ConsoleError("SEconomy requires Microsoft .NET framework 4.5 or later.");
				TShock.Log.ConsoleError("SEconomy will not run.");
				return -1;
			}
			try
			{
				Configuration = Config.FromFile(Config.BaseDirectory + Path.DirectorySeparatorChar + "SEconomy.config.json");
				if (!LoadJournal())
				{
					return -1;
				}
				WorldEc = new WorldEconomy(this);
				EventHandlers = new EventHandlers(this);
				ChatCommands = new ChatCommands(this);
				TransactionCache = new JournalTransactionCache();
			}
			catch (Exception)
			{
				return -1;
			}
			return 0;
		}

		protected bool LoadJournal()
		{
			ITransactionJournal runningJournal = null;
			if (Configuration == null)
			{
				return false;
			}
			if (Configuration.JournalType.Equals("xml", StringComparison.InvariantCultureIgnoreCase))
			{
				XmlTransactionJournal xmlTransactionJournal = new XmlTransactionJournal(this, Config.JournalPath);
				xmlTransactionJournal.JournalLoadingPercentChanged += delegate(object sender, JournalLoadingPercentChangedEventArgs args)
				{
					ConsoleEx.WriteBar(args);
				};
				runningJournal = xmlTransactionJournal;
			}
			else if (Configuration.JournalType.Equals("mysql", StringComparison.InvariantCultureIgnoreCase) || Configuration.JournalType.Equals("sql", StringComparison.InvariantCultureIgnoreCase))
			{
				MySQLTransactionJournal mySQLTransactionJournal = new MySQLTransactionJournal(this, Configuration.SQLConnectionProperties);
				mySQLTransactionJournal.JournalLoadingPercentChanged += delegate(object sender, JournalLoadingPercentChangedEventArgs args)
				{
					ConsoleEx.WriteBar(args);
				};
				runningJournal = mySQLTransactionJournal;
			}
			RunningJournal = runningJournal;
			if (!RunningJournal.LoadJournal())
			{
				return false;
			}
			return true;
		}

		internal async Task<IBankAccount> CreatePlayerAccountAsync(TSPlayer player)
		{

			IBankAccount newAccount = SEconomyPlugin.Instance.RunningJournal.AddBankAccount(player.Name, Main.worldID, BankAccountFlags.Enabled, "");
			TShock.Log.ConsoleInfo("seconomy: bank account for " + player.Name + " created.");
			if (int.TryParse(SEconomyPlugin.Instance.Configuration.StartingMoney, out var Money) && (long)Money > 0)
			{
				await SEconomyPlugin.Instance.WorldAccount.TransferToAsync(newAccount, Money, BankAccountTransferOptions.AnnounceToReceiver, "starting out.", "starting out.");
			}
			return newAccount;
		}

		public async Task BindToWorldAsync()
		{
			IBankAccount bankAccount = (WorldAccount = RunningJournal.GetWorldAccount());
			IBankAccount bankAccount2 = bankAccount;
			if (bankAccount2 == null)
			{
				TShock.Log.ConsoleError("seconomy bind:  The journal system did not return a world account.  This is an internal error.");
				return;
			}
			await WorldAccount.SyncBalanceAsync();
			TShock.Log.ConsoleInfo(string.Format(SEconomyPlugin.Locale.StringOrDefault(1, "SEconomy: world account: paid {0} to players."), WorldAccount.Balance.ToLongString()));
			await Task.Delay(5000);
			TSPlayer[] players = TShock.Players;
			TSPlayer[] array = players;
			foreach (TSPlayer tSPlayer in array)
			{
				if (tSPlayer != null && !string.IsNullOrWhiteSpace(tSPlayer.Name))
				{
					IBankAccount bankAccount3 = (bankAccount2 = GetBankAccount(tSPlayer));
					if (bankAccount2 != null)
					{
						await bankAccount3.SyncBalanceAsync();
					}
				}
			}
		}

		public void RemovePlayerIdleCache(Player player)
		{
			if (IdleCache != null && player != null && IdleCache.ContainsKey(player))
			{
				IdleCache.Remove(player);
			}
		}

		public TimeSpan? PlayerIdleSince(Player player)
		{
			if (player == null || IdleCache == null || !IdleCache.ContainsKey(player))
			{
				return null;
			}
			return DateTime.UtcNow.Subtract(IdleCache[player]);
		}

		public void UpdatePlayerIdle(Player player)
		{
			if (player != null && IdleCache != null)
			{
				if (!IdleCache.ContainsKey(player))
				{
					IdleCache.Add(player, DateTime.UtcNow);
				}
				IdleCache[player] = DateTime.UtcNow;
			}
		}

		public int PurgeAccounts()
		{
			return 0;
		}

		public IBankAccount GetBankAccount(TSPlayer tsPlayer)
		{
			if (tsPlayer == null || RunningJournal == null)
			{
				return null;
			}
			if (tsPlayer == TSPlayer.Server)
			{
				return WorldAccount;
			}
			try
			{
				return RunningJournal.GetBankAccountByName(tsPlayer.Name);
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError("seconomy error: Error getting bank account for {0}: {1}", tsPlayer.Name, ex.Message);
				return null;
			}
		}

		public IBankAccount GetBankAccount(Player player)
		{
			return GetBankAccount(player.whoAmI);
		}

		public IBankAccount GetBankAccount(string userAccountName)
		{
			return GetBankAccount(TShock.Players.FirstOrDefault((TSPlayer i) => i != null && i.Account.Name == userAccountName));
		}

		public IBankAccount GetPlayerBankAccount(string playerName)
		{
			return GetBankAccount(TShock.Players.FirstOrDefault((TSPlayer i) => i != null && i.Name == playerName));
		}

		public Task<List<KeyValuePair<TSPlayer, IBankAccount>>> SearchPlayerBankAccountAsync(string playerName)
		{
			return Task.Run(() => SearchPlayerBankAccount(playerName));
		}

		public List<KeyValuePair<TSPlayer, IBankAccount>> SearchPlayerBankAccount(string playerName)
		{
			List<KeyValuePair<TSPlayer, IBankAccount>> list = new List<KeyValuePair<TSPlayer, IBankAccount>>();
			TSPlayer[] players = TShock.Players;
			TSPlayer[] array = players;
			foreach (TSPlayer tSPlayer in array)
			{
				IBankAccount bankAccount;
				if (tSPlayer != null && (bankAccount = GetBankAccount(tSPlayer)) != null)
				{
					list.Add(new KeyValuePair<TSPlayer, IBankAccount>(tSPlayer, bankAccount));
				}
			}
			return list;
		}

		public IBankAccount GetBankAccount(int who)
		{
			if (who >= 0)
			{
				return GetBankAccount(TShock.Players.FirstOrDefault((TSPlayer i) => i != null && i.Index == who));
			}
			return GetBankAccount(TSPlayer.Server);
		}

		[Obsolete("Use GetBankAccount() instead.", true)]
		public object GetEconomyPlayerByBankAccountNameSafe(string Name)
		{
			throw new NotSupportedException("Use GetBankAccount() instead.");
		}

		[Obsolete("Use GetBankAccount() instead.", true)]
		public object GetEconomyPlayerSafe(int Id)
		{
			throw new NotSupportedException("Use GetBankAccount() instead.");
		}

		[Obsolete("Use GetBankAccount() instead.", true)]
		public object GetEconomyPlayerSafe(string Name)
		{
			throw new NotSupportedException("Use GetBankAccount() instead.");
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
				if (EventHandlers != null)
				{
					EventHandlers.Dispose();
				}
				if (ChatCommands != null)
				{
					ChatCommands.Dispose();
				}
				if (WorldEc != null)
				{
					WorldEc.Dispose();
				}
				if (TransactionCache != null)
				{
					TransactionCache.Dispose();
				}
				if (RunningJournal != null)
				{
					RunningJournal.Dispose();
				}
				Configuration = null;
			}
		}
	}
}
