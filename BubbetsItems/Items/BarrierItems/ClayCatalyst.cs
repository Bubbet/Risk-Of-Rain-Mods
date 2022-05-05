using BubbetsItems.Helpers;
using System.Collections.Generic;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace BubbetsItems.Items.BarrierItems
{
	public class ClayCatalyst : ItemBase
	{
		private static BuffDef? _buffDef;
		private static BuffDef? BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefClayCatalyst");
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("CLAYCATALYST_NAME","Clay Catalyst");
			AddToken("CLAYCATALYST_DESC", "Release a " + "{0}m barrier effect ".Style(StyleEnum.Health) + "during the Teleporter event, " + "multiplying barrier decay ".Style(StyleEnum.Health) + "on all nearby allies for " + "{1:0%}".Style(StyleEnum.Health) + ".");
			AddToken("CLAYCATALYST_PICKUP", "Slow down barrier decay nearby the Teleporter event and 'Holdout Zones' such as the Void Fields.");
			AddToken("CLAYCATALYST_LORE","");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("10 + 3 * [a]", "Distance From Teleporter");
			AddScalingFunction("1 - (1.1 - Pow(0.9, [a]))", "Barrier Decay Mult");
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void FixBarrier(CharacterBody __instance)
		{
			var instance = GetInstance<ClayCatalyst>();
			if (instance == null) return;
			var teamIndex = __instance.teamComponent.teamIndex;
			if (__instance.GetBuffCount(BuffDef) <= 0) return;
			var amount = Util.GetItemCountForTeam(teamIndex, instance.ItemDef.itemIndex, false);
			__instance.barrierDecayRate *= instance.scalingInfos[1].ScalingFunction(amount);
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

		private static GameObject? _zoneObject;

		public static GameObject ZoneObject
		{
			get
			{
				return _zoneObject ??= BubbetsItemsPlugin.AssetBundle.LoadAsset<GameObject>("ClayCatalystTeleporter");
			}
		}
	}
}