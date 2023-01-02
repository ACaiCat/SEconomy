using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Wolfje.Plugins.SEconomy.Configuration.WorldConfiguration;
using Wolfje.Plugins.SEconomy.Journal;
using Wolfje.Plugins.SEconomy.Packets;

namespace Wolfje.Plugins.SEconomy
{
	public class WorldEconomy : IDisposable
	{
		private Dictionary<NPC, List<PlayerDamage>> DamageDictionary = new Dictionary<NPC, List<PlayerDamage>>();

		protected Dictionary<int, int> PVPDamage = new Dictionary<int, int>();

		protected readonly object __dictionaryMutex = new object();

		protected readonly object __pvpDictMutex = new object();

		protected static readonly object __NPCDamageMutex = new object();

		protected SEconomy Parent { get; set; }

		public int CustomMultiplier { get; set; }

		public WorldConfig WorldConfiguration { get; private set; }

		public WorldEconomy(SEconomy parent)
		{
			WorldConfiguration = WorldConfig.LoadConfigurationFromFile("tshock" + Path.DirectorySeparatorChar + "SEconomy" + Path.DirectorySeparatorChar + "SEconomy.WorldConfig.json");
			Parent = parent;
			ServerApi.Hooks.NetGetData.Register(Parent.PluginInstance, NetHooks_GetData);
			ServerApi.Hooks.NetSendData.Register(Parent.PluginInstance, NetHooks_SendData);
			ServerApi.Hooks.GameUpdate.Register(Parent.PluginInstance, Game_Update);
			CustomMultiplier = 1;
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
				ServerApi.Hooks.NetGetData.Deregister(Parent.PluginInstance, NetHooks_GetData);
				ServerApi.Hooks.NetSendData.Deregister(Parent.PluginInstance, NetHooks_SendData);
				ServerApi.Hooks.GameUpdate.Deregister(Parent.PluginInstance, Game_Update);
			}
		}

		protected void Game_Update(EventArgs args)
		{
			NPC[] npc = Main.npc;
			NPC[] array = npc;
			foreach (NPC nPC in array)
			{
				if (nPC != null && !nPC.townNPC && nPC.lifeMax != 0 && !nPC.active)
				{
					GiveRewardsForNPC(nPC);
				}
			}
		}

		protected void AddNPCDamage(NPC NPC, Player Player, int Damage, bool crit = false)
		{
			List<PlayerDamage> list = null;
			PlayerDamage playerDamage = null;
			if (Player == null || !NPC.active || NPC.life <= 0)
			{
				return;
			}
			lock (__dictionaryMutex)
			{
				if (DamageDictionary.ContainsKey(NPC))
				{
					list = DamageDictionary[NPC];
				}
				else
				{
					list = new List<PlayerDamage>(1);
					DamageDictionary.Add(NPC, list);
				}
			}
			double num;
			lock (__NPCDamageMutex)
			{
				if ((playerDamage = list.FirstOrDefault((PlayerDamage i) => i.Player == Player)) == null)
				{
					playerDamage = new PlayerDamage
					{
						Player = Player
					};
					list.Add(playerDamage);
				}
				if ((num = (double)((!crit) ? 1 : 2) * Main.CalculateDamageNPCsTake(Damage, NPC.ichor ? (NPC.defense - 20) : NPC.defense)) > (double)NPC.life)
				{
					num = NPC.life;
				}
			}
			playerDamage.Damage += num;
			if (playerDamage.Damage > (double)NPC.lifeMax)
			{
				playerDamage.Damage -= playerDamage.Damage % (double)NPC.lifeMax;
			}
		}

		protected void GiveRewardsForNPC(NPC NPC)
		{
			List<PlayerDamage> list = null;
			Money money = 0L;
			lock (__dictionaryMutex)
			{
				if (DamageDictionary.ContainsKey(NPC))
				{
					list = DamageDictionary[NPC];
					if (!DamageDictionary.Remove(NPC))
					{
						TShock.Log.ConsoleError("seconomy: 世界经济: 奖励失败后将NPC移出.这是一个内部错误.");
					}
				}
			}
			if (list == null || ((!NPC.boss || !WorldConfiguration.MoneyFromBossEnabled) && (NPC.boss || !WorldConfiguration.MoneyFromNPCEnabled)) || (NPC.SpawnedFromStatue && WorldConfiguration.IgnoreSpawnedFromStatue))
			{
				return;
			}
			foreach (PlayerDamage damage in list)
			{
				TSPlayer tSPlayer;
				IBankAccount bankAccount;
				if (damage.Player == null || (tSPlayer = TShock.Players.FirstOrDefault((TSPlayer i) => i != null && i.Index == damage.Player.whoAmI)) == null || (bankAccount = Parent.GetBankAccount(tSPlayer)) == null)
				{
					continue;
				}
				money = CustomMultiplier * Convert.ToInt64(Math.Round(Convert.ToDouble(WorldConfiguration.MoneyPerDamagePoint) * damage.Damage));
				NPCRewardOverride nPCRewardOverride = WorldConfiguration.Overrides.FirstOrDefault((NPCRewardOverride i) => i.NPCID == NPC.type);
				if (nPCRewardOverride != null)
				{
					money = CustomMultiplier * Convert.ToInt64(Math.Round(Convert.ToDouble(nPCRewardOverride.OverridenMoneyPerDamagePoint) * damage.Damage));
				}
				if ((long)money > 0 && tSPlayer.Group.HasPermission("seconomy.world.mobgains"))
				{
					CachedTransaction cachedTransaction = new CachedTransaction
					{
						Aggregations = 1,
						Amount = money,
						DestinationBankAccountK = bankAccount.BankAccountK,
						Message = NPC.FullName,
						SourceBankAccountK = Parent.WorldAccount.BankAccountK
					};
					if ((NPC.boss && WorldConfiguration.AnnounceBossKillGains) || (!NPC.boss && WorldConfiguration.AnnounceNPCKillGains))
					{
						cachedTransaction.Options |= BankAccountTransferOptions.AnnounceToReceiver;
					}
					Parent.TransactionCache.AddCachedTransaction(cachedTransaction);
				}
			}
		}

		protected void PlayerHitPlayer(int HitterSlot, int VictimSlot)
		{
			lock (__pvpDictMutex)
			{
				if (PVPDamage.ContainsKey(VictimSlot))
				{
					PVPDamage[VictimSlot] = HitterSlot;
				}
				else
				{
					PVPDamage.Add(VictimSlot, HitterSlot);
				}
			}
		}

		protected Money GetDeathPenalty(TSPlayer player)
		{
			Money money = 0L;
			IBankAccount bankAccount;
			if (Parent == null || (bankAccount = Parent.GetBankAccount(player)) == null)
			{
				return default(Money);
			}
			if (!WorldConfiguration.StaticDeathPenalty)
			{
				return (long)Math.Round(Convert.ToDouble(bankAccount.Balance.Value) * (Convert.ToDouble(WorldConfiguration.DeathPenaltyPercentValue) * Math.Pow(10.0, -2.0)) * (double)CustomMultiplier);
			}
			money = WorldConfiguration.StaticPenaltyAmount;
			StaticPenaltyOverride staticPenaltyOverride;
			if ((staticPenaltyOverride = WorldConfiguration.StaticPenaltyOverrides.FirstOrDefault((StaticPenaltyOverride i) => i.TShockGroup == player.Group.Name)) != null)
			{
				money = CustomMultiplier * staticPenaltyOverride.StaticRewardOverride;
			}
			return money;
		}

