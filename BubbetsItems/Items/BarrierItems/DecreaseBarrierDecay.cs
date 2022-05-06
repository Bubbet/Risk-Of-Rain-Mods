using BubbetsItems.Helpers;
using HarmonyLib;
using RoR2;

namespace BubbetsItems.Items
{
	public class DecreaseBarrierDecay : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("DECREASEBARRIERDECAY_NAME", "Mechanical Snail");
			AddToken("DECREASEBARRIERDECAY_DESC", "Multiplies " + "barrier decay ".Style(StyleEnum.Heal) + "by " + "{0:0%}".Style(StyleEnum.Heal) + ".");
			AddToken("DECREASEBARRIERDECAY_PICKUP", "Slow down barrier decay.");
			AddToken("DECREASEBARRIERDECAY_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("1 / ([a] * 0.2 + 1)", "Barrier Decay Mult");
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void FixBarrier(CharacterBody __instance)
		{
			var inv = __instance.inventory;
			if (!inv) return;
			var instance = GetInstance<DecreaseBarrierDecay>();
			if (instance == null) return;
			var count = inv.GetItemCount(instance.ItemDef);
			if (count <= 0) return;
			__instance.barrierDecayRate *= instance.scalingInfos[0].ScalingFunction(count);
		}
	}
}