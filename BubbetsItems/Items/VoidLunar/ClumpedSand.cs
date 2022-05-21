using System.Collections.Generic;
using HarmonyLib;
using RoR2;

namespace BubbetsItems.Items.VoidLunar
{
	public class ClumpedSand : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			var name = GetType().Name.ToUpper();
			SimpleDescriptionToken = name + "_DESC_SIMPLE";
			AddToken(name + "_NAME", "Clumped Sand");
			AddToken(name + "_DESC", "");
			AddToken(name + "_DESC_SIMPLE", "All attacks hit 1 (+1 per stack) more times for 50% base damage. Your health regeneration is now -3/s (-3/s per stack). Corrupts all Shaped Glass.");
			AddToken(name + "_PICKUP", "Damage is dealt again at a weaker state… BUT gain negative regeneration. Corrupts all Shaped Glass.");
			AddToken(name + "_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[a]", "Attack Hit Count");
			AddScalingFunction("3 * [a]", "Regen Remove");
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			AddVoidPairing(nameof(RoR2Content.Items.LunarDagger));
		}


		public static DamageInfo? mostRecentInfo;
		[HarmonyPrefix, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
		public static void DuplicateDamage(HealthComponent __instance, DamageInfo damageInfo)
		{
			if (mostRecentInfo == null)
			{
				if (!damageInfo.attacker) return;
				var body = damageInfo.attacker.GetComponent<CharacterBody>();
				if (!body) return;
				var inv = body.inventory;
				if (!inv) return;
				var inst = GetInstance<ClumpedSand>();
				var amount = inv.GetItemCount(inst.ItemDef);
				if (amount <= 0) return;
				mostRecentInfo = damageInfo;
				for (var i = 0; i < inst.scalingInfos[0].ScalingFunction(amount); i++)
				{
					__instance.TakeDamage(damageInfo);
				}
				mostRecentInfo = null;
			}
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void ReduceRegen(CharacterBody __instance)
		{
			if (!__instance) return;
			var inv = __instance.inventory;
			if (!inv) return;
			var inst = GetInstance<ClumpedSand>();
			var amount = inv.GetItemCount(inst.ItemDef);
			if (amount <= 0) return;
			__instance.regen -= inst.scalingInfos[1].ScalingFunction(amount);
		}
	}
}