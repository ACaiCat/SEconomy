using System;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Wolfje.Plugins.Jist.Framework;
using Wolfje.Plugins.Jist.stdlib;

namespace Wolfje.Plugins.Jist
{
	[ApiVersion(2, 1)]
	public class JistPlugin : TerrariaPlugin
	{
		protected JistRestInterface _restInterface;

		public static JistEngine Instance { get; protected set; }

		public override string Author => "Wolfje";

		public override string Description => "Javascript interpreted scripting for TShock";

		public override string Name => "Jist";

		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		public static event EventHandler<JavascriptFunctionsNeededEventArgs> JavascriptFunctionsNeeded;

		public JistPlugin(Main game)
			: base(game)
		{
			base.Order = 1;
			Instance = new JistEngine(this);
			Commands.ChatCommands.Add(new Command("jist.cmd", TShockAPI_JistChatCommand, "jist"));
			ServerApi.Hooks.GameInitialize.Register(this, game_initialize);
		}

		private async void TShockAPI_JistChatCommand(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				return;
			}
			if (args.Parameters[0].Equals("dumpenv", StringComparison.CurrentCultureIgnoreCase))
			{
				if (Instance == null)
				{
				}
				return;
			}
			if (args.Parameters[0].Equals("dumptasks", StringComparison.CurrentCultureIgnoreCase))
			{
				foreach (RecurringFunction item in from i in Instance.stdTask.DumpTasks()
					orderby i.NextRunTime
					select i)
				{
					args.Player.SendInfoMessage(item.ToString());
				}
				return;
			}
			if (args.Parameters[0].Equals("eval", StringComparison.CurrentCultureIgnoreCase) || (args.Parameters[0].Equals("ev", StringComparison.CurrentCultureIgnoreCase) && args.Parameters.Count > 1))
			{
				args.Player.SendInfoMessage(Instance.Eval(args.Parameters[1]));
			}
			else if (args.Parameters[0].Equals("reload", StringComparison.CurrentCultureIgnoreCase) || args.Parameters[0].Equals("rl", StringComparison.CurrentCultureIgnoreCase))
			{
				Instance.Dispose();
				Instance = null;
				Instance = new JistEngine(this);
				await Instance.LoadEngineAsync();
				args.Player.SendInfoMessage("Jist reloaded");
			}
		}

		internal static void RequestExternalFunctions()
		{
			JavascriptFunctionsNeededEventArgs e = new JavascriptFunctionsNeededEventArgs(Instance);
			if (JistPlugin.JavascriptFunctionsNeeded != null)
			{
				JistPlugin.JavascriptFunctionsNeeded(Instance, e);
			}
		}

		public override void Initialize()
		{
		}

		private void game_initialize(EventArgs args)
		{
			_restInterface = new JistRestInterface(this);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				Instance.Dispose();
			}
		}
	}
}
