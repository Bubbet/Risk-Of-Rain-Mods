using HarmonyLib;
using RoR2;

namespace BubbetsItems.Items
{
	public class DecreaseBarrierDecay : ItemBase
	{
		public static DecreaseBarrierDecay Instance;

		public DecreaseBarrierDecay()
		{
			Instance = this;
		}

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("DECREASEBARRIERDECAY_NAME", "");
			AddToken("DECREASEBARRIERDECAY_DESC", "Multiplies barrier decay by {0}.");
			AddToken("DECREASEBARRIERDECAY_PICKUP", "");
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
			var count = __instance.inventory?.GetItemCount(Instance.ItemDef) ?? 0;
			if (count <= 0) return;
			__instance.barrierDecayRate *= Instance.scalingInfos[0].ScalingFunction(count);
		}
	}
}