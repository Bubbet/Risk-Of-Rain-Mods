using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BubbetsItems.Components;
using BubbetsItems.Helpers;
using HarmonyLib;
using R2API;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using RoR2.Skills;

namespace BubbetsItems.Items
{
	public class DecreaseBarrierDecay : ItemBase
	{
		public ConfigEntry<string> skillBlacklist;
		public ConfigEntry<string> skillWhitelist;
		private static List<SkillDef> skillDefBlacklist;
		private static List<SkillDef> skillDefWhitelist;

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("DECREASEBARRIERDECAY_NAME", "Mechanical Snail");
			// Using skills with a cooldown gives 10% barrier. Regenerates when out of combat. Having barrier gives 20 armor.
			AddToken("DECREASEBARRIERDECAY_DESC", "Using skills with a cooldown gives " + "{0:0%} temporary barrier per second of cooldown. ".Style(StyleEnum.Heal) + "Having " + "temporary barrier ".Style(StyleEnum.Heal) + "gives " + "{1} armor. ".Style(StyleEnum.Utility) + "Regenerates when out of combat.");
			AddToken("DECREASEBARRIERDECAY_DESC_SIMPLE", "Using skills with a cooldown gives " + "10% temporary barrier. ".Style(StyleEnum.Heal) + "(+5% per stack) ".Style(StyleEnum.Stack) + "Having " + "temporary barrier ".Style(StyleEnum.Heal) + "gives " + "20 armor. ".Style(StyleEnum.Utility) + "(+10 per stack) ".Style(StyleEnum.Stack) + "Regenerates when out of combat.");
			SimpleDescriptionToken = "DECREASEBARRIERDECAY_DESC_SIMPLE";
			AddToken("DECREASEBARRIERDECAY_PICKUP", "Gain barrier for using skills. Reduce damage taken for having barrier.");
			AddToken("DECREASEBARRIERDECAY_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[a] * 0.01 * [c]", "Barrier Percent Add", desc: "[a] = item count; [c] = skill cooldown");
			AddScalingFunction("[a] * 10 + 10", "Armor Add");
		}

		protected override void FillRequiredExpansions()
		{
			base.FillRequiredExpansions();
			
			var defaultValue = "";
			var values = string.Join(" ", SkillCatalog._allSkillDefs.Select(x => x.skillName));
			skillBlacklist = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Mechanical Snail Blacklist", defaultValue, "Skills to not give barrier for, Valid values: " + values);
			skillBlacklist.SettingChanged += (_, _) => SettingChanged();
			skillWhitelist = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Mechanical Snail Whitelist", defaultValue, "Skills to bypass cooldown/stock check and give barrier for, Valid values: " + values);
			skillWhitelist.SettingChanged += (_, _) => SettingChanged();
			SettingChanged();

			if (BubbetsItemsPlugin.riskOfOptionsEnabled)
			{
				MakeRiskOfOptionsLate();
			}
		}

		private void MakeRiskOfOptionsLate()
		{
			ModSettingsManager.AddOption(new StringInputFieldOption(skillWhitelist));
			ModSettingsManager.AddOption(new StringInputFieldOption(skillBlacklist));
		}

		private void SettingChanged()
		{
			skillDefBlacklist = skillBlacklist.Value.Split(' ')
				.Select(SkillCatalog.FindSkillIndexByName)
				.Where(index => index != -1)
				.Select(SkillCatalog.GetSkillDef).ToList();
			skillDefWhitelist = skillWhitelist.Value.Split(' ')
				.Select(SkillCatalog.FindSkillIndexByName)
				.Where(index => index != -1)
				.Select(SkillCatalog.GetSkillDef).ToList();
		}

		public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
		{
			scalingInfos[0].WorkingContext.c = 1;
			return base.GetFormattedDescription(inventory, token, forceHideExtended);
		}


		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.OnSkillActivated))]
		public static void SkillActivated(CharacterBody __instance, GenericSkill skill)
		{
			if (!skillDefWhitelist.Contains(skill.baseSkill) && (skill.baseSkill.baseRechargeInterval <= 0.001 || skill.baseSkill.stockToConsume == 0) || skillDefBlacklist.Contains(skill.baseSkill)) return;
			var inst = GetInstance<DecreaseBarrierDecay>();
			var inv = __instance.inventory;
			var amt = inv.GetItemCount(inst!.ItemDef);
			if (amt <= 0) return;
			var info = inst.scalingInfos[0];
			info.WorkingContext.c = skill.baseSkill.baseRechargeInterval;
			__instance.healthComponent.AddBarrier(__instance.healthComponent.fullHealth * info.ScalingFunction(amt));
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			RecalculateStatsAPI.GetStatCoefficients += RecalcStats;
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			RecalculateStatsAPI.GetStatCoefficients -= RecalcStats;
		}

		public static void RecalcStats(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
		{
			var inst = GetInstance<DecreaseBarrierDecay>();
			var inv = characterBody.inventory;
			if (!inv) return;
			var amt = inv.GetItemCount(inst!.ItemDef);
			if (amt <= 0) return;
			if (characterBody.healthComponent.barrier > 0) args.armorAdd += inst.scalingInfos[1].ScalingFunction(amt);
		}
	}
}