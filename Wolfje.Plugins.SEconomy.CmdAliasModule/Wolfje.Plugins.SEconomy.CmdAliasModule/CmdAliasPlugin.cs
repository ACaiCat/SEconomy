using System;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;

namespace Wolfje.Plugins.SEconomy.CmdAliasModule
{
	[ApiVersion(2, 1)]
	public class CmdAliasPlugin : TerrariaPlugin
	{
		protected static CmdAlias aliasCmdInstance;

		public static CmdAlias Instance => aliasCmdInstance;

		public override string Author => "Wolfje";

		public override string Description => "Provides a list of customized command aliases that cost money in SEconomy.";

		public override string Name => "CmdAlias";

		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		public CmdAliasPlugin(Main game)
			: base(game)
		{
			base.Order = 2147483646;
		}

		public override void Initialize()
		{
			aliasCmdInstance = new CmdAlias(this);
		}
	}
}
