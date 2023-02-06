using BubbetsItems.Helpers;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace BubbetsItems.Items.BarrierItems
{
	public class ClayCatalyst : ItemBase
	{
		private static BuffDef? _buffDef;
		public static BuffDef? BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefClayCatalyst");
		protected override void FillDefsFromSerializableCP(SerializableContentPack serializableContentPack)
		{
			base.FillDefsFromSerializableCP(serializableContentPack);
			// yeahh code based content because TK keeps fucking freezing
			var buff = ScriptableObject.CreateInstance<BuffDef>();
			buff.name = "BuffDefClayCatalyst";
			buff.buffColor = new Color(r: 1, g: 0.80784315f, b: 0, a: 1);
			buff.iconSprite = BubbetsItemsPlugin.AssetBundle.LoadAsset<Sprite>("CatalystBuff");
			serializableContentPack.buffDefs = serializableContentPack.buffDefs.AddItem(buff).ToArray();
		}
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("CLAYCATALYST_NAME","Clay Catalyst");
			AddToken("CLAYCATALYST_DESC", "Release a " + "{0}m barrier effect ".Style(StyleEnum.Heal) + "during the Teleporter event, " + "multiplying barrier decay ".Style(StyleEnum.Heal) + "on all nearby allies for " + "{1:0%}".Style(StyleEnum.Heal) + ".");
			AddToken("CLAYCATALYST_DESC_SIMPLE", "Releases a " + "13m ".Style(StyleEnum.Heal) + "(+3m per stack) ".Style(StyleEnum.Stack) + "barrier effect ".Style(StyleEnum.Heal) + "for all allies in teleporter events and holdout zones, " + "multiplying barrier decay ".Style(StyleEnum.Heal) + "by " + "80%".Style(StyleEnum.Heal) + " (-11% per stack)".Style(StyleEnum.Stack) + ".");
			SimpleDescriptionToken = "CLAYCATALYST_DESC_SIMPLE";
			AddToken("CLAYCATALYST_PICKUP", "Slow down barrier decay nearby the Teleporter event and 'Holdout Zones' such as the Void Fields.");
			AddToken("CLAYCATALYST_LORE","");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("10 + 3 * [a]", "Distance From Teleporter");
			AddScalingFunction("1 - (1.1 - Pow(0.9, [a]))", "Barrier Decay Mult");
			AddScalingFunction("0.33", "Barrier Add Pulse Frequency");
			AddScalingFunction("25 * [a]", "Barrier Add Amount");
		}

		public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
		{
			var master = inventory ? inventory!.GetComponent<CharacterMaster>() : null;
			if (master) // TODO test if this even works, and do the same for void beads and locus if it does
				scalingInfos[0].WorkingContext.a = Util.GetItemCountForTeam(master!.teamIndex, ItemDef.itemIndex, false, false);
			return base.GetFormattedDescription(null, token, forceHideExtended);
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			RecalculateStatsAPI.GetStatCoefficients += FixBarrier;
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			RecalculateStatsAPI.GetStatCoefficients -= FixBarrier;
		}

		public static void FixBarrier(CharacterBody __instance, RecalculateStatsAPI.StatHookEventArgs args)
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
		public static GameObject ZoneObject => _zoneObject ??= BubbetsItemsPlugin.AssetBundle.LoadAsset<GameObject>("ClayCatalystTeleporter");
	}
}