using System.Collections.Generic;
using BubbetsItems.Helpers;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Items
{
	public class RecursionBullets : ItemBase
	{
		private static RecursionBullets _instance;
		private static BuffDef _buffDef;
		private static BuffDef BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefRecursionBullets");
		public override bool RequiresSotv => true;
		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			pairs.Add(new ItemDef.Pair
			{
				itemDef1 = RoR2Content.Items.BossDamageBonus,
				itemDef2 = ItemDef
			});
		}
		public RecursionBullets()
		{
			_instance = this;
		}
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("RECURSIONBULLETS_NAME", "Recursion Bullets");
			AddToken("RECURSIONBULLETS_DESC", "Attacking a boss increases " + "attack speed by {0:P0}".Style(StyleEnum.Utility) + ". Maximum cap of " + "{1:P0} attack speed.".Style(StyleEnum.Utility) + "\nCorrupts all Armor-Piercing Rounds".Style(StyleEnum.Void));
			AddToken("RECURSIONBULLETS_PICKUP", "Attacking bosses increases attack speed. \n" + "Corrupts Armor-Piercing Rounds.".Style(StyleEnum.Void));
			AddToken("RECURSIONBULLETS_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("0.05", "Per Hit");
			AddScalingFunction("0.1 * [a]", "Max Cap");
			AddScalingFunction("5", "Buff Duration");
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onServerDamageDealt += OnDamage;
		}
		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			GlobalEventManager.onServerDamageDealt -= OnDamage;
		}
		private void OnDamage(DamageReport obj)
		{
			if (!obj.victimIsBoss) return;
			// ReSharper disable thrice Unity.NoNullPropagation
			var body = obj.attacker?.GetComponent<CharacterBody>();
			var inv = body?.inventory;
			var amount = inv?.GetItemCount(ItemDef) ?? 0;
			if (amount <= 0) return;
			
			body.AddTimedBuff(BuffDef, scalingInfos[2].ScalingFunction(amount), Mathf.FloorToInt(scalingInfos[1].ScalingFunction(amount) / scalingInfos[0].ScalingFunction(amount)));
		}
		
		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void RecalcStatsAttackSpeed(CharacterBody __instance)
		{
			var inv = __instance!.inventory;
			var amount = inv?.GetItemCount(_instance.ItemDef) ?? 0;
			if (amount <= 0) return;
			
			var buffAmount = __instance.GetBuffCount(BuffDef);
			var baseAttack = __instance.baseAttackSpeed + __instance.levelAttackSpeed * (__instance.level - 1f);
			__instance.attackSpeed /= baseAttack;
			__instance.attackSpeed *= 1f + buffAmount * _instance.scalingInfos[0].ScalingFunction(amount);
			__instance.attackSpeed *= baseAttack;
		}
	}
}
