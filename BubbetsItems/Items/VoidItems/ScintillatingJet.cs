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
			AddToken("SCINTILLATINGJET_PICKUP", "Reduce damage temporarily after getting hit. " + "Corrupts all Oddly-shaped Opals".Style(StyleEnum.Void) + ".");
			AddToken("SCINTILLATINGJET_DESC", "Getting hit " + "increases armor ".Style(StyleEnum.Heal) + "by " + "{0} ".Style(StyleEnum.Heal) + "for {1} seconds. ");
			AddToken("SCINTILLATINGJET_DESC_SIMPLE", "Getting hit " + "increases armor ".Style(StyleEnum.Heal) + "by " + "20 ".Style(StyleEnum.Heal) + "(+10 per stack) ".Style(StyleEnum.Stack) + "for 2 seconds. ");
			SimpleDescriptionToken = "SCINTILLATINGJET_DESC_SIMPLE";
			AddToken("SCINTILLATINGJET_LORE", "\"What do you mean Jet isn't a gemstone? It clearly is!\"");
		}
		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("([a] * 10 + 10) * [b]", "Armor amount", "[a] = Item amount, [b] = Buff amount");
			AddScalingFunction("2", "Buff Duration");
			stackable = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "ScintillatingJet Buff Stackable", false, "Can the buff stack.");
			stackable.SettingChanged += (_,_) => StackableChanged();
		}
		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			StackableChanged();
			AddVoidPairing(nameof(DLC1Content.Items.OutOfCombatArmor));
		}
		private void StackableChanged()
		{
			BuffDef!.canStack = stackable.Value;
		}
		public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
		{
			scalingInfos[0].WorkingContext.b = 1; // Make tooltip not update with buff amount
			return base.GetFormattedDescription(inventory, token, forceHideExtended);
		}
		private ConfigEntry<bool> stackable;
		private static BuffDef? _buffDef;
		private static BuffDef? BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefScintillatingJet");

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onServerDamageDealt += DamageDealt;
			Inventory.onInventoryChangedGlobal += CleanupOpal;
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			GlobalEventManager.onServerDamageDealt -= DamageDealt;
			Inventory.onInventoryChangedGlobal -= CleanupOpal;
		}
		
		private void CleanupOpal(Inventory obj)
		{
			if (obj.GetItemCount(DLC1Content.Items.OutOfCombatArmor) == 0)
			{
				var body = obj.GetComponent<CharacterMaster>()?.GetBody();
				if (body && body!.HasBuff(DLC1Content.Buffs.OutOfCombatArmorBuff))
					body.RemoveBuff(DLC1Content.Buffs.OutOfCombatArmorBuff);
			}
		}

		private void DamageDealt(DamageReport obj)
		{
			var body = obj.victim.body;
			var inv = body?.inventory;
			var count = inv?.GetItemCount(ItemDef) ?? 0;
			if (count <= 0) return;
			if (!stackable.Value && body!.GetBuffCount(BuffDef) > 0) return; // Make the buff not get added again if you already have it.
			body!.AddTimedBuff(BuffDef, scalingInfos[1].ScalingFunction(count));
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void RecalcStats(CharacterBody __instance)
		{
			if (!__instance) return;
			if (!__instance.inventory) return;
			var instance = GetInstance<ScintillatingJet>();
			var info = instance.scalingInfos[0];
			info.WorkingContext.b = __instance.GetBuffCount(BuffDef);
			__instance.armor += info.ScalingFunction(__instance.inventory.GetItemCount(instance.ItemDef));
		}
	}
}