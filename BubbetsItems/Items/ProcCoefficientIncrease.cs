using HarmonyLib;
using RoR2;

namespace BubbetsItems.Items
{
	public class ProcCoefficientIncrease : ItemBase
	{
		public static ProcCoefficientIncrease Instance;

		public ProcCoefficientIncrease()
		{
			Instance = this;
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("1 + [a] * 0.3", "Proc Coefficient");
		}

		[HarmonyPrefix, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
		public static void ApplyCoefficent(HealthComponent __instance, ref DamageInfo damageInfo)
		{
			var count = damageInfo.attacker?.GetComponent<CharacterBody>()?.inventory?.GetItemCount(Instance.ItemDef) ?? 0;
			if (count <= 0) return;
			damageInfo.procCoefficient *= Instance.scalingInfos[0].ScalingFunction(count);
		}
	}
}