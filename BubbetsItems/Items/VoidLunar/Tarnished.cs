using System.Collections.Generic;
using BubbetsItems.Helpers;
using BubbetsItems.ItemBehaviors;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace BubbetsItems.Items.VoidLunar
{
	public class Tarnished : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			var name = GetType().Name.ToUpper();
			SimpleDescriptionToken = name + "_DESC_SIMPLE";
			AddToken(name + "_NAME", "Tarnished");
			var convert = "Corrupts all Purity.".Style(StyleEnum.Void);
			AddToken(name + "_DESC", "Gain "+"1 Luck".Style(StyleEnum.Utility) +" for " + "{0} favorable rolls per stage.".Style(StyleEnum.Utility) +" Once out of favorable rolls, gain {1} luck. ".Style(StyleEnum.Health) + convert);
			AddToken(name + "_DESC_SIMPLE", "While active, all random effects are rolled " + "+1 time for a favorable outcome".Style(StyleEnum.Utility) + ". " + "Only stays active for 50 ".Style(StyleEnum.Health) + "(+50 per stack) ".Style(StyleEnum.Stack) + "times per stage".Style(StyleEnum.Health) + ". " + "When inactive, all random effects are rolled +1 ".Style(StyleEnum.Health) + "(+1 per stack) ".Style(StyleEnum.Stack) + "times for an unfavorable outcome".Style(StyleEnum.Health) + ". " + convert);
			AddToken(name + "_PICKUP", "Gain temporary luck, " + "then become unlucky.".Style(StyleEnum.Health) + convert);
			AddToken(name + "_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[a] * 50", "Rolls Per Stage");
			AddScalingFunction("[a] * -1", "Unfavorable Rolls");
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			AddVoidPairing(nameof(RoR2Content.Items.LunarBadLuck));
		}

		private static BuffDef? _buffDef;
		public static BuffDef? BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefTarnished");

		[HarmonyPrefix, HarmonyPatch(typeof(Util), nameof(Util.CheckRoll), typeof(float), typeof(float), typeof(CharacterMaster))]
		public static void UpdateRollsPre(float percentChance, float luck = 0f, CharacterMaster effectOriginMaster = null)
		{
			if (!NetworkServer.active) return;
			if (!effectOriginMaster) return;
			var body = effectOriginMaster.GetBody();
			if (!body) return;
			if (!body.wasLucky) return;
			body.wasLucky = false;
		}

		[HarmonyPostfix, HarmonyPatch(typeof(Util), nameof(Util.CheckRoll), typeof(float), typeof(float), typeof(CharacterMaster))]
		public static void UpdateRolls(bool __result, float percentChance, float luck = 0f, CharacterMaster effectOriginMaster = null)
		{
			if (!NetworkServer.active) return;
			if (!__result) return;
			if (!effectOriginMaster) return;
			var body = effectOriginMaster.GetBody();
			if (!body) return;
			if (!body.wasLucky) return;
			var inv = effectOriginMaster.inventory;
			if (!inv) return;
			var inst = GetInstance<Tarnished>();
			var amount = inv.GetItemCount(inst.ItemDef);
			if (amount <= 0) return;
			body.GetComponent<TarnishedBehavior>().rolls--;
		}
	}
}