using System.Collections.Generic;
using BubbetsItems.Helpers;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Items
{
	public class VoidHourglass : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("VOIDHOURGLASS_NAME", "Deficient Clepsydra");
			//AddToken("VOIDHOURGLASS_DESC", "The duration of your inflicted Damage Over Times are multiplied by {0}. " + "Corrupts all Abundant Hourglasses.".Style(StyleEnum.Void));
			//AddToken("VOIDHOURGLASS_PICKUP", "Duration of inflicted debuffs are extended.");
			var convert = "Corrupts all Abundant Hourglasses".Style(StyleEnum.Void);
			AddToken("VOIDHOURGLASS_CONVERT", convert);
			AddToken("VOIDHOURGLASS_DESC", "{0:0%} chance to duplicate " + "damage over time ".Style(StyleEnum.Damage) + "inflictons. ");
			AddToken("VOIDHOURGLASS_DESC_SIMPLE", "30% " + "(+30% per stack) ".Style(StyleEnum.Stack) + "chance to duplicate " + "damage over time ".Style(StyleEnum.Damage) + "inflictons. ");
			SimpleDescriptionToken = "VOIDHOURGLASS_DESC_SIMPLE";
			AddToken("VOIDHOURGLASS_PICKUP", "Chance to duplicate damage over times. " + convert);
            
			AddToken("VOIDHOURGLASS_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			//AddScalingFunction("1.15 + 0.1 * [a]", "Debuff Duration");
			AddScalingFunction("0.1 + 0.20 * [a]", "Duplicate Chance");
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			AddVoidPairing("ItemDefHourglass");
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			DotController.onDotInflictedServerGlobal += DotInflicted;
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			DotController.onDotInflictedServerGlobal -= DotInflicted;
		}
		
		private void DotInflicted(DotController dotcontroller, ref InflictDotInfo inflictDotInfo)
		{
			if (!inflictDotInfo.attackerObject) return;
			var playerBody = inflictDotInfo.attackerObject.GetComponent<CharacterBody>();
			var inv = playerBody.inventory;
			if (!inv) return;
			var amount = inv.GetItemCount(ItemDef);
			if (amount <= 0) return;
			
			var chance = scalingInfos[0].ScalingFunction(amount);
			var leftover = chance - Mathf.Floor(chance);
			var guarenteed = Mathf.FloorToInt(chance - leftover);

			for (var i = 0; i < guarenteed; i++)
				dotcontroller.AddDot(inflictDotInfo.attackerObject, inflictDotInfo.duration, inflictDotInfo.dotIndex, inflictDotInfo.damageMultiplier, inflictDotInfo.maxStacksFromAttacker, inflictDotInfo.totalDamage, inflictDotInfo.preUpgradeDotIndex);
			if (Util.CheckRoll(leftover * 100f, playerBody.master)) 
				dotcontroller.AddDot(inflictDotInfo.attackerObject, inflictDotInfo.duration, inflictDotInfo.dotIndex, inflictDotInfo.damageMultiplier, inflictDotInfo.maxStacksFromAttacker, inflictDotInfo.totalDamage, inflictDotInfo.preUpgradeDotIndex);
		}

		/*
		[HarmonyPrefix, HarmonyPatch(typeof(DotController), nameof(DotController.AddDot))]
		public static void ExtendDot(GameObject attackerObject, ref float duration)
		{
			var aBody = attackerObject.GetComponent<CharacterBody>();
			var count = aBody.inventory?.GetItemCount(Instance.ItemDef) ?? 0;
			if (count <= 0) return;
			duration *= Instance.scalingInfos[0].ScalingFunction(count);
		}*/
	}
}