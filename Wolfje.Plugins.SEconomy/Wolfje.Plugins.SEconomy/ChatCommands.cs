using System;
using System.Linq;
using System.Threading;
using TShockAPI;
using Wolfje.Plugins.SEconomy.Journal;

namespace Wolfje.Plugins.SEconomy
{
	internal class ChatCommands : IDisposable
	{
		private SEconomy Parent { get; set; }

		internal ChatCommands(SEconomy parent)
		{
			Parent = parent;
			Commands.ChatCommands.Add(new Command(Chat_BankCommand, "bank")
			{
				AllowServer = true
			});
		}

		protected async void Chat_BankCommand(CommandArgs args)
		{
			IBankAccount selectedAccount = Parent.GetBankAccount(args.Player);
			IBankAccount callerAccount = Parent.GetBankAccount(args.Player);
			if (args.Parameters.Count == 0)
			{
				args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(230, "指令列表:"));
				if (args.Player.Group.HasPermission("bank.transfer"))
				{
					args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(32, "* 指令:/bank pay <玩家名称> <Exp数量> - 将自己的Exp给指定玩家"));
				}
				if (args.Player.Group.HasPermission("bank.viewothers"))
				{
					args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(33, "* 指令:/bank bal - 查看你自己的Exp数量"));
				}
				if (args.Player.Group.HasPermission("bank.worldtransfer"))
				{
					args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(34, "* 指令:/bank give|take <玩家> <数量> - 扣除或给予给一位玩家货币"));
				}
				if (args.Player.Group.HasPermission("bank.mgr"))
				{
					args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(35, "* 指令:/bank mgr - 启动图形界面管理功能"));
				}
				if (args.Player.Group.HasPermission("bank.savejournal"))
				{
					args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(36, "* 指令:/bank savejournal - 备份交易日志"));
				}
				if (args.Player.Group.HasPermission("bank.loadjournal"))
				{
					args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(37, "* 指令:/bank loadjournal - 从文件加载交易日志"));
				}
				if (args.Player.Group.HasPermission("bank.squashjournal"))
				{
					args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(38, "* 指令:/bank squashjournal - 压缩日志文件"));
				}
				if (args.Player.Group.HasPermission("bank.worldtransfer"))
				{
					args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(34, "* 指令:/sec - 查看关于SE的其他指令"));
				}
				return;
			}
			if (args.Parameters[0].Equals("reset", StringComparison.CurrentCultureIgnoreCase))
			{
				if (args.Player.Group.HasPermission("seconomy.reset"))
				{
					if (args.Parameters.Count >= 2 && !string.IsNullOrEmpty(args.Parameters[1]))
					{
						IBankAccount playerBankAccount = Parent.GetPlayerBankAccount(args.Parameters[1]);
						if (playerBankAccount != null)
						{
							args.Player.SendInfoMessage(string.Format(SEconomyPlugin.Locale.StringOrDefault(39, "seconomy reset: Resetting {0}'s account."), args.Parameters[1]));
							playerBankAccount.Transactions.Clear();
							await playerBankAccount.SyncBalanceAsync();
							args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(40, "seconomy reset:  Reset complete."));
						}
						else
						{
							args.Player.SendErrorMessage(string.Format(SEconomyPlugin.Locale.StringOrDefault(41, "seconomy reset: Cannot find player \"{0}\" or no bank account found."), args.Parameters[1]));
						}
					}
				}
				else
				{
					args.Player.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(42, "seconomy reset: You do not have permission to perform this command."));
				}
			}
			if (args.Parameters[0].Equals("bal", StringComparison.CurrentCultureIgnoreCase) || args.Parameters[0].Equals("您总共拥有", StringComparison.CurrentCultureIgnoreCase))
			{
				if (args.Player.Group.HasPermission("bank.viewothers") && args.Parameters.Count >= 2)
				{
					selectedAccount = Parent.GetPlayerBankAccount(args.Parameters[1]);
					_ = args.Parameters[1] + "'s";
				}
				if (selectedAccount != null)
				{
					if (!selectedAccount.IsAccountEnabled && !args.Player.Group.HasPermission("bank.viewothers"))
					{
						args.Player.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(43, "bank balance: your account is disabled"));
						return;
					}
					args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(44, "你目前拥有{0}Exp,请努力升级吧！"), selectedAccount.Balance.ToLongString(ShowNegativeSign: true), selectedAccount.IsAccountEnabled ? "" : SEconomyPlugin.Locale.StringOrDefault(45, "(disabled)"));
				}
				else
				{
					args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(46, "bank balance: Cannot find player or no bank account."));
				}
			}
			else if (args.Parameters[0].Equals("mgr"))
			{
				if (!args.Player.Group.HasPermission("bank.mgr"))
				{
					return;
				}
				if (args.Player is TSServerPlayer)
				{
					Thread thread = new Thread((ThreadStart)delegate
					{
						TShock.Log.ConsoleInfo(SEconomyPlugin.Locale.StringOrDefault(47, "seconomy mgr: opening bank manager window"));
						Parent.RunningJournal.BackupsEnabled = false;
						Parent.RunningJournal.BackupsEnabled = true;
						TShock.Log.ConsoleInfo(SEconomyPlugin.Locale.StringOrDefault(49, "seconomy management: window closed"));
					});
					thread.SetApartmentState(ApartmentState.STA);
					thread.Start();
				}
				else
				{
					args.Player.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(50, "请在控制台使用该指令"));
				}
			}
			else if (args.Parameters[0].Equals("savejournal"))
			{
				if (args.Player.Group.HasPermission("bank.savejournal"))
				{
					args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(51, "备份交易日志"));
					await Parent.RunningJournal.SaveJournalAsync();
				}
			}
			else if (args.Parameters[0].Equals("loadjournal"))
			{
				if (args.Player.Group.HasPermission("bank.loadjournal"))
				{
					args.Player.SendInfoMessage(SEconomyPlugin.Locale.StringOrDefault(52, "从文件加载交易日志"));
					await Parent.RunningJournal.LoadJournalAsync();
				}
			}
			else if (args.Parameters[0].Equals("squashjournal", StringComparison.CurrentCultureIgnoreCase))
			{
				if (args.Player.Group.HasPermission("bank.squashjournal"))
				{
					await Parent.RunningJournal.SquashJournalAsync();
					await Parent.RunningJournal.SaveJournalAsync();
				}
				else
				{
					args.Player.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(53, "您没有执行此命令的权限"));
				}
			}
			else if (args.Parameters[0].Equals("pay", StringComparison.CurrentCultureIgnoreCase) || args.Parameters[0].Equals("transfer", StringComparison.CurrentCultureIgnoreCase) || args.Parameters[0].Equals("tfr", StringComparison.CurrentCultureIgnoreCase))
			{
				if (args.Player.Group.HasPermission("bank.transfer"))
				{
					if (args.Parameters.Count >= 3)
					{
						selectedAccount = Parent.GetPlayerBankAccount(args.Parameters[1]);
						Money Money = 0L;
						if (selectedAccount == null)
						{
							args.Player.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(54, "找不到名为{0}的玩家,这是替身攻击!"), args.Parameters[1]);
						}
						else if (Money.TryParse(args.Parameters[2], out Money))
						{
							if (callerAccount == null)
							{
								args.Player.SendErrorMessage("交易失败,可能因为你输入的是错误的玩家名称");
								return;
							}
							await callerAccount.TransferToAsync(selectedAccount, Money, BankAccountTransferOptions.AnnounceToReceiver | BankAccountTransferOptions.AnnounceToSender | BankAccountTransferOptions.IsPlayerToPlayerTransfer, args.Player.Name + " >> " + args.Parameters[1], "SE: tfr: " + args.Player.Name + " to " + args.Parameters[1] + " for " + Money.ToString());
						}
						else
						{
							args.Player.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(55, "bank give: \"{0}\" isn't a valid amount of money."), args.Parameters[2]);
						}
					}
					else
					{
						args.Player.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(56, "正确的姿势: /bank pay [玩家] [Exp]"));
					}
				}
				else
				{
					args.Player.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(57, "你没有权限给其他玩家Exp"));
				}
			}
			else
			{
				if (!args.Parameters[0].Equals("give", StringComparison.CurrentCultureIgnoreCase) && !args.Parameters[0].Equals("take", StringComparison.CurrentCultureIgnoreCase))
				{
					return;
				}
				if (args.Player.Group.HasPermission("bank.worldtransfer"))
				{
					if (args.Parameters.Count >= 3)
					{
						selectedAccount = Parent.GetPlayerBankAccount(args.Parameters[1]);
						Money Money2 = 0L;
						if (selectedAccount == null)
						{
							args.Player.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(54, "找不到名为{0}的玩家"), args.Parameters[1]);
						}
						else if (Money.TryParse(args.Parameters[2], out Money2))
						{
							if (args.Parameters[0].Equals("take", StringComparison.CurrentCultureIgnoreCase) && (long)Money2 > 0)
							{
								Money2 = -(long)Money2;
							}
							Parent.WorldAccount.TransferTo(selectedAccount, Money2, BankAccountTransferOptions.AnnounceToReceiver, args.Parameters[0] + " command", "SE: pay: " + Money2.ToString() + " to " + args.Parameters[1] + " ");
						}
						else
						{
							args.Player.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(55, "\"{0}\" 不是有效的金额!"), args.Parameters[2]);
						}
					}
					else
					{
						args.Player.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(58, "使用方法:/bank give|take [玩家] [金额]"));
					}
				}
				else
				{
					args.Player.SendErrorMessage(SEconomyPlugin.Locale.StringOrDefault(57, "没有权限"));
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
				Command command = Commands.ChatCommands.FirstOrDefault((Command i) => i.Name == "bank" && i.CommandDelegate == new CommandDelegate(Chat_BankCommand));
				if (command != null)
				{
					Commands.ChatCommands.Remove(command);
				}
			}
		}
	}
}
