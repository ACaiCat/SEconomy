using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TShockAPI;
using Wolfje.Plugins.SEconomy.CmdAliasModule.Extensions;
using Wolfje.Plugins.SEconomy.Journal;

namespace Wolfje.Plugins.SEconomy.CmdAliasModule
{
	public class CmdAlias : IDisposable
	{
		protected object __rndLock = new object();

		protected readonly Random randomGenerator = new Random();

		protected CmdAliasPlugin parent;

		protected readonly Regex parameterRegex = new Regex("\\$(\\d)(-(\\d)?)?");

		protected readonly Regex randomRegex = new Regex("\\$random\\((\\d*),(\\d*)\\)", RegexOptions.IgnoreCase);

		protected readonly Regex runasFunctionRegex = new Regex("(\\$runas\\((.*?),(.*?)\\)$)", RegexOptions.IgnoreCase);

		protected readonly Regex msgRegex = new Regex("(\\$msg\\((.*?),(.*?)\\)$)", RegexOptions.IgnoreCase);

		public readonly Dictionary<KeyValuePair<string, AliasCommand>, DateTime> CooldownList = new Dictionary<KeyValuePair<string, AliasCommand>, DateTime>();

		public Configuration Configuration { get; protected set; }

		public event EventHandler<AliasExecutedEventArgs> AliasExecuted;

		public CmdAlias(CmdAliasPlugin pluginInstance)
		{
			parent = pluginInstance;
			Commands.ChatCommands.Add(new Command("aliascmd", ChatCommand_GeneralCommand, "aliascmd")
			{
				AllowServer = true
			});
			Configuration = Configuration.LoadConfigurationFromFile("tshock" + Path.DirectorySeparatorChar + "SEconomy" + Path.DirectorySeparatorChar + "AliasCmd.config.json");
			ParseCommands();
			AliasExecuted += CmdAliasPlugin_AliasExecuted;
		}

		protected async void ChatCommand_GeneralCommand(CommandArgs args)
		{
			if (args.Parameters.Count >= 1 && args.Parameters[0].Equals("reload", StringComparison.CurrentCultureIgnoreCase) && args.Player.Group.HasPermission("aliascmd.reloadconfig"))
			{
				args.Player.SendInfoMessage("aliascmd: Reloading configuration file.");
				try
				{
					await ReloadConfigAfterDelayAsync(1);
					args.Player.SendInfoMessage("aliascmd: reloading complete.");
					return;
				}
				catch (Exception ex)
				{
					args.Player.SendErrorMessage("aliascmd: reload failed.  You need to check the server console to find out what went wrong.");
					TShock.Log.ConsoleError("aliascmd reload: Cannot load configuration: {0}", ex.Message);
					return;
				}
			}
			args.Player.SendErrorMessage("aliascmd: usage: /aliascmd reload: reloads the AliasCmd configuration file.");
		}

		protected async Task ReloadConfigAfterDelayAsync(int DelaySeconds)
		{
			await Task.Delay(DelaySeconds * 1000);
			TShock.Log.ConsoleInfo("AliasCmd: reloading config.");
			try
			{
				Configuration = Configuration.LoadConfigurationFromFile("tshock" + Path.DirectorySeparatorChar + "SEconomy" + Path.DirectorySeparatorChar + "AliasCmd.config.json");
				ParseCommands();
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError("aliascmd: Your new config could not be loaded, fix any problems and save the file.  Your old configuration is in effect until this is fixed. \r\n\r\n" + ex.ToString());
				throw;
			}
			TShock.Log.ConsoleInfo("AliasCmd: config reload done.");
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

		protected void CmdAliasPlugin_AliasExecuted(object sender, AliasExecutedEventArgs e)
		{
			foreach (AliasCommand item in Configuration.CommandAliases.Where((AliasCommand i) => i.CommandAlias == e.CommandIdentifier))
			{
				DateTime dateTime = DateTime.MinValue;
				Money Money = 0L;
				if (item == null || SEconomyPlugin.Instance == null)
				{
					continue;
				}
				KeyValuePair<string, AliasCommand> keyValuePair = new KeyValuePair<string, AliasCommand>(e.CommandArgs.Player.Name, item);
				if (CooldownList.ContainsKey(keyValuePair))
				{
					dateTime = CooldownList[keyValuePair];
				}
				if (DateTime.UtcNow <= dateTime && !e.CommandArgs.Player.Group.HasPermission("aliascmd.bypasscooldown"))
				{
					e.CommandArgs.Player.SendErrorMessage("{0}: You need to wait {1:0} more seconds to be able to use that.", item.CommandAlias, dateTime.Subtract(DateTime.UtcNow).TotalSeconds);
					break;
				}
				if (string.IsNullOrEmpty(item.Cost) || e.CommandArgs.Player.Group.HasPermission("aliascmd.bypasscost") || !Money.TryParse(item.Cost, out Money) || (long)Money == 0L)
				{
					DoCommands(item, e.CommandArgs.Player, e.CommandArgs.Parameters);
					PopulateCooldownList(keyValuePair);
					break;
				}
				IBankAccount bankAccount;
				if ((bankAccount = SEconomyPlugin.Instance.GetBankAccount(e.CommandArgs.Player)) == null)
				{
					e.CommandArgs.Player.SendErrorMessage("This command costs money and you don't have a bank account.  Please log in first.");
					break;
				}
				if (!bankAccount.IsAccountEnabled)
				{
					e.CommandArgs.Player.SendErrorMessage("This command costs money and your account is disabled.");
					break;
				}
				if ((long)bankAccount.Balance < (long)Money)
				{
					Money money = (long)Money - (long)bankAccount.Balance;
					e.CommandArgs.Player.SendErrorMessage("This command costs {0}. You need {1} more to be able to use this.", Money.ToLongString(), money.ToLongString());
				}
				try
				{
					if (bankAccount.TransferTo(SEconomyPlugin.Instance.WorldAccount, Money, BankAccountTransferOptions.AnnounceToSender | BankAccountTransferOptions.IsPayment, "", "AC: " + e.CommandArgs.Player.Name + " cmd " + item.CommandAlias).TransferSucceeded)
					{
						DoCommands(item, e.CommandArgs.Player, e.CommandArgs.Parameters);
						PopulateCooldownList(keyValuePair);
						break;
					}
					e.CommandArgs.Player.SendErrorMessage("Your payment failed.");
				}
				catch (Exception ex)
				{
					e.CommandArgs.Player.SendErrorMessage("An error occured in the alias.");
					TShock.Log.ConsoleError("aliascmd error: {0} tried to execute alias {1} which failed with error {2}: {3}", e.CommandArgs.Player.Name, e.CommandIdentifier, ex.Message, ex.ToString());
					break;
				}
			}
		}

		protected void ParseCommands()
		{
			Commands.ChatCommands.RemoveAll((Command i) => i.Names.Count((string x) => x.StartsWith("cmdalias.")) > 0);
			foreach (AliasCommand commandAlias in Configuration.CommandAliases)
			{
				Command item = new Command(commandAlias.Permissions, ChatCommand_AliasExecuted, commandAlias.CommandAlias, "cmdalias." + commandAlias.CommandAlias)
				{
					AllowServer = true
				};
				Commands.ChatCommands.Add(item);
			}
		}

		protected void ReplaceParameterMarkers(IList<string> parameters, ref string CommandToExecute)
		{
			if (!parameterRegex.IsMatch(CommandToExecute))
			{
				return;
			}
			foreach (Match item in parameterRegex.Matches(CommandToExecute))
			{
				int num = ((!string.IsNullOrEmpty(item.Groups[1].Value)) ? int.Parse(item.Groups[1].Value) : 0);
				int num2 = ((!string.IsNullOrEmpty(item.Groups[3].Value)) ? int.Parse(item.Groups[3].Value) : 0);
				bool flag = !string.IsNullOrEmpty(item.Groups[2].Value);
				StringBuilder stringBuilder = new StringBuilder();
				if (!flag && num > 0)
				{
					if (num <= parameters.Count)
					{
						stringBuilder.Append(parameters[num - 1]);
					}
					else
					{
						stringBuilder.Append("");
					}
				}
				else if (flag && num2 > num)
				{
					for (int i = num; i <= num2; i++)
					{
						if (parameters.Count >= i)
						{
							stringBuilder.Append(" " + parameters[i - 1]);
						}
					}
				}
				else if (flag && num2 == 0)
				{
					for (int j = num; j <= parameters.Count; j++)
					{
						stringBuilder.Append(" " + parameters[j - 1]);
					}
				}
				else
				{
					stringBuilder.Append("");
				}
				CommandToExecute = CommandToExecute.Replace(item.ToString(), stringBuilder.ToString());
			}
		}

		protected void DoCommands(AliasCommand alias, TSPlayer player, List<string> parameters)
		{
			foreach (string item in alias.CommandsToExecute)
			{
				string CommandToExecute = item;
				bool flag = true;
				ReplaceParameterMarkers(parameters, ref CommandToExecute);
				CommandToExecute = CommandToExecute.Replace("$calleraccount", player.Account.Name);
				CommandToExecute = CommandToExecute.Replace("$callername", player.Name);
				if (randomRegex.IsMatch(CommandToExecute))
				{
					foreach (Match item2 in randomRegex.Matches(CommandToExecute))
					{
						int result = 0;
						int result2 = 0;
						if (!string.IsNullOrEmpty(item2.Groups[2].Value) && int.TryParse(item2.Groups[2].Value, out result2) && !string.IsNullOrEmpty(item2.Groups[1].Value) && int.TryParse(item2.Groups[1].Value, out result))
						{
							lock (__rndLock)
							{
								CommandToExecute = CommandToExecute.Replace(item2.ToString(), randomGenerator.Next(result, result2).ToString());
							}
						}
						else
						{
							TShock.Log.ConsoleError(item2.ToString() + " has some stupid shit in it, have a look at your AliasCmd config file.");
							CommandToExecute = CommandToExecute.Replace(item2.ToString(), "");
						}
					}
				}
				if (runasFunctionRegex.IsMatch(CommandToExecute))
				{
					foreach (Match item3 in runasFunctionRegex.Matches(CommandToExecute))
					{
						string impersonatedName = item3.Groups[2].Value;
						TSPlayer tSPlayer = TShock.Players.FirstOrDefault((TSPlayer i) => i != null && i.Name == impersonatedName);
						if (tSPlayer != null)
						{
							string value = item3.Groups[3].Value;
							player = tSPlayer;
							CommandToExecute = value.Trim();
						}
					}
				}
				if (msgRegex.IsMatch(CommandToExecute))
				{
					foreach (Match item4 in msgRegex.Matches(CommandToExecute))
					{
						string msgTarget = item4.Groups[2].Value.Trim();
						string msg = item4.Groups[3].Value.Trim();
						TSPlayer tSPlayer2 = TShock.Players.FirstOrDefault((TSPlayer i) => i != null && i.Name == msgTarget);
						if (tSPlayer2 != null)
						{
							flag = false;
							tSPlayer2.SendInfoMessage(msg);
						}
					}
				}
				try
				{
					if (!CommandToExecute.Split(' ')[0].Substring(1).Equals(alias.CommandAlias, StringComparison.CurrentCultureIgnoreCase))
					{
						if (flag)
						{
							player.PermissionlessInvoke(CommandToExecute);
						}
					}
					else
					{
						TShock.Log.ConsoleError("cmdalias " + alias.CommandAlias + ": calling yourself in an alias will cause an infinite loop. Ignoring.");
					}
				}
				catch
				{
					player.SendErrorMessage(alias.UsageHelpText);
				}
			}
		}

		protected void ChatCommand_AliasExecuted(CommandArgs e)
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
				this.AliasExecuted(null, e2);
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			Commands.ChatCommands.RemoveAll((Command i) => i.Names.Count((string x) => x.StartsWith("cmdalias.")) > 0);
			AliasExecuted -= CmdAliasPlugin_AliasExecuted;
		}
	}
}