		protected void ProcessDeath(int DeadPlayerSlot, bool PVPDeath)
		{
			TSPlayer tSPlayer = null;
			TSPlayer tSPlayer2 = null;
			Money money = default(Money);
			int index = 0;
			CachedTransaction cachedTransaction = null;
			CachedTransaction cachedTransaction2 = null;
			lock (__pvpDictMutex)
			{
				if (PVPDamage.ContainsKey(DeadPlayerSlot))
				{
					index = PVPDamage[DeadPlayerSlot];
					PVPDamage.Remove(DeadPlayerSlot);
				}
			}
			IBankAccount bankAccount;
			if ((tSPlayer2 = TShock.Players.ElementAtOrDefault(DeadPlayerSlot)) != null && !tSPlayer2.Group.HasPermission("seconomy.world.bypassdeathpenalty") && (bankAccount = Parent.GetBankAccount(tSPlayer2)) != null && (long)(money = GetDeathPenalty(tSPlayer2)) != 0L)
			{
				cachedTransaction2 = new CachedTransaction
				{
					DestinationBankAccountK = Parent.WorldAccount.BankAccountK,
					SourceBankAccountK = bankAccount.BankAccountK,
					Message = "dying",
					Options = (BankAccountTransferOptions.AnnounceToSender | BankAccountTransferOptions.MoneyTakenOnDeath),
					Amount = money
				};
				Parent.TransactionCache.AddCachedTransaction(cachedTransaction2);
				IBankAccount bankAccount2;
				if (PVPDeath && WorldConfiguration.MoneyFromPVPEnabled && WorldConfiguration.KillerTakesDeathPenalty && (tSPlayer = TShock.Players.ElementAtOrDefault(index)) != null && (bankAccount2 = Parent.GetBankAccount(tSPlayer)) != null)
				{
					cachedTransaction = new CachedTransaction
					{
						SourceBankAccountK = Parent.WorldAccount.BankAccountK,
						DestinationBankAccountK = bankAccount2.BankAccountK,
						Amount = money,
						Message = "正在杀死" + tSPlayer2.Name,
						Options = BankAccountTransferOptions.AnnounceToReceiver
					};
					Parent.TransactionCache.AddCachedTransaction(cachedTransaction);
				}
			}
		}

		protected void NetHooks_GetData(GetDataEventArgs args)
		{
			byte[] array = null;
			TSPlayer tSPlayer = null;
			if (args.Handled || (tSPlayer = TShock.Players.ElementAtOrDefault(args.Msg.whoAmI)) == null)
			{
				return;
			}
			array = new byte[args.Length];
			Array.Copy(args.Msg.readBuffer, args.Index, array, 0, args.Length);
			if (args.MsgID == PacketTypes.NpcStrike)
			{
				NPC nPC = null;
				DamageNPC damageNPC = PacketMarshal.MarshalFromBuffer<DamageNPC>(array);
				if (damageNPC.NPCID >= 0 && damageNPC.NPCID <= Main.npc.Length && args.Msg.whoAmI >= 0 && damageNPC.NPCID <= Main.player.Length && (nPC = Main.npc.ElementAtOrDefault(damageNPC.NPCID)) != null && !(DateTime.UtcNow.Subtract(tSPlayer.LastThreat).TotalMilliseconds < 5000.0))
				{
					AddNPCDamage(nPC, tSPlayer.TPlayer, damageNPC.Damage, Convert.ToBoolean(damageNPC.CrititcalHit));
				}
			}
		}

		protected void NetHooks_SendData(SendDataEventArgs e)
		{
			try
			{
				if (e.MsgId == PacketTypes.PlayerHurtV2)
				{
					if (Convert.ToBoolean(e.number4) && Main.player[e.number] != null)
					{
						PlayerHitPlayer(e.ignoreClient, e.number);
					}
				}
				else if (e.MsgId == PacketTypes.PlayerDeathV2)
				{
					ProcessDeath(e.number, Convert.ToBoolean(e.number4));
				}
			}
			catch
			{
			}
		}
	}
}
