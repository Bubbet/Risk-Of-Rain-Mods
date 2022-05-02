using BubbetsItems.Helpers;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Items
{
	public class AcidSoakedBlindfold : ItemBase
	{
		protected override void MakeTokens()
		{
			// Where III is located in ACIDSOAKEDBLINDFOLD_DESC, create a new config for spawn time please
			base.MakeTokens();
			AddToken("ACIDSOAKEDBLINDFOLD_NAME", "Acid Soaked Blindfold");
			AddToken("ACIDSOAKEDBLINDFOLD_PICKUP", "Recruit a Blind Vermin with items.");
			AddToken("ACIDSOAKEDBLINDFOLD_DESC", "Every {3} seconds, " + "summon a Blind Vermin".Style(StyleEnum.Utility) + " with " + "{1} ".Style(StyleEnum.Utility) + "Common".Style(StyleEnum.White) + " or " + "Uncommon".Style(StyleEnum.Green) + " items.");
			AddToken("ACIDSOAKEDBLINDFOLD_LORE", "What is that smell?");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("1", "Vermin Count");
			AddScalingFunction("[a] * 5 + 5", "Item Count");
			AddScalingFunction("0.2", "Green Item Chance");
			AddScalingFunction("30", "Respawn Delay"); 
		}

		protected override void FillItemDisplayRules()
		{
			base.FillItemDisplayRules();
			AddDisplayRules(VanillaIDRS.Engineer, 
				new ItemDisplayRule
				{
					
				}
			);
			AddDisplayRules(VanillaIDRS.Commando, 
				new ItemDisplayRule
				{
					
				}
			);
			AddDisplayRules(VanillaIDRS.Huntress, 
				new ItemDisplayRule
				{
					
				}
			);
			AddDisplayRules(VanillaIDRS.Bandit, 
				new ItemDisplayRule
				{
					
				}
			);
			AddDisplayRules(VanillaIDRS.Mult, 
				new ItemDisplayRule
				{
					
				}
			);
			AddDisplayRules(VanillaIDRS.Artificer, 
				new ItemDisplayRule
				{
					
				}
			);
			AddDisplayRules(VanillaIDRS.Mercenary, 
				new ItemDisplayRule
				{
					
				}
			);
			AddDisplayRules(VanillaIDRS.Rex,
				new ItemDisplayRule
				{
					
				}
			);
			AddDisplayRules(VanillaIDRS.Loader,
				new ItemDisplayRule
				{
					
				}
			);
			AddDisplayRules(VanillaIDRS.Acrid,
				new ItemDisplayRule
				{
					
				}
			);
			AddDisplayRules(VanillaIDRS.Captain, 
				new ItemDisplayRule
				{
					
				}
			);
			AddDisplayRules(VanillaIDRS.RailGunner, 
				new ItemDisplayRule
				{
					
				}
			);
			AddDisplayRules(VanillaIDRS.VoidFiend, 
				new ItemDisplayRule
				{
					
				}
			);
		}
	}
}