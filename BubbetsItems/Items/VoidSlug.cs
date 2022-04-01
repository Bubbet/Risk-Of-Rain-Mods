using System.Collections.Generic;
using BubbetsItems.Helpers;
using HarmonyLib;
using RoR2;

namespace BubbetsItems.Items
{
	public class VoidSlug : ItemBase
	{
		public static VoidSlug Instance;
		public VoidSlug()
		{
			Instance = this;
		}
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("VOIDSLUG_NAME", "Void Slug");
			AddToken("VOIDSLUG_DESC", "Gain {0} regen per missing health. " + "Corrupts all Cautious Slug".Style(StyleEnum.Void));
			AddToken("VOIDSLUG_PICKUP", "Gain regen for missing health.");
			AddToken("VOIDSLUG_LORE", "");
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			AddVoidPairing(nameof(RoR2Content.Items.HealWhileSafe));
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[h] * 0.005 * [a] + 0.0196", "Regen", new ExpressionContext {h = 1}, "[h] = Missing health, [a] = Item count");
		}

		public override string GetFormattedDescription(Inventory inventory, string? token = null)
		{
			scalingInfos[0].WorkingContext.h = 1f;
			return base.GetFormattedDescription(inventory, token);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.Heal))]
		private static void HealServer(HealthComponent __instance)
		{
			var count = __instance.body?.inventory?.GetItemCount(Instance.ItemDef) ?? 0;
			if (count <= 0 || __instance.missingCombinedHealth < 0.1f) return;
			__instance.body?.RecalculateStats();
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void RecalcStats(CharacterBody __instance)
		{
			var count = __instance.inventory?.GetItemCount(Instance.ItemDef) ?? 0;
			if (count <= 0) return;
			var info = Instance.scalingInfos[0];
			info.WorkingContext.h = __instance.healthComponent.missingCombinedHealth;
			__instance.regen += info.ScalingFunction(count);
		}
	}
}