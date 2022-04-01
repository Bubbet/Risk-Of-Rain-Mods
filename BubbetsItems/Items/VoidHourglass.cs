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
			AddToken("VOIDHOURGLASS_NAME", "");
			AddToken("VOIDHOURGLASS_DESC", "Increase the duration of your inflicted DOTs by {0:0%}. " + "Corrupts all Abundant Hourglasses.".Style(StyleEnum.Void));
			AddToken("VOIDHOURGLASS_PICKUP", "");
			AddToken("VOIDHOURGLASS_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("0.05 + 0.05 * [a]", "Debuff Duration");
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
			duration *= 1f + Instance.scalingInfos[0].ScalingFunction(count);
		}
	}
}