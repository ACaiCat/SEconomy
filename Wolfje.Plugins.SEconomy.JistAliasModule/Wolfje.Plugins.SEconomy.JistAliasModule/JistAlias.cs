using System;
using System.Collections.Generic;
using System.Linq;
using TShockAPI;
using Wolfje.Plugins.Jist;
using Wolfje.Plugins.SEconomy.CmdAliasModule;
using Wolfje.Plugins.SEconomy.JistAliasModule.AliasLib;
using Wolfje.Plugins.SEconomy.Journal;

namespace Wolfje.Plugins.SEconomy.JistAliasModule
{
	public class JistAlias : IDisposable
	{
		protected readonly JistAliasPlugin parent;

		protected readonly List<JScriptAliasCommand> jsAliases;

		internal stdalias stdAlias;

		internal readonly Dictionary<KeyValuePair<string, AliasCommand>, DateTime> CooldownList = new Dictionary<KeyValuePair<string, AliasCommand>, DateTime>();

		internal event EventHandler<AliasExecutedEventArgs> AliasExecuted;

		public JistAlias(JistAliasPlugin plugin)
		{
			jsAliases = new List<JScriptAliasCommand>();
			parent = plugin;
			AliasExecuted += JistAlias_AliasExecuted;
			stdAlias = new stdalias(JistPlugin.Instance, this);
		}

		public void ParseJSCommands()
		{
			lock (jsAliases)
			{
				foreach (JScriptAliasCommand jsAlias in jsAliases)
				{
					Command item = new Command(jsAlias.Permissions, ChatCommand_AliasExecuted, jsAlias.CommandAlias, "jistalias." + jsAlias.CommandAlias)
					{
						AllowServer = true
					};
					Commands.ChatCommands.Add(item);
				}
			}
		}

		internal void ChatCommand_AliasExecuted(CommandArgs e)
		{
			string commandIdentifier = e.Message;
			if (!string.IsNullOrEmpty(e.Message))
			{
				commandIdentifier = e.Message.Split(' ').FirstOrDefault();
			}
			if (this.AliasExecuted != null)
			{
				AliasExecutedEventArgs e2 = new AliasExecutedEventArgs
				{
					CommandIdentifier = commandIdentifier,
					CommandArgs = e
				};
				this.AliasExecuted(this, e2);
			}
		}

		internal void PopulateCooldownList(KeyValuePair<string, AliasCommand> cooldownReference, TimeSpan? customValue = null)
		{
			DateTime value = DateTime.UtcNow.Add(TimeSpan.FromSeconds(cooldownReference.Value.CooldownSeconds));
			if (customValue.HasValue)
			{
				DateTime.UtcNow.Add(customValue.Value);
			}
			if (CooldownList.ContainsKey(cooldownReference))
			{
				CooldownList[cooldownReference] = value;
			}
			else
			{
				CooldownList.Add(cooldownReference, value);
			}
		}

		internal JScriptAliasCommand GetAlias(object aliasObject)
		{
			string aliasName = null;
			if (aliasObject == null)
			{
				return null;
			}
			if (aliasObject is JScriptAliasCommand)
			{
				return aliasObject as JScriptAliasCommand;
			}
			if (aliasObject is string)
			{
				aliasName = aliasObject as string;
				return jsAliases.FirstOrDefault((JScriptAliasCommand i) => i.CommandAlias == aliasName);
			}
			return null;
		}

		internal void CreateAlias(JScriptAliasCommand alias, bool allowServer = true)
		{
			Command command = new Command(alias.Permissions, ChatCommand_AliasExecuted, alias.CommandAlias, "jistalias." + alias.CommandAlias)
			{
				AllowServer = allowServer
			};
			command.DoLog = !alias.Silent;
			Commands.ChatCommands.RemoveAll((Command i) => i.Names.Contains("jistalias." + alias.CommandAlias));
			Commands.ChatCommands.Add(command);
			jsAliases.RemoveAll((JScriptAliasCommand i) => i.CommandAlias == alias.CommandAlias);
			jsAliases.Add(alias);
		}

		internal void RemoveAlias(JScriptAliasCommand alias)
		{
			jsAliases.RemoveAll((JScriptAliasCommand i) => i.CommandAlias == alias.CommandAlias);
			Commands.ChatCommands.RemoveAll((Command i) => i.Names.Contains("jistalias." + alias.CommandAlias));
		}

		internal void RefundAlias(Money commandCost, TSPlayer toPlayer)
		{
			if ((long)commandCost != 0L && toPlayer != null)
			{
				_ = SEconomyPlugin.Instance;
			}
		}

		internal async void JistAlias_AliasExecuted(object sender, AliasExecutedEventArgs e)
		{
			foreach (JScriptAliasCommand alias in jsAliases.Where((JScriptAliasCommand i) => i.CommandAlias == e.CommandIdentifier))
			{
				DateTime dateTime = DateTime.MinValue;
				Money commandCost = 0L;
				if (alias == null)
				{
					continue;
				}
				KeyValuePair<string, AliasCommand> cooldownReference = new KeyValuePair<string, AliasCommand>(e.CommandArgs.Player.Name, alias);
				if (CooldownList.ContainsKey(cooldownReference))
				{
					dateTime = CooldownList[cooldownReference];
				}
				if (DateTime.UtcNow <= dateTime && !e.CommandArgs.Player.Group.HasPermission("aliascmd.bypasscooldown"))
				{
					e.CommandArgs.Player.SendErrorMessage("{0}: You need to wait {1:0} more seconds to be able to use that.", alias.CommandAlias, dateTime.Subtract(DateTime.UtcNow).TotalSeconds);
					return;
				}
				if (string.IsNullOrEmpty(alias.Cost) || e.CommandArgs.Player.Group.HasPermission("aliascmd.bypasscost") || !Money.TryParse(alias.Cost, out commandCost) || (long)commandCost == 0L)
				{
					if (JistPlugin.Instance == null)
					{
						return;
					}
					try
					{
						PopulateCooldownList(cooldownReference);
						JistPlugin.Instance.CallFunction(alias.func, alias, e.CommandArgs.Player, e.CommandArgs.Parameters);
						return;
					}
					catch
					{
					}
				}
				if (SEconomyPlugin.Instance == null)
				{
					return;
				}
				IBankAccount bankAccount;
				if ((bankAccount = SEconomyPlugin.Instance.GetBankAccount(e.CommandArgs.Player)) == null)
				{
					e.CommandArgs.Player.SendErrorMessage("This command costs money and you don't have a bank account.  Please log in first.");
					return;
				}
				if (!bankAccount.IsAccountEnabled)
				{
					e.CommandArgs.Player.SendErrorMessage("This command costs money and your account is disabled.");
					return;
				}
				if ((long)bankAccount.Balance < (long)commandCost)
				{
					Money money = (long)commandCost - (long)bankAccount.Balance;
					e.CommandArgs.Player.SendErrorMessage("This command costs {0}. You need {1} more to be able to use this.", commandCost.ToLongString(), money.ToLongString());
				}
				try
				{
					if (!(await bankAccount.TransferToAsync(SEconomyPlugin.Instance.WorldAccount, commandCost, BankAccountTransferOptions.AnnounceToSender | BankAccountTransferOptions.IsPayment, "", "AC: " + e.CommandArgs.Player.Name + " cmd " + alias.CommandAlias)).TransferSucceeded)
					{
						e.CommandArgs.Player.SendErrorMessage("Your payment failed.");
						return;
					}
					if (JistPlugin.Instance == null)
					{
						return;
					}
					try
					{
						PopulateCooldownList(cooldownReference);
						JistPlugin.Instance.CallFunction(alias.func, alias, e.CommandArgs.Player.Name, e.CommandArgs.Parameters);
					}
					catch (Exception)
					{
						ScriptLog.ErrorFormat("alias", "{0} paid {1} for alias {2} but it failed and was refunded.", e.CommandArgs.Player.Name, commandCost.ToString(), alias.CommandAlias);
						RefundAlias(commandCost, e.CommandArgs.Player);
					}
				}
				catch (Exception ex2)
				{
					e.CommandArgs.Player.SendErrorMessage("An error occured in the alias.");
					TShock.Log.ConsoleError("aliascmd error: {0} tried to execute alias {1} which failed with error {2}: {3}", e.CommandArgs.Player.Name, e.CommandIdentifier, ex2.Message, ex2.ToString());
					return;
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
			if (!disposing)
			{
				return;
			}
			stdAlias.Dispose();
			Commands.ChatCommands.RemoveAll((Command i) => i.Names.Count((string x) => x.StartsWith("jistalias.")) > 0);
			AliasExecuted -= JistAlias_AliasExecuted;
		}
	}
}
