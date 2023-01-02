using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Wolfje.Plugins.SEconomy.Configuration.WorldConfiguration;
using Wolfje.Plugins.SEconomy.Journal;

namespace Wolfje.Plugins.SEconomy
{
	public class EventHandlers : IDisposable
	{
		public SEconomy Parent { get; protected set; }

		protected System.Timers.Timer PayRunTimer { get; set; }

		public EventHandlers(SEconomy Parent)
		{
			this.Parent = Parent;
			if (Parent.Configuration.PayIntervalMinutes > 0)
			{
				PayRunTimer = new System.Timers.Timer(Parent.Configuration.PayIntervalMinutes * 60000);
				PayRunTimer.Elapsed += PayRunTimer_Elapsed;
				PayRunTimer.Start();
			}
			PlayerHooks.PlayerPostLogin += PlayerHooks_PlayerPostLogin;
			Parent.RunningJournal.BankTransferCompleted += BankAccount_BankTransferCompleted;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
			ServerApi.Hooks.GamePostInitialize.Register(this.Parent.PluginInstance, GameHooks_PostInitialize);
			ServerApi.Hooks.ServerJoin.Register(this.Parent.PluginInstance, ServerHooks_Join);
			ServerApi.Hooks.ServerLeave.Register(this.Parent.PluginInstance, ServerHooks_Leave);
			ServerApi.Hooks.NetGetData.Register(this.Parent.PluginInstance, NetHooks_GetData);
		}

		protected void BankAccount_BankTransferCompleted(object s, BankTransferEventArgs e)
		{
			if (e.ReceiverAccount == null || (e.TransferOptions & BankAccountTransferOptions.SuppressDefaultAnnounceMessages) == BankAccountTransferOptions.SuppressDefaultAnnounceMessages)
			{
				return;
			}
			TSPlayer tSPlayer = TShock.Players.FirstOrDefault((TSPlayer i) => i != null && i.Name == e.SenderAccount.UserAccountName);
			TSPlayer tSPlayer2 = TShock.Players.FirstOrDefault((TSPlayer i) => i != null && i.Name == e.ReceiverAccount.UserAccountName);
			WorldConfig worldConfiguration = Parent.WorldEc.WorldConfiguration;
			int packedValue = (int)new Color(worldConfiguration.OverheadColor[0], worldConfiguration.OverheadColor[1], worldConfiguration.OverheadColor[2]).PackedValue;
			if ((e.TransferOptions & BankAccountTransferOptions.AnnounceToReceiver) == BankAccountTransferOptions.AnnounceToReceiver && e.ReceiverAccount != null && tSPlayer2 != null)
			{
				bool flag = (long)e.Amount > 0 && (e.TransferOptions & BankAccountTransferOptions.IsPayment) == 0;
				if (worldConfiguration.ShowKillGainsDetailed)
				{
					string text = string.Format("\n\n\n\n\n\r\n\r\n{5}RPG系统\r\n击杀获得{1}\r\n总共拥有:{3}{4}", flag ? "+" : "", e.Amount.ToString(), "for " + e.TransactionMessage, e.ReceiverAccount.Balance.ToString(), RepeatLineBreaks(59), RepeatLineBreaks(11));
					tSPlayer2.SendData(PacketTypes.Status, text);
				}
				if (worldConfiguration.ShowKillGainsOverhead)
				{
					tSPlayer2.SendData(PacketTypes.CreateCombatTextExtended, (flag ? "+" : "-") + e.Amount.ToString(), packedValue, tSPlayer2.X, tSPlayer2.Y);
				}
			}
			if ((e.TransferOptions & BankAccountTransferOptions.AnnounceToSender) == BankAccountTransferOptions.AnnounceToSender && tSPlayer != null)
			{
				bool flag2 = false;
				if (worldConfiguration.ShowKillGainsDetailed)
				{
					string text2 = string.Format("\n\n\n\n\n\r\n\r\n{5}SE经济系统\r\n你被杀死{0}{1}\r\n总共拥有:{3}{4}", flag2 ? "+" : "-", e.Amount.ToString(), "for " + e.TransactionMessage, e.SenderAccount.Balance.ToString(), RepeatLineBreaks(59), RepeatLineBreaks(11));
					tSPlayer.SendData(PacketTypes.Status, text2);
				}
				if (worldConfiguration.ShowKillGainsOverhead)
				{
					tSPlayer.SendData(PacketTypes.CreateCombatTextExtended, (flag2 ? "+" : "-") + e.Amount.ToString(), packedValue, tSPlayer.X, tSPlayer.Y);
				}
			}
		}

