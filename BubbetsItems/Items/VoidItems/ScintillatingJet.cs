using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
using R2API;
using RiskOfOptions.Options;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;

namespace BubbetsItems.Items
{
	public class ScintillatingJet : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("SCINTILLATINGJET_NAME", "Scintillating Jet");
			var convert = "Corrupts all Oddly Shaped Opals".Style(StyleEnum.Void) + ".";
			AddToken("SCINTILLATINGJET_CONVERT", convert);
			AddToken("SCINTILLATINGJET_PICKUP", "Reduce damage temporarily after getting hit. " + "Corrupts all Oddly-shaped Opals".Style(StyleEnum.Void) + ". " + convert);
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
			AddVoidPairing(nameof(DLC1Content.Items.OutOfCombatArmor));
		}

		protected override void FillRequiredExpansions()
		{
			StackableChanged();
			base.FillRequiredExpansions();
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
		public static BuffDef? BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefScintillatingJet");
		protected override void FillDefsFromSerializableCP(SerializableContentPack serializableContentPack)
		{
			base.FillDefsFromSerializableCP(serializableContentPack);
			// yeahh code based content because TK keeps fucking freezing
			var buff = ScriptableObject.CreateInstance<BuffDef>();
			buff.name = "BuffDefScintillatingJet";
			buff.iconSprite = BubbetsItemsPlugin.AssetBundle.LoadAsset<Sprite>("ScintillatingBuffIcon");
			serializableContentPack.buffDefs = serializableContentPack.buffDefs.AddItem(buff).ToArray();
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onServerDamageDealt += DamageDealt;
			RecalculateStatsAPI.GetStatCoefficients += RecalcStats;
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			GlobalEventManager.onServerDamageDealt -= DamageDealt;
			RecalculateStatsAPI.GetStatCoefficients -= RecalcStats;
		}

		public override void MakeRiskOfOptions()
		{
			base.MakeRiskOfOptions();
			RiskOfOptions.ModSettingsManager.AddOption(new CheckBoxOption(stackable));
		}

		private void DamageDealt(DamageReport obj)
		{
			var body = obj.victim.body;
			if (!body) return;
			var inv = body.inventory;
			if (!inv) return;
			var count = inv.GetItemCount(ItemDef);
			if (count <= 0) return;
			if (!stackable.Value && body.GetBuffCount(BuffDef) > 0) return; // Make the buff not get added again if you already have it.
			body.AddTimedBuff(BuffDef, scalingInfos[1].ScalingFunction(count));
		}
		
		
		public static void RecalcStats(CharacterBody __instance, RecalculateStatsAPI.StatHookEventArgs args)
		{
			if (!__instance) return;
			if (!__instance.inventory) return;
			var instance = GetInstance<ScintillatingJet>();
			var info = instance.scalingInfos[0];
			info.WorkingContext.b = __instance.GetBuffCount(BuffDef);
			args.armorAdd += info.ScalingFunction(__instance.inventory.GetItemCount(instance.ItemDef));
		}
	}
}