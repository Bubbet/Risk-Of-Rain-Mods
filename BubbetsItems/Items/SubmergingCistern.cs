using System.Collections.Generic;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Items
{
	//TODO make tethering effect and item behavior, and tethering controller
	public class SubmergingCistern : ItemBase
	{
		private ConfigEntry<float> _dropChance;
		public ConfigEntry<bool> ignoreHealNova;

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			_dropChance = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Submerging Cistern Drop Chance", 0.5f, "Drop chance of submerging cistern");
			AddScalingFunction("[d] * 0.5", "Healing From Damage", "[a] = item count; [d] = damage dealt");
			AddScalingFunction("[a] + 2", "Teammate Count");
			AddScalingFunction("20", "Range");
			ignoreHealNova = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General,
				"SubmergingCistern Dont Proc HealNova", true, "Disable procs of N'kuhana's Opinion");
			//_range = configFile.Bind(ConfigCategoriesEnum.BalancingFunctions, "Submerging Cistern Range", 20f, "Range for the Submerging Cistern to heal within.");
			//_amount = configFile.Bind(ConfigCategoriesEnum.BalancingFunctions, "Submerging Cistern Damage", 0.5f, "Damage percent to heal.");
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			AddVoidPairing(nameof(RoR2Content.Items.SiphonOnLowHealth));
		}

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("SUBMERGINGCISTERN_NAME", "Submerging Cistern");
			AddToken("SUBMERGINGCISTERN_DESC", "While in combat, the nearest {1} allies within " + "{2}m ".Style(StyleEnum.Utility) + "of you will be 'tethered'. Dealing damage will " + "heal ".Style(StyleEnum.Heal) + "tethered allies for " + "{0:0%} ".Style(StyleEnum.Utility) + "of damage dealth. " + "Healing ".Style(StyleEnum.Heal) + "is divided among allies. " + "Corrupts all Mired Urns".Style(StyleEnum.Void) + ".");
			AddToken("SUBMERGINGCISTERN_DESC_SIMPLE", "While in combat, the nearest 3" + "(+1 per stack) ".Style(StyleEnum.Stack) + "allies within " + "20m ".Style(StyleEnum.Utility) + "of you will be 'tethered'. Dealing damage will " + "heal ".Style(StyleEnum.Heal) + "tethered allies for " + "50% ".Style(StyleEnum.Utility) + "of damage dealth. " + "Healing ".Style(StyleEnum.Heal) + "is divided among allies. " + "Corrupts all Mired Urns".Style(StyleEnum.Void) + ".");
			SimpleDescriptionToken = "SUBMERGINGCISTERN_DESC_SIMPLE";
			AddToken("SUBMERGINGCISTERN_PICKUP", "Heal nearby allies based on your damage. Divided over teammates in range.  " + "Consumes Mired Urn".Style(StyleEnum.Void) + ".");
			AddToken("SUBMERGINGCISTERN_LORE", "");
		}

		public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
		{
			scalingInfos[0].WorkingContext.d = 1f;
			return base.GetFormattedDescription(inventory, token, forceHideExtended);
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onCharacterDeathGlobal += CharacterDeath;
		}

		private void CharacterDeath(DamageReport obj)
		{
			var body = obj.victimBody;
			if (!body || !obj.attackerMaster || obj.victimBodyIndex != BodyCatalog.FindBodyIndex("ClayBossBody") || !body.isElite || !body.HasBuff(DLC1Content.Buffs.EliteVoid)) return;
			if (Util.CheckRoll(_dropChance.Value * 100f, obj.attackerMaster))
				PickupDropletController.CreatePickupDroplet(PickupIndex, body.transform.position, Vector3.up * 3f);
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			GlobalEventManager.onCharacterDeathGlobal -= CharacterDeath;
		}
	}
}