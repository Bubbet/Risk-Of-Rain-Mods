using System.Collections.Generic;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace BubbetsItems.Items.BarrierItems
{
	public class ClayCatalyst : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("CLAYCATALYST_NAME","Clay Catalyst");
			AddToken("CLAYCATALYST_DESC","Within {0}m of the teleporter, barrier decay is multiplied by {1:0%}.");
			AddToken("CLAYCATALYST_PICKUP","");
			AddToken("CLAYCATALYST_LORE","");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("10 + 3 * [a]", "Distance From Teleporter");
			AddScalingFunction("1 - (1.1 - Pow(0.9, [a]))", "Barrier Decay Mult");
		}

		public static Dictionary<HoldoutZoneController, GameObject[]> ZoneInstances = new(); 
		
		[HarmonyPostfix, HarmonyPatch(typeof(HoldoutZoneController), nameof(HoldoutZoneController.UpdateHealingNovas))]
		public static void UpdateClayCatalyst(HoldoutZoneController __instance, bool isCharging)
		{
			var inst = GetInstance<ClayCatalyst>();
			if (inst == null) return;

			ZoneInstances.TryGetValue(__instance, out var zones);
			zones ??= new GameObject[5];

			for (TeamIndex teamIndex = TeamIndex.Neutral; teamIndex < TeamIndex.Count; teamIndex += 1)
			{
				bool AnyPlayers = Util.GetItemCountForTeam(teamIndex, inst.ItemDef.itemIndex, false) > 0 && isCharging;
				if (NetworkServer.active)
				{
					ref var ptr = ref zones[(int) teamIndex];
					if (AnyPlayers != ptr)
					{
						if (AnyPlayers)
						{
							ptr = GameObject.Instantiate(ZoneObject, __instance.healingNovaRoot ? __instance.healingNovaRoot : __instance.transform);
							ptr.GetComponent<TeamFilter>().teamIndex = teamIndex;
							NetworkServer.Spawn(ptr);
						}
						else
						{
							GameObject.Destroy(ptr);
							ptr = null;
						}
					}
				}
			}

			ZoneInstances[__instance] = zones;
		}

		public static GameObject ZoneObject { get; set; } // TODO
	}
}