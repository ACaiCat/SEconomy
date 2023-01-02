using System;
using System.Reflection;
using Jint.Native;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Wolfje.Plugins.Jist;
using Wolfje.Plugins.Jist.Framework;
using Wolfje.Plugins.SEconomy.Journal;

namespace Wolfje.Plugins.SEconomy.SEconomyScriptPlugin
{
	[ApiVersion(2, 1)]
	public class SEconomyScriptPlugin : TerrariaPlugin
	{
		public override string Author => "Wolfje";

		public override string Description => "Provides SEconomy scripting support to Jist";

		public override string Name => "SEconomy Jist Support";

		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		public SEconomyScriptPlugin(Main game)
			: base(game)
		{
		}

		public override void Initialize()
		{
			JistPlugin.JavascriptFunctionsNeeded += JistPlugin_JavascriptFunctionsNeeded;
		}

		private void JistPlugin_JavascriptFunctionsNeeded(object sender, JavascriptFunctionsNeededEventArgs e)
		{
			e.Engine.CreateScriptFunctions(GetType(), this);
		}

		[JavascriptFunction(new string[] { "seconomy_transfer_async" })]
		public async void SEconomyTransferAsync(IBankAccount From, IBankAccount To, Money Amount, string TxMessage, JsValue completedCallback)
		{
			if (JistPlugin.Instance != null && SEconomyPlugin.Instance != null && From != null && To != null)
			{
				BankTransferEventArgs bankTransferEventArgs = await From.TransferToAsync(To, Amount, BankAccountTransferOptions.AnnounceToSender, TxMessage, TxMessage);
				if (bankTransferEventArgs == null)
				{
					bankTransferEventArgs = new BankTransferEventArgs
					{
						TransferSucceeded = false
					};
				}
				JistPlugin.Instance.CallFunction(completedCallback, null, bankTransferEventArgs);
			}
		}

		[JavascriptFunction(new string[] { "seconomy_pay_async" })]
		public async void SEconomyPayAsync(IBankAccount From, IBankAccount To, Money Amount, string TxMessage, JsValue completedCallback)
		{
			if (JistPlugin.Instance != null && SEconomyPlugin.Instance != null && From != null && To != null)
			{
				BankTransferEventArgs bankTransferEventArgs = await From.TransferToAsync(To, Amount, BankAccountTransferOptions.AnnounceToReceiver | BankAccountTransferOptions.AnnounceToSender | BankAccountTransferOptions.IsPayment, TxMessage, TxMessage);
				if (bankTransferEventArgs == null)
				{
					bankTransferEventArgs = new BankTransferEventArgs
					{
						TransferSucceeded = false
					};
				}
				JistPlugin.Instance.CallFunction(completedCallback, null, bankTransferEventArgs);
			}
		}

		[JavascriptFunction(new string[] { "seconomy_parse_money" })]
		public Money SEconomyParseMoney(object MoneyRep)
		{
			try
			{
				return Money.Parse(MoneyRep.ToString());
			}
			catch
			{
				return 0L;
			}
		}

		[JavascriptFunction(new string[] { "seconomy_valid_money" })]
		public bool SEconomyMoneyValid(object MoneyRep)
		{
			Money Money;
			return Money.TryParse(MoneyRep.ToString(), out Money);
		}

		[JavascriptFunction(new string[] { "seconomy_get_offline_account" })]
		public IBankAccount GetBankAccountOffline(object accountRef)
		{
			if (JistPlugin.Instance == null || SEconomyPlugin.Instance == null)
			{
				return null;
			}
			if (accountRef is double)
			{
				return SEconomyPlugin.Instance.RunningJournal.GetBankAccount(Convert.ToInt64((double)accountRef));
			}
			if (accountRef is string)
			{
				return SEconomyPlugin.Instance.RunningJournal.GetBankAccountByName(accountRef as string);
			}
			return null;
		}

		[JavascriptFunction(new string[] { "seconomy_get_account" })]
		public IBankAccount GetBankAccount(object PlayerRep)
		{
			IBankAccount bankAccount = null;
			TSPlayer tSPlayer = null;
			if (JistPlugin.Instance == null || SEconomyPlugin.Instance == null || (tSPlayer = JistPlugin.Instance.stdTshock.GetPlayer(PlayerRep)) == null || (bankAccount = SEconomyPlugin.Instance.GetBankAccount(tSPlayer)) == null)
			{
				return null;
			}
			return bankAccount;
		}

		[JavascriptFunction(new string[] { "seconomy_set_multiplier" })]
		public bool SetMultiplier(int multi)
		{
			if (SEconomyPlugin.Instance == null)
			{
				return false;
			}
			SEconomyPlugin.Instance.WorldEc.CustomMultiplier = multi;
			return true;
		}

		[JavascriptFunction(new string[] { "seconomy_get_multiplier" })]
		public int GetMultiplier()
		{
			if (SEconomyPlugin.Instance == null)
			{
				return -1;
			}
			return SEconomyPlugin.Instance.WorldEc.CustomMultiplier;
		}

		[JavascriptFunction(new string[] { "seconomy_world_account" })]
		public IBankAccount WorldAccount()
		{
			if (SEconomyPlugin.Instance == null)
			{
				return null;
			}
			return SEconomyPlugin.Instance.WorldAccount;
		}
	}
}
