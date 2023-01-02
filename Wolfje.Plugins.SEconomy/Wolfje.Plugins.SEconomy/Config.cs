using System;
using System.IO;
using System.Security;
using Newtonsoft.Json;
using TShockAPI;
using Wolfje.Plugins.SEconomy.Configuration;

namespace Wolfje.Plugins.SEconomy
{
	public class Config
	{
		public static string BaseDirectory = "tshock" + Path.DirectorySeparatorChar + "SEconomy";

		public static string JournalPath = BaseDirectory + Path.DirectorySeparatorChar + "SEconomy.journal.xml.gz";

		protected string path;

		public bool BankAccountsEnabled = true;

		public string StartingMoney = "0";

		public int PayIntervalMinutes = 30;

		public int IdleThresholdMinutes = 10;

		public string IntervalPayAmount = "0";

		public string JournalType = "xml";

		public int JournalBackupMinutes = 1;

		public MoneyProperties MoneyConfiguration = new MoneyProperties();

		public SQLConnectionProperties SQLConnectionProperties = new SQLConnectionProperties();

		public bool EnableProfiler;

		public Config(string path)
		{
			this.path = path;
		}

		public static Config FromFile(string Path)
		{
			Config config = null;
			if (!Directory.Exists(BaseDirectory))
			{
				try
				{
					Directory.CreateDirectory(BaseDirectory);
				}
				catch
				{
					TShock.Log.ConsoleError("seconomy configuration: Cannot create base directory: {0}", BaseDirectory);
					return null;
				}
			}
			try
			{
				config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path));
				config.path = Path;
				return config;
			}
			catch (Exception ex)
			{
				if (!(ex is FileNotFoundException) && !(ex is DirectoryNotFoundException))
				{
					if (!(ex is SecurityException))
					{
						TShock.Log.ConsoleError("seconomy configuration: error " + ex.ToString());
						return config;
					}
					TShock.Log.ConsoleError("seconomy configuration: Access denied reading file " + Path);
					return config;
				}
				TShock.Log.ConsoleError("seconomy configuration: Cannot find file or directory. Creating new one.");
				config = new Config(Path);
				config.SaveConfiguration();
				return config;
			}
		}

		public void SaveConfiguration()
		{
			try
			{
				string contents = JsonConvert.SerializeObject(this, Formatting.Indented);
				File.WriteAllText(path, contents);
			}
			catch (Exception ex)
			{
				if (ex is DirectoryNotFoundException)
				{
					TShock.Log.ConsoleError("seconomy config: save directory not found: " + path);
					return;
				}
				if (!(ex is UnauthorizedAccessException) && !(ex is SecurityException))
				{
					TShock.Log.ConsoleError("seconomy config: Error reading file: " + path);
					throw;
				}
				TShock.Log.ConsoleError("seconomy config: Access is denied to config: " + path);
			}
		}
	}
}
