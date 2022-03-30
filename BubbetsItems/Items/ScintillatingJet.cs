using System.Collections.Generic;
using HarmonyLib;
using RoR2;

namespace BubbetsItems.Items
{
	public class ScintillatingJet : ItemBase
	{
		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			AddVoidPairing(nameof(DLC1Content.Items.OutOfCombatArmor));
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("([a] * 10 + 10) * [b]", "Armor amount", new ExpressionContext {b = 1}, "[a] = Item amount, [b] = Buff amount");
			AddScalingFunction("2", "Buff Duration");
		}

		public ScintillatingJet()
		{
			instance = this;
		}
		private static ScintillatingJet instance;
		private static BuffDef? _buffDef;
		private static BuffDef? BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefScintillatingJet");

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onServerDamageDealt += DamageDealt;
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			GlobalEventManager.onServerDamageDealt -= DamageDealt;
		}

		private void DamageDealt(DamageReport obj)
		{
			var body = obj.victim.body;
			var inv = body?.inventory;
			var count = inv?.GetItemCount(ItemDef) ?? 0;
			if (count <= 0) return;
			body!.AddTimedBuff(BuffDef, scalingInfos[1].ScalingFunction(count));
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void RecalcStats(CharacterBody __instance)
		{
			var info = instance.scalingInfos[0];
			info.WorkingContext.b = __instance.GetBuffCount(BuffDef);
			__instance.armor += info.ScalingFunction(__instance.inventory.GetItemCount(instance.ItemDef));
		}
	}
}