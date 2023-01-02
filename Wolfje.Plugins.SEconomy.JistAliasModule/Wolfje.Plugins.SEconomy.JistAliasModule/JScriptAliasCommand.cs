using Jint.Native;
using Wolfje.Plugins.SEconomy.CmdAliasModule;

namespace Wolfje.Plugins.SEconomy.JistAliasModule
{
	public class JScriptAliasCommand : AliasCommand
	{
		public JsValue func;

		public bool Silent { get; set; }

		public static JScriptAliasCommand Create(string AliasName, string Cost, int CooldownSeconds, string PermissionNeeded, JsValue func)
		{
			return new JScriptAliasCommand
			{
				CommandAlias = AliasName,
				CommandsToExecute = null,
				CooldownSeconds = CooldownSeconds,
				Permissions = PermissionNeeded,
				func = func
			};
		}

		public static JScriptAliasCommand CreateSilent(string AliasName, string Cost, int CooldownSeconds, string PermissionNeeded, JsValue func)
		{
			return new JScriptAliasCommand
			{
				CommandAlias = AliasName,
				CommandsToExecute = null,
				CooldownSeconds = CooldownSeconds,
				Permissions = PermissionNeeded,
				func = func,
				Silent = true
			};
		}
	}
}
