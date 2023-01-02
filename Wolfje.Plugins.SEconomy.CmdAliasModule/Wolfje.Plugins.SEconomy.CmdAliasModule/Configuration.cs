using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using Newtonsoft.Json;
using TShockAPI;

namespace Wolfje.Plugins.SEconomy.CmdAliasModule
{
	public class Configuration
	{
		public List<AliasCommand> CommandAliases = new List<AliasCommand>();

		public static Configuration LoadConfigurationFromFile(string Path)
		{
			Configuration configuration = null;
			string path = System.IO.Path.Combine(Path, "aliascmd.conf.d");
			try
			{
				configuration = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(Path));
			}
			catch (Exception ex)
			{
				if (ex is FileNotFoundException || ex is DirectoryNotFoundException)
				{
					TShock.Log.ConsoleError("cmdalias configuration: Cannot find file or directory. Creating new one.");
					configuration = NewSampleConfiguration();
					configuration.SaveConfiguration(Path);
				}
				else if (ex is SecurityException)
				{
					TShock.Log.ConsoleError("cmdalias configuration: Access denied reading file " + Path);
				}
				else
				{
					TShock.Log.ConsoleError("cmdalias configuration: error " + ex.ToString());
				}
			}
			if (!Directory.Exists(path))
			{
				try
				{
					Directory.CreateDirectory(path);
				}
				catch
				{
				}
			}
			if (!Directory.Exists(path))
			{
				return configuration;
			}
			foreach (string item in Directory.EnumerateFiles(path, "*.json"))
			{
				string value = null;
				Configuration configuration2 = null;
				if (string.IsNullOrEmpty(item) || !File.Exists(item))
				{
					continue;
				}
				try
				{
					value = File.ReadAllText(item);
				}
				catch
				{
				}
				if (string.IsNullOrEmpty(value))
				{
					continue;
				}
				try
				{
					configuration2 = JsonConvert.DeserializeObject<Configuration>(value);
				}
				catch
				{
				}
				if (configuration2 == null)
				{
					continue;
				}
				foreach (AliasCommand alias in configuration2.CommandAliases)
				{
					if (configuration.CommandAliases.FirstOrDefault((AliasCommand i) => i.CommandAlias == alias.CommandAlias) != null)
					{
						TShock.Log.ConsoleError("aliascmd warning: Duplicate alias {0} in file {1} ignored", alias.CommandAlias, System.IO.Path.GetFileName(item));
					}
					else
					{
						configuration.CommandAliases.Add(alias);
					}
				}
			}
			return configuration;
		}

		public static Configuration NewSampleConfiguration()
		{
			Configuration configuration = new Configuration();
			configuration.CommandAliases.Add(AliasCommand.Create("testparms", "", "0c", "", 0, "/bc Input param 1 2 3: $1 $2 $3", "/bc Input param 1-3: $1-3", "/bc Input param 2 to end of line: $2-"));
			configuration.CommandAliases.Add(AliasCommand.Create("testrandom", "", "0c", "", 0, "/bc Random Number: $random(1,100)"));
			configuration.CommandAliases.Add(AliasCommand.Create("impersonate", "", "0c", "", 0, "$runas($1,/me can fit $random(1,100) cocks in their mouth at once.)"));
			return configuration;
		}

		public void SaveConfiguration(string Path)
		{
			try
			{
				string contents = JsonConvert.SerializeObject(this, Formatting.Indented);
				File.WriteAllText(Path, contents);
			}
			catch (Exception ex)
			{
				if (ex is DirectoryNotFoundException)
				{
					TShock.Log.ConsoleError("cmdalias config: save directory not found: " + Path);
					return;
				}
				if (!(ex is UnauthorizedAccessException) && !(ex is SecurityException))
				{
					TShock.Log.ConsoleError("cmdalias config: Error reading file: " + Path);
					throw;
				}
				TShock.Log.ConsoleError("cmdalias config: Access is denied to Vault config: " + Path);
			}
		}
	}
}
