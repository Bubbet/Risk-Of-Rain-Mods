using BepInEx.Configuration;
using BubbetsItems.Helpers;
using RoR2;

namespace BubbetsItems.Items
{
	//TODO make tethering effect and item behavior, and tethering controller
	public class SubmergingCistern : ItemBase
	{
		
		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[d] * 0.5", "Healing From Damage", "[a] = item count; [d] = damage dealt");
			AddScalingFunction("[a] + 2", "Teammate Count");
			AddScalingFunction("20", "Range");
			//_range = configFile.Bind(ConfigCategoriesEnum.BalancingFunctions, "Submerging Cistern Range", 20f, "Range for the Submerging Cistern to heal within.");
			//_amount = configFile.Bind(ConfigCategoriesEnum.BalancingFunctions, "Submerging Cistern Damage", 0.5f, "Damage percent to heal.");
		}

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("SUBMERGINGCISTERN_NAME", "Submerging Cistern");
			AddToken("SUBMERGINGCISTERN_DESC", "While in combat, the nearest {1} allies within " + "{2}m ".Style(StyleEnum.Utility) + "of you will be 'tethered'. Dealing damage will " + "heal ".Style(StyleEnum.Heal) + "tethered allies for " + "{0:0%} ".Style(StyleEnum.Utility) + "of damage dealth. " + "Healing ".Style(StyleEnum.Heal) + "is divided among allies. " + "Corrupts all Mired Urns".Style(StyleEnum.Void) + ".");
			AddToken("SUBMERGINGCISTERN_PICKUP", "Heal nearby allies based on your damage. Divided over teammates in range.  " + "Consumes Mired Urn".Style(StyleEnum.Void) + ".");
			AddToken("SUBMERGINGCISTERN_LORE", "");
		}

		public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
		{
			scalingInfos[0].WorkingContext.d = 1f;
			return base.GetFormattedDescription(inventory, token, forceHideExtended);
		}
	}
}