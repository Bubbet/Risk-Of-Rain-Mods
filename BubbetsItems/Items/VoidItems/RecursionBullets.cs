using System.Collections.Generic;
using System.Linq;
using BubbetsItems.Helpers;
using HarmonyLib;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;

namespace BubbetsItems.Items
{
	public class RecursionBullets : ItemBase
	{
		
		private static BuffDef? _buffDef;
		public static BuffDef? BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefRecursionBullets");
		protected override void FillDefsFromSerializableCP(SerializableContentPack serializableContentPack)
		{
			base.FillDefsFromSerializableCP(serializableContentPack);
			// yeahh code based content because TK keeps fucking freezing
			var buff = ScriptableObject.CreateInstance<BuffDef>();
			buff.canStack = true;
			buff.name = "BuffDefRecursionBullets";
			buff.iconSprite = BubbetsItemsPlugin.AssetBundle.LoadAsset<Sprite>("Rec"); 
			serializableContentPack.buffDefs = serializableContentPack.buffDefs.AddItem(buff).ToArray();
		}
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("RECURSIONBULLETS_NAME", "Recursion Bullets");
			var convert = "Corrupts all Armor-Piercing Rounds".Style(StyleEnum.Void) + ".";
			AddToken("RECURSIONBULLETS_CONVERT", convert);
			AddToken("RECURSIONBULLETS_PICKUP", "Attacking bosses increases attack speed. " + convert);
			AddToken("RECURSIONBULLETS_DESC", "Attacking bosses increases " + "attack speed ".Style(StyleEnum.Damage) + "by " + "{0:0%}".Style(StyleEnum.Damage) + ". Maximum cap of " + "{1:0%} attack speed".Style(StyleEnum.Damage) + ". " );
			AddToken("RECURSIONBULLETS_DESC_SIMPLE", "Attacking bosses increases " + "attack speed ".Style(StyleEnum.Damage) + "by " + "5% ".Style(StyleEnum.Damage) + ", caps at " + "10% ".Style(StyleEnum.Damage) + "(+10% per stack) ".Style(StyleEnum.Stack) + "attack speed".Style(StyleEnum.Damage) + ". ");
			SimpleDescriptionToken = "RECURSIONBULLETS_DESC_SIMPLE";
			AddToken("RECURSIONBULLETS_LORE", "\"I just shot these unusual bullets that I found buried near an abandoned testing site, and they appeared back in my magazine. I stumbled across something I shouldn't have... This stuff gives off a strange aura whenever I use it. They don't get damaged either... Maybe I should've left them where I found them. For now, I'll keep them locked up in the warehouse.\"");
		}
		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("0.05", "Per Hit");
			AddScalingFunction("0.1 * [a]", "Max Cap");
			AddScalingFunction("5", "Buff Duration");
		}
		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			AddVoidPairing("BossDamageBonus");
		}
		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onServerDamageDealt += OnDamage;
			RecalculateStatsAPI.GetStatCoefficients += RecalcStats;
		}
		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			GlobalEventManager.onServerDamageDealt -= OnDamage;
			RecalculateStatsAPI.GetStatCoefficients -= RecalcStats;
		}
		private void OnDamage(DamageReport obj)
		{
			if (!obj.victimIsBoss) return;
			if (!obj.attacker) return;
			var body = obj.attacker.GetComponent<CharacterBody>();
			var inv = body?.inventory;
			if (!inv) return;
			var amount = inv!.GetItemCount(ItemDef);
			if (amount <= 0) return;


			body!.AddTimedBuff(BuffDef, scalingInfos[2].ScalingFunction(amount), Mathf.FloorToInt(scalingInfos[1].ScalingFunction(amount) / scalingInfos[0].ScalingFunction(amount)));
		}
		
		
		public static void RecalcStats(CharacterBody __instance, RecalculateStatsAPI.StatHookEventArgs args)
		{
			var inv = __instance!.inventory;
			if (!inv) return;
			var recursionBullets = GetInstance<RecursionBullets>()!;
			var amount = inv.GetItemCount(recursionBullets.ItemDef);
			if (amount <= 0) return;
			
			var buffAmount = __instance.GetBuffCount(BuffDef);
			//var baseAttack = __instance.baseAttackSpeed + __instance.levelAttackSpeed * (__instance.level - 1f);
			args.attackSpeedMultAdd += 1f + buffAmount * recursionBullets.scalingInfos[0].ScalingFunction(amount); 
			//__instance.attackSpeed /= baseAttack;
			//__instance.attackSpeed *= baseAttack;
		}
	}
}
