using HarmonyLib;
using RoR2;

namespace BubbetsItems.Items.BarrierItems
{
	public class CeremonialProbe : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("CEREMONIALPROBE_NAME", "Ceremonial Probe");
			AddToken("CEREMONIALPROBE_DESC", "When below {0:0%} health, freeze barrier decay.");
			AddToken("CEREMONIALPROBE_PICKUP", "");
			AddToken("CEREMONIALPROBE_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("Min(1, 1.5 - Pow(20/([a]+20), 2))", "Health threshold");
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onServerDamageDealt += OnHit;
		}
		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			GlobalEventManager.onServerDamageDealt -= OnHit;
		}
		private void OnHit(DamageReport obj)
		{
			if (!obj.victim) return;
			var body = obj.victim.body;
			if (!body) return;
			DoEffect(body);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void RecalcStats(CharacterBody __instance)
		{
			DoEffect(__instance);
		}

		public static void DoEffect(CharacterBody body)
		{
			var inst = GetInstance<CeremonialProbe>();
			if (inst == null) return;
			var inv = body.inventory;
			if (!inv) return;
			var amount = inv.GetItemCount(inst.ItemDef);
			if (amount <= 0) return;
			if (body.healthComponent.health / body.healthComponent.fullHealth < inst.scalingInfos[0].ScalingFunction(amount))
				body.barrierDecayRate = 0f;
		}

	}
}