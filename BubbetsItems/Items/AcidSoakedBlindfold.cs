using BubbetsItems.Bases;
using BubbetsItems.Helpers;

namespace BubbetsItems.Items
{
	public class AcidSoakedBlindfold : ItemBase
	{
		public static AcidSoakedBlindfold? Instance;
		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("1", "Vermin Count");
			AddScalingFunction("[a] * 5 + 5", "Item Count");
			AddScalingFunction("0.2", "Green Item Chance"); 
		}

		protected override void MakeTokens()
		{
			// Where III is located in ACIDSOAKEDBLINDFOLD_DESC, create a new config for spawn time please
			base.MakeTokens();
			AddToken("ACIDSOAKEDBLINDFOLD_NAME", "Acid Soaked Blindfold");
			AddToken("ACIDSOAKEDBLINDFOLD_PICKUP", "Recruit a Blind Vermin with items.");
			AddToken("ACIDSOAKEDBLINDFOLD_DESC", "Every III seconds, " + "summon a Blind Vermin".Style(StyleEnum.Utility) + "with " + "{1} ".Style(StyleEnum.Utility) + "Common ".Style(StyleEnum.White) + "or " + "Uncommon ".Style(StyleEnum.Green) + "items.");
			AddToken("ACIDSOAKEDBLINDFOLD_LORE", "What is that smell?");
		}

		public AcidSoakedBlindfold()
		{
			Instance = this;
		}
	}
}