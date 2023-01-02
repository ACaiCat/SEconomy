using System;
using System.Collections.Generic;
using Jint.Native;
using TShockAPI;
using Wolfje.Plugins.Jist;
using Wolfje.Plugins.Jist.Framework;
using Wolfje.Plugins.Jist.stdlib;
using Wolfje.Plugins.SEconomy.CmdAliasModule;

namespace Wolfje.Plugins.SEconomy.JistAliasModule.AliasLib
{
	internal class stdalias : stdlib_base
	{
		protected JistAlias aliasEngine;

		public stdalias(JistEngine engine, JistAlias aliasEngine)
			: base(engine)
		{
			base.Provides = "aliascmd";
			base.engine = engine;
			this.aliasEngine = aliasEngine;
			JistPlugin.JavascriptFunctionsNeeded += JistPlugin_JavascriptFunctionsNeeded;
		}

		private void JistPlugin_JavascriptFunctionsNeeded(object sender, JavascriptFunctionsNeededEventArgs e)
		{
			e.Engine.CreateScriptFunctions(GetType(), this);
		}

		[JavascriptFunction(new string[] { "create_alias", "acmd_alias_create" })]
		public bool CreateAlias(string AliasName, string Cost, int CooldownSeconds, string Permissions, JsValue func)
		{
			try
			{
				JScriptAliasCommand alias = new JScriptAliasCommand
				{
					CommandAlias = AliasName,
					CooldownSeconds = Convert.ToInt32(CooldownSeconds),
					Cost = Cost,
					Permissions = Permissions,
					func = func
				};
				aliasEngine.CreateAlias(alias);
			}
			catch (Exception ex)
			{
				ScriptLog.ErrorFormat("aliascmd", "CreateAlias failed: " + ex.Message);
				return false;
			}
			return true;
		}

		[JavascriptFunction(new string[] { "acmd_alias_create_silent" })]
		public bool CreateAliasSilent(string AliasName, string Cost, int CooldownSeconds, string Permissions, JsValue func)
		{
			try
			{
				JScriptAliasCommand alias = new JScriptAliasCommand
				{
					CommandAlias = AliasName,
					CooldownSeconds = Convert.ToInt32(CooldownSeconds),
					Cost = Cost,
					Permissions = Permissions,
					func = func,
					Silent = true
				};
				aliasEngine.CreateAlias(alias);
			}
			catch (Exception ex)
			{
				ScriptLog.ErrorFormat("aliascmd", "CreateAlias failed: " + ex.Message);
				return false;
			}
			return true;
		}

		[JavascriptFunction(new string[] { "acmd_alias_remove" })]
		public bool RemoveAlias(object aliasObject)
		{
			JScriptAliasCommand jScriptAliasCommand = null;
			if ((jScriptAliasCommand = aliasEngine.GetAlias(aliasObject)) == null)
			{
				return false;
			}
			try
			{
				aliasEngine.RemoveAlias(jScriptAliasCommand);
			}
			catch (Exception ex)
			{
				ScriptLog.ErrorFormat("aliascmd", "RemoveAlias failed: " + ex.Message);
				return false;
			}
			return true;
		}

		[JavascriptFunction(new string[] { "acmd_cooldown_reset" })]
		public bool ResetCooldown(object player, object aliasObject)
		{
			JScriptAliasCommand jScriptAliasCommand = null;
			TSPlayer tSPlayer = null;
			if ((jScriptAliasCommand = aliasEngine.GetAlias(aliasObject)) == null || (tSPlayer = JistPlugin.Instance.stdTshock.GetPlayer(player)) == null)
			{
				return false;
			}
			KeyValuePair<string, AliasCommand> key = new KeyValuePair<string, AliasCommand>(tSPlayer.Name, jScriptAliasCommand);
			if (aliasEngine.CooldownList.ContainsKey(key))
			{
				aliasEngine.CooldownList.Remove(key);
			}
			return true;
		}

		[JavascriptFunction(new string[] { "acmd_cooldown_set" })]
		public bool SetCooldown(object player, object aliasObject, int cooldownSeconds)
		{
			JScriptAliasCommand jScriptAliasCommand = null;
			TSPlayer tSPlayer = null;
			if ((jScriptAliasCommand = aliasEngine.GetAlias(aliasObject)) == null || (tSPlayer = JistPlugin.Instance.stdTshock.GetPlayer(player)) == null)
			{
				return false;
			}
			KeyValuePair<string, AliasCommand> cooldownReference = new KeyValuePair<string, AliasCommand>(tSPlayer.Name, jScriptAliasCommand);
			aliasEngine.PopulateCooldownList(cooldownReference, TimeSpan.FromSeconds(cooldownSeconds));
			return true;
		}
	}
}
