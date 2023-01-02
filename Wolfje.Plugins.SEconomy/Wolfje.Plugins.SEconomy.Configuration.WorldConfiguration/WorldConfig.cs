using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using Newtonsoft.Json;
using TShockAPI;

namespace Wolfje.Plugins.SEconomy.Configuration.WorldConfiguration
{
	public class WorldConfig
	{
		public bool MoneyFromNPCEnabled = true;

		public bool MoneyFromBossEnabled = true;

		public bool MoneyFromPVPEnabled = true;

		public bool AnnounceNPCKillGains = true;

		public bool AnnounceBossKillGains = true;

		public bool ShowKillGainsDetailed = true;

		public bool ShowKillGainsOverhead = true;

		public int[] OverheadColor = new int[3] { 255, 255, 0 };

		public bool StaticDeathPenalty;

		public long StaticPenaltyAmount;

		public List<StaticPenaltyOverride> StaticPenaltyOverrides = new List<StaticPenaltyOverride>();

		public bool KillerTakesDeathPenalty = true;

		public decimal DeathPenaltyPercentValue = 10.0m;

		public decimal MoneyPerDamagePoint = 1.0m;

		public bool IgnoreSpawnedFromStatue = true;

		public List<NPCRewardOverride> Overrides = new List<NPCRewardOverride>();

		public WorldConfig()
		{
			StaticPenaltyOverrides.Add(new StaticPenaltyOverride
			{
				StaticRewardOverride = 0L,
				TShockGroup = ""
			});
		}

		public static WorldConfig LoadConfigurationFromFile(string Path)
		{
			WorldConfig result = null;
			try
			{
				result = JsonConvert.DeserializeObject<WorldConfig>(File.ReadAllText(Path));
				return result;
			}
			catch (Exception ex)
			{
				if (!(ex is FileNotFoundException) && !(ex is DirectoryNotFoundException))
				{
					if (!(ex is SecurityException))
					{
						TShock.Log.ConsoleError("seconomy worldconfig: error " + ex.ToString());
						return result;
					}
					TShock.Log.ConsoleError("seconomy worldconfig: Access denied reading file " + Path);
					return result;
				}
				TShock.Log.ConsoleError("seconomy worldconfig: Cannot find file or directory. Creating new one.");
				result = NewSampleConfiguration();
				result.SaveConfiguration(Path);
				return result;
			}
		}

		public static WorldConfig NewSampleConfiguration()
		{
			WorldConfig worldConfig = new WorldConfig();
			int[] array = new int[10] { 1, 49, 74, 46, 85, 67, 55, 63, 58, 21 };
			int[] array2 = array;
			foreach (int nPCID in array2)
			{
				worldConfig.Overrides.Add(new NPCRewardOverride
				{
					NPCID = nPCID,
					OverridenMoneyPerDamagePoint = 1.0m
				});
			}
			worldConfig.StaticPenaltyOverrides.Add(new StaticPenaltyOverride
			{
				StaticRewardOverride = 0L,
				TShockGroup = ""
			});
			return worldConfig;
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
					TShock.Log.ConsoleError("seconomy worldconfig: save directory not found: " + Path);
					return;
				}
				if (!(ex is UnauthorizedAccessException) && !(ex is SecurityException))
				{
					TShock.Log.ConsoleError("seconomy worldconfig: Error reading file: " + Path);
					throw;
				}
				TShock.Log.ConsoleError("seconomy worldconfig: Access is denied to Vault config: " + Path);
			}
		}
	}
}