		protected string RepeatLineBreaks(int number)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < number; i++)
			{
				stringBuilder.Append("\r\n");
			}
			return stringBuilder.ToString();
		}

		protected void NetHooks_GetData(GetDataEventArgs args)
		{
			Player player;
			if (args.MsgID == PacketTypes.PlayerUpdate && args.Msg.readBuffer[args.Index] >= 0 && (player = Main.player.ElementAtOrDefault(args.Msg.whoAmI)) != null && args.Msg.readBuffer[args.Index + 1] != 0)
			{
				Parent.UpdatePlayerIdle(player);
			}
		}

		protected void ServerHooks_Leave(LeaveEventArgs args)
		{
			Parent.RemovePlayerIdleCache(Main.player.ElementAtOrDefault(args.Who));
		}

		protected void ServerHooks_Join(JoinEventArgs args)
		{
		}

		protected async void GameHooks_PostInitialize(EventArgs e)
		{
			await Parent.BindToWorldAsync();
		}

		protected async void PlayerHooks_PlayerPostLogin(PlayerPostLoginEventArgs e)
		{
			await Task.Delay(2500);
			IBankAccount bankAccount;
			IBankAccount account = (bankAccount = Parent.GetBankAccount(e.Player));
			if (bankAccount == null)
			{
				if (await Parent.CreatePlayerAccountAsync(e.Player) == null)
				{
					TShock.Log.ConsoleError("seconomy error:  Creating account for {0} failed.", e.Player.Name);
				}
			}
			else
			{
				await account.SyncBalanceAsync();
				e.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(26, "你目前拥有{0},请努力升级吧！"), account.Balance.ToLongString());
			}
		}

		protected void PayRunTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			Money Money = 0L;
			if (Parent.Configuration.PayIntervalMinutes <= 0 || string.IsNullOrEmpty(Parent.Configuration.IntervalPayAmount) || !Money.TryParse(Parent.Configuration.IntervalPayAmount, out Money) || (long)Money <= 0)
			{
				return;
			}
			TSPlayer[] players = TShock.Players;
			TSPlayer[] array = players;
			foreach (TSPlayer tSPlayer in array)
			{
				if (tSPlayer != null && Parent != null)
				{
					TimeSpan? timeSpan;
					TimeSpan? timeSpan2 = (timeSpan = Parent.PlayerIdleSince(tSPlayer.TPlayer));
					IBankAccount bankAccount;
					if (timeSpan2.HasValue && !(timeSpan.Value.TotalMinutes > (double)Parent.Configuration.IdleThresholdMinutes) && (bankAccount = Parent.GetBankAccount(tSPlayer)) != null)
					{
						Parent.WorldAccount.TransferTo(bankAccount, Money, BankAccountTransferOptions.AnnounceToReceiver, "being awesome", "being awesome");
					}
				}
			}
		}

		protected void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
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
				PlayerHooks.PlayerPostLogin -= PlayerHooks_PlayerPostLogin;
				Parent.RunningJournal.BankTransferCompleted -= BankAccount_BankTransferCompleted;
				TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
				PayRunTimer.Elapsed -= PayRunTimer_Elapsed;
				PayRunTimer.Dispose();
				ServerApi.Hooks.GamePostInitialize.Deregister(Parent.PluginInstance, GameHooks_PostInitialize);
				ServerApi.Hooks.ServerJoin.Deregister(Parent.PluginInstance, ServerHooks_Join);
				ServerApi.Hooks.ServerLeave.Deregister(Parent.PluginInstance, ServerHooks_Leave);
				ServerApi.Hooks.NetGetData.Deregister(Parent.PluginInstance, NetHooks_GetData);
			}
		}
	}
}
