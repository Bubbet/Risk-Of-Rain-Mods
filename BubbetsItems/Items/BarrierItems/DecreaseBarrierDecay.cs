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
			// Using skills with a cooldown gives 10% barrier. Regenerates when out of combat. Having barrier gives 20 armor.
			AddToken("DECREASEBARRIERDECAY_DESC", "Using skills with a cooldown gives " + "{0:0%} temporary barrier. ".Style(StyleEnum.Utility) + "Having " + "temporary barrier ".Style(StyleEnum.Utility) + "gives " + "{1} armor. ".Style(StyleEnum.Utility) + "Regenerates when out of combat.");
			AddToken("DECREASEBARRIERDECAY_DESC_SIMPLE", "Using skills with a cooldown gives " + "10% temporary barrier. ".Style(StyleEnum.Utility) + "(+5% per stack) ".Style(StyleEnum.Stack) + "Having " + "temporary barrier ".Style(StyleEnum.Utility) + "gives " + "20 armor. ".Style(StyleEnum.Utility) + "(+10 per stack) ".Style(StyleEnum.Stack) + "Regenerates when out of combat.");
			SimpleDescriptionToken = "DECREASEBARRIERDECAY_DESC_SIMPLE";
			AddToken("DECREASEBARRIERDECAY_PICKUP", "Gain barrier for using skills. Reduce damage taken for having barrier.");
			AddToken("DECREASEBARRIERDECAY_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[a] * 0.05 + 0.05", "Barrier Percent Add");
			AddScalingFunction("[a] * 10 + 10", "Armor Add");
		}

		
		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.OnSkillActivated))]
		public static void SkillActivated(CharacterBody __instance, GenericSkill skill)
		{
			if (skill.baseSkill.baseRechargeInterval > 0)
			{
				var inst = GetInstance<DecreaseBarrierDecay>();
				var inv = __instance.inventory;
				var amt = inv.GetItemCount(inst!.ItemDef);
				if (amt <= 0) return;
				__instance.healthComponent.AddBarrier(__instance.healthComponent.fullHealth * inst.scalingInfos[0].ScalingFunction(amt));
			}
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void RecalcStats(CharacterBody __instance)
		{
			DoEffect(__instance);
		}

		private static void DoEffect(CharacterBody characterBody)
		{
			var inst = GetInstance<DecreaseBarrierDecay>();
			var inv = characterBody.inventory;
			if (!inv) return;
			var amt = inv.GetItemCount(inst!.ItemDef);
			if (amt <= 0) return;
			if (characterBody.healthComponent.barrier > 0)
			{
				characterBody.armor += inst.scalingInfos[1].ScalingFunction(amt);
			}
		}
	}
}