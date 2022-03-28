namespace BubbetsItems.Items
{
	public class AcidSoakedBlindfold : ItemBase
	{
		public static AcidSoakedBlindfold instance;
		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("1", "Vermin Count");
			AddScalingFunction("[a] * 5 + 5", "Item Count");
			AddScalingFunction("0.2", "Green Item Chance"); 
		}

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("ACIDSOAKEDBLINDFOLD_NAME", "Acid Soaked Blindfold");
			AddToken("ACIDSOAKEDBLINDFOLD_DESC", "Gain {0} blind vermin ally with {1} green or white items.");
			AddToken("ACIDSOAKEDBLINDFOLD_PICKUP", "Gain a blind vermin ally.");
			AddToken("ACIDSOAKEDBLINDFOLD_LORE", "");
		}

		public AcidSoakedBlindfold()
		{
			instance = this;
		}
	}
}