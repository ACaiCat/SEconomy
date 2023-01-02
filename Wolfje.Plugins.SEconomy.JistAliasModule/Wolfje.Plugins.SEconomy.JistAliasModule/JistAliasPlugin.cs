using System;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;

namespace Wolfje.Plugins.SEconomy.JistAliasModule
{
	[ApiVersion(2, 1)]
	public class JistAliasPlugin : TerrariaPlugin
	{
		public override string Author => "Wolfje";

		public override string Description => "Provides AliasCmd scripting support for Jist";

		public override string Name => "JistAlias";

		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		public JistAlias Instance { get; protected set; }

		public JistAliasPlugin(Main game)
			: base(game)
		{
			base.Order = 2147483645;
		}

		public override void Initialize()
		{
			Instance = new JistAlias(this);
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
