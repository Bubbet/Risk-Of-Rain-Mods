using BubbetsItems.Helpers;
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
			AddToken("CEREMONIALPROBE_DESC", "Falling bellow " + "{0:0%} health ".Style(StyleEnum.Health) + " consumes this item and gives you " + "{1:0%} temporary barrier. ".Style(StyleEnum.Utility) + "Regenerates next stage.");
			AddToken("CEREMONIALPROBE_DESC_SIMPLE", "Falling bellow " + "35% health ".Style(StyleEnum.Health) + " consumes this item and gives you " + "75% temporary barrier. ".Style(StyleEnum.Utility) + "Regenerates next stage.");
			SimpleDescriptionToken = "CEREMONIALPROBE_DESC_SIMPLE";
			AddToken("CEREMONIALPROBE_PICKUP", "Get barrier at low health.");
			AddToken("CEREMONIALPROBE_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("0.35", "Health Threshold");
			AddScalingFunction("0.75", "Barrier Add Percent");
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
			if (body.healthComponent.combinedHealth / body.healthComponent.fullCombinedHealth <
			    inst.scalingInfos[0].ScalingFunction(amount))
			{
				body.healthComponent.AddBarrier(body.healthComponent.fullCombinedHealth *
				                                inst.scalingInfos[1].ScalingFunction(amount));
				var broke = GetInstance<BrokenCeremonialProbe>()!.ItemDef;
				body.inventory.RemoveItem(inst.ItemDef);
				body.inventory.GiveItem(broke);
				CharacterMasterNotificationQueue.SendTransformNotification(body.master, inst.ItemDef.itemIndex, broke.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
			}
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CharacterMaster), nameof(CharacterMaster.OnServerStageBegin))]
		public static void RegenItem(CharacterMaster __instance)
		{
			var broke = GetInstance<BrokenCeremonialProbe>()!.ItemDef;
			var regular = GetInstance<CeremonialProbe>()!.ItemDef;
			var itemCount = __instance.inventory.GetItemCount(broke);
			if (itemCount <= 0) return;
			__instance.inventory.RemoveItem(broke, itemCount);
			__instance.inventory.GiveItem(regular, itemCount);
			CharacterMasterNotificationQueue.SendTransformNotification(__instance, broke.itemIndex, regular.itemIndex, CharacterMasterNotificationQueue.TransformationType.RegeneratingScrapRegen);
		}
	}
}