using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
using RoR2;

namespace BubbetsItems.Items
{
	public class ScintillatingJet : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("SCINTILLATINGJET_NAME", "Scintillating Jet");
			AddToken("SCINTILLATINGJET_PICKUP", "Reduce damage after getting hit. " + "Corrupts all Oddly-shaped Opals".Style(StyleEnum.Void) + ".");
			AddToken("SCINTILLATINGJET_DESC", "Gain armor ".Style(StyleEnum.Heal) + "temporarily from " + "incoming damage ".Style(StyleEnum.Damage) + "for " + "{0}".Style(StyleEnum.Heal) + ", lasting {1} seconds. " + "Corrupts all Oddly-shaped Opals".Style(StyleEnum.Void) + ".");
			AddToken("SCINTILLATINGJET_LORE", "");
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			StackableChanged();
			AddVoidPairing(nameof(DLC1Content.Items.OutOfCombatArmor));
		}

		public override string GetFormattedDescription(Inventory inventory, string? token = null)
		{
			scalingInfos[0].WorkingContext.b = 1; // Make tooltip not update with buff amount
			return base.GetFormattedDescription(inventory, token);
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("([a] * 10 + 10) * [b]", "Armor amount", new ExpressionContext {b = 1}, "[a] = Item amount, [b] = Buff amount");
			AddScalingFunction("2", "Buff Duration");
			stackable = configFile.Bind(ConfigCategoriesEnum.General, "ScintillatingJet Buff Stackable", false, "Can the buff stack.");
			stackable.SettingChanged += (_,_) => StackableChanged();
		}

		private void StackableChanged()
		{
			BuffDef!.canStack = stackable.Value;
		}

		public ScintillatingJet()
		{
			instance = this;
		}
		private static ScintillatingJet instance;
		private static BuffDef? _buffDef;
		private ConfigEntry<bool> stackable;
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
			//if (body.GetBuffCount(BuffDef) > 0) return; // Make the buff not get added again if you already have it.
			body!.AddTimedBuff(BuffDef, scalingInfos[1].ScalingFunction(count));
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void RecalcStats(CharacterBody __instance)
		{
			if (!__instance) return;
			if (!__instance.inventory) return;
			var info = instance.scalingInfos[0];
			info.WorkingContext.b = __instance.GetBuffCount(BuffDef);
			__instance.armor += info.ScalingFunction(__instance.inventory.GetItemCount(instance.ItemDef));
		}
	}
}