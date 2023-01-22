using BubbetsItems.Components;
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
			AddToken("DECREASEBARRIERDECAY_DESC", "Slows " + "barrier decay ".Style(StyleEnum.Heal) + "by " + "{0:0%}".Style(StyleEnum.Heal) + ".");
			AddToken("DECREASEBARRIERDECAY_DESC_SIMPLE", "Slows " + "barrier decay ".Style(StyleEnum.Heal) + "by " + "17% ".Style(StyleEnum.Heal) + "(stacks exponentially)".Style(StyleEnum.Stack) + ".");
			SimpleDescriptionToken = "DECREASEBARRIERDECAY_DESC_SIMPLE";
			AddToken("DECREASEBARRIERDECAY_PICKUP", "Slow down barrier decay.");
			AddToken("DECREASEBARRIERDECAY_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("1 - 1 / ([a] * 0.2 + 1)", "Barrier Decay Mult", oldDefault: "1 / ([a] * 0.2 + 1)");
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			CommonBodyPatches.CollectExtraStats += GetBarrierDecay;
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			CommonBodyPatches.CollectExtraStats -= GetBarrierDecay;
		}

		private void GetBarrierDecay(CommonBodyPatches.ExtraStats obj)
		{
			var count = obj.inventory.GetItemCount(ItemDef);
			if (count <= 0) return;
			obj.barrierDecay += scalingInfos[0].ScalingFunction(count);
		}
	}
}