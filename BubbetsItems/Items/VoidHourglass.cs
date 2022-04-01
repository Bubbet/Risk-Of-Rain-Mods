using System.Collections.Generic;
using BubbetsItems.Helpers;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Items
{
	public class VoidHourglass : ItemBase
	{
		public static VoidHourglass Instance;

		public VoidHourglass()
		{
			Instance = this;
		}
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("VOIDHOURGLASS_NAME", "Deficient Clepsydra");
			AddToken("VOIDHOURGLASS_DESC", "The duration of your inflicted Damage Over Times are multiplied by {0}. " + "Corrupts all Abundant Hourglasses.".Style(StyleEnum.Void));
			AddToken("VOIDHOURGLASS_PICKUP", "Duration of inflicted debuffs are extended.");
			AddToken("VOIDHOURGLASS_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("1.15 + 0.1 * [a]", "Debuff Duration");
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			AddVoidPairing("ItemDefHourglass");
		}

		[HarmonyPrefix, HarmonyPatch(typeof(DotController), nameof(DotController.AddDot))]
		public static void ExtendDot(GameObject attackerObject, ref float duration)
		{
			var aBody = attackerObject.GetComponent<CharacterBody>();
			var count = aBody.inventory?.GetItemCount(Instance.ItemDef) ?? 0;
			if (count <= 0) return;
			duration *= Instance.scalingInfos[0].ScalingFunction(count);
		}
	}
}