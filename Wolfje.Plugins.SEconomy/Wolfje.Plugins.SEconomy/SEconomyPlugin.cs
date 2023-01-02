using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Wolfje.Plugins.SEconomy.Lang;

namespace Wolfje.Plugins.SEconomy
{
	[ApiVersion(2, 1)]
	public class SEconomyPlugin : TerrariaPlugin
	{
		protected static string genericErrorMessage = "SEconomy failed to load and is disabled. You can attempt to fix what's stopping it from starting and relaunch it with /sec start.\r\n\r\nYou do NOT have to restart the server to issue this command.  Just continue as normal, and issue the command when the game starts.";

		public static Localization Locale { get; private set; }

		public override string Author => "Wolfje";

		public override string Description => "Provides server-sided currency tools for servers running TShock";

		public override string Name => "SEconomy (Milestone 1) Update " + Version.Build;

		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		public static SEconomy Instance { get; set; }

		public static event EventHandler SEconomyLoaded;

		public static event EventHandler SEconomyUnloaded;

		public SEconomyPlugin(Main Game)
			: base(Game)
		{
			base.Order = 20000;
		}

		public override void Initialize()
		{
			Localization.PrepareLanguages();
			Locale = new Localization("en-AU");
			PrintIntro();
			Commands.ChatCommands.Add(new Command("seconomy.cmd", TShock_CommandExecuted, "seconomy", "sec"));
			try
			{
				Instance = new SEconomy(this);
				if (Instance.LoadSEconomy() < 0)
				{
					throw new Exception("LoadSEconomy() failed.");
				}
			}
			catch
			{
				Instance = null;
				TShock.Log.ConsoleError(genericErrorMessage);
			}
			ServerApi.Hooks.GameInitialize.Register(this, delegate
			{
			});
		}

		protected void PrintIntro()
		{
			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write(" SEconomyÏµÍ³ ");
			Console.ForegroundColor = ConsoleColor.DarkCyan;
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write(" Copyright (C) Wolfje(CaiÉý¼¶), 2014-2023");
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.Write("http://github.com/tylerjwatson/SEconomy");
			Console.WriteLine("\r\n");
			ConsoleColor backgroundColor = Console.BackgroundColor;
			Console.ForegroundColor = ConsoleColor.Black;
			Console.BackgroundColor = ConsoleColor.Yellow;
			Console.WriteLine(" SEconomy is free software.  If you paid for it you were scammed.");
			Console.BackgroundColor = backgroundColor;
			Console.WriteLine("\r\n");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(" Please wait...");
			Console.WriteLine();
			Console.ResetColor();
		}

		public string GetVersionString()
		{
			StringBuilder stringBuilder = new StringBuilder("SEconomy Update");
			stringBuilder.AppendFormat(" {0}", Version.Build);
			return stringBuilder.ToString();
		}

		protected void RaiseUnloadedEvent()
		{
			if (SEconomyPlugin.SEconomyUnloaded != null)
			{
				SEconomyPlugin.SEconomyUnloaded(this, new EventArgs());
			}
		}

		protected void RaiseLoadedEvent()
		{
			if (SEconomyPlugin.SEconomyLoaded != null)
			{
				SEconomyPlugin.SEconomyLoaded(this, new EventArgs());
			}
		}

		protected async void TShock_CommandExecuted(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendInfoMessage(string.Format(Locale.StringOrDefault(3, "{0} by Wolfje"), GetVersionString()));
				args.Player.SendInfoMessage(" * http://plugins.tw.id.au");
				args.Player.SendInfoMessage(Locale.StringOrDefault(5, " * /sec[onomy] reload|rl - Reloads SEconomy"));
				args.Player.SendInfoMessage(Locale.StringOrDefault(6, " * /sec[onomy] stop - Stops and unloads SEconomy"));
				args.Player.SendInfoMessage(Locale.StringOrDefault(7, " * /sec[onomy] start - Starts SEconomy"));
			}
			else if ((args.Parameters[0].Equals("reload", StringComparison.CurrentCultureIgnoreCase) || args.Parameters[0].Equals("rl", StringComparison.CurrentCultureIgnoreCase)) && args.Player.Group.HasPermission("seconomy.command.reload"))
			{
				try
				{
					await Task.Run(async delegate
					{
						if (Instance != null)
						{
							Instance.Dispose();
							Instance = null;
							RaiseUnloadedEvent();
						}
						try
						{
							Instance = new SEconomy(this);
							if (Instance.LoadSEconomy() < 0)
							{
								throw new Exception("LoadSEconomy() failed.");
							}
							await Instance.BindToWorldAsync();
							RaiseLoadedEvent();
						}
						catch
						{
							Instance = null;
							TShock.Log.ConsoleError(genericErrorMessage);
							throw;
						}
					});
				}
				catch
				{
					RaiseUnloadedEvent();
					args.Player.SendErrorMessage(Locale.StringOrDefault(12, "SEconomy failed to initialize, and will be unavailable for this session."));
					return;
				}
				args.Player.SendSuccessMessage(Locale.StringOrDefault(8, "SEconomy is reloaded."));
			}
			else if (args.Parameters[0].Equals("stop", StringComparison.CurrentCultureIgnoreCase) && args.Player.Group.HasPermission("seconomy.command.stop"))
			{
				if (Instance == null)
				{
					args.Player.SendErrorMessage(Locale.StringOrDefault(9, "seconomy stop: SEconomy is already stopped. Use /sec start to start"));
					return;
				}
				await Task.Run(delegate
				{
					Instance.Dispose();
					Instance = null;
				});
				args.Player.SendSuccessMessage(Locale.StringOrDefault(10, "SEconomy is stopped."));
				RaiseUnloadedEvent();
			}
			else if (args.Parameters[0].Equals("start", StringComparison.CurrentCultureIgnoreCase) && args.Player.Group.HasPermission("seconomy.command.start"))
			{
				if (Instance != null)
				{
					args.Player.SendErrorMessage(Locale.StringOrDefault(11, "seconomy stop: SEconomy is already started. Use /sec stop to stop."));
					return;
				}
				try
				{
					await Task.Run(async delegate
					{
						try
						{
							Instance = new SEconomy(this);
							if (Instance.LoadSEconomy() < 0)
							{
								throw new Exception("LoadSEconomy() failed.");
							}
							await Instance.BindToWorldAsync();
						}
						catch
						{
							RaiseUnloadedEvent();
							Instance = null;
							TShock.Log.ConsoleError(genericErrorMessage);
							throw;
						}
					});
				}
				catch
				{
					args.Player.SendErrorMessage(Locale.StringOrDefault(12, "SEconomy failed to initialize, and will be unavailable for this session."));
					return;
				}
				args.Player.SendSuccessMessage(Locale.StringOrDefault(13, "SEconomy has started."));
				RaiseLoadedEvent();
			}
			else if ((args.Parameters[0].Equals("multi", StringComparison.CurrentCultureIgnoreCase) || args.Parameters[0].Equals("multiplier", StringComparison.CurrentCultureIgnoreCase)) && args.Player.Group.HasPermission("seconomy.command.multi"))
			{
				RaiseUnloadedEvent();
				int result = 0;
				if (args.Parameters.Count == 1)
				{
					args.Player.SendInfoMessage("sec multi: Reward multiplier: {0}", Instance.WorldEc.CustomMultiplier);
				}
				else if (!int.TryParse(args.Parameters[1], out result) || result < 0 || result > 100)
				{
					args.Player.SendErrorMessage("sec multi: Syntax: /sec multi[plier] 1-100");
				}
				else
				{
					Instance.WorldEc.CustomMultiplier = result;
					args.Player.SendInfoMessage("sec multi: Multiplier of {0} set successfully.", result);
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && Instance != null)
			{
				Instance.Dispose();
				Instance = null;
			}
			base.Dispose(disposing);
		}
	}
}
