using System.Collections.Generic;
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
			AddToken(name + "_DESC", "");
			AddToken(name + "_DESC_SIMPLE", "All random effects are rolled +1 time for a favorable outcome. Only favorable 50 times (+50 per stack) per stage. When inactive, all random effects are rolled +1(+1 per stack) for an unfavorable outcome. Corrupts all Purity.");
			AddToken(name + "_PICKUP", "Convert all your shield into health. Increase maximum shield… BUT your armor is frail. Corrupts all Purity.");
			AddToken(name + "_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[a] * 50", "Rolls Per Stage");
			AddScalingFunction("[a] * 1", "Unfavorable Rolls");
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			AddVoidPairing(nameof(RoR2Content.Items.LunarBadLuck));
		}

		private static BuffDef? _buffDef;
		public static BuffDef? BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefTarnished");

		[HarmonyPostfix, HarmonyPatch(typeof(Util), nameof(Util.CheckRoll), new[]{typeof(float), typeof(float), typeof(CharacterMaster)})]
		public static void UpdateRolls(bool __result, float percentChance, float luck = 0f, CharacterMaster effectOriginMaster = null)
		{
			if (!NetworkServer.active) return;
			if (!__result) return;
			if (!effectOriginMaster) return;
			var body = effectOriginMaster.GetBody();
			if (!body) return;
			var inv = effectOriginMaster.inventory;
			if (!inv) return;
			var inst = GetInstance<Tarnished>();
			var amount = inv.GetItemCount(inst.ItemDef);
			if (amount <= 0) return;
			body.GetComponent<TarnishedBehavior>().rolls--;
		}
	}
}