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
					childName = "HeadCenter",
					localPos = new Vector3(0.00003F, -0.01356F, -0.00489F),
					localAngles = new Vector3(271.3241F, 163.1169F, 196.504F),
					localScale = new Vector3(20.3724F, 20.3724F, 20.3724F)

				}
			);
			AddDisplayRules(VanillaIDRS.Commando, 
				new ItemDisplayRule
				{
					childName = "HeadCenter",
					localPos = new Vector3(0.0002F, -0.04676F, 0.0113F),
					localAngles = new Vector3(287.2982F, 181.1993F, 178.6921F),
					localScale = new Vector3(19.99757F, 19.99757F, 19.99757F)

				}
			);
			AddDisplayRules(VanillaIDRS.Huntress, new []{
					new ItemDisplayRule
					{
						childName = "Head",
						localPos = new Vector3(0.00019F, 0.20214F, -0.03606F),
						localAngles = new Vector3(287.774F, 199.0562F, 149.1596F),
						localScale = new Vector3(15.20139F, 18.35448F, 15.20139F)
					}, new ItemDisplayRule
					{
						childName = "Head",
						localPos = new Vector3(-0.00071F, 0.10571F, -0.02957F),
						localAngles = new Vector3(280.0173F, 181.7912F, 178.092F),
						localScale = new Vector3(15.24168F, 21.07032F, 24.06534F)
					}
				}
			);
			AddDisplayRules(VanillaIDRS.Bandit, 
				new ItemDisplayRule
				{
					childName = "Head",
					localPos = new Vector3(0.00447F, -0.0211F, 0.03583F),
					localAngles = new Vector3(277.9982F, 351.0759F, 354.0609F),
					localScale = new Vector3(12.09946F, 15.5366F, 16.23927F)

				}
			);
			AddDisplayRules(VanillaIDRS.Mult, 
				new ItemDisplayRule
				{
					childName = "HeadCenter",
					localPos = new Vector3(0.09484F, 0.8559F, 1.11345F),
					localAngles = new Vector3(343.7497F, 359.7337F, 180.0988F),
					localScale = new Vector3(115.3599F, 151.6162F, 83.94396F)

				}
			);
			AddDisplayRules(VanillaIDRS.Artificer, 
				new ItemDisplayRule
				{
					childName = "HeadCenter",
					localPos = new Vector3(-0.00001F, 0.00038F, -0.04013F),
					localAngles = new Vector3(320.1061F, 359.752F, 0.07683F),
					localScale = new Vector3(11.08636F, 19.87087F, 13.89857F)

				}
			);
			AddDisplayRules(VanillaIDRS.Mercenary, 
				new ItemDisplayRule
				{
					childName = "Head",
					localPos = new Vector3(-0.00013F, 0.03063F, 0.00036F),
					localAngles = new Vector3(277.698F, 358.2014F, 1.56591F),
					localScale = new Vector3(15.9417F, 21.57999F, 20.53232F)

				}
			);
			AddDisplayRules(VanillaIDRS.Rex,
				new ItemDisplayRule
				{
					childName = "PlatformBase",
					localPos = new Vector3(0F, 0.71469F, -0.09846F),
					localAngles = new Vector3(271.2605F, 0F, 0F),
					localScale = new Vector3(128.5089F, 118.9175F, 126.109F)

				}
			);
			AddDisplayRules(VanillaIDRS.Loader,
				new ItemDisplayRule
				{
					childName = "Head",
					localPos = new Vector3(0F, -0.00059F, 0.00047F),
					localAngles = new Vector3(278.6809F, 2.11529F, 357.5374F),
					localScale = new Vector3(15.83965F, 18.71547F, 18.71547F)

				}
			);
			AddDisplayRules(VanillaIDRS.Acrid,
				new ItemDisplayRule
				{
					childName = "Head",
					localPos = new Vector3(0.00921F, 1.67026F, 0.33465F),
					localAngles = new Vector3(336.4973F, 178.6391F, 180.3327F),
					localScale = new Vector3(197.9459F, 239.8344F, 224.4934F)

				}
			);
			AddDisplayRules(VanillaIDRS.Captain, 
				new ItemDisplayRule
				{
					childName = "Head",
					localPos = new Vector3(0F, 0F, 0F),
					localAngles = new Vector3(288.8836F, 2.75125F, 349.6624F),
					localScale = new Vector3(14.29886F, 20.996F, 25.28969F)

				}
			);
			AddDisplayRules(VanillaIDRS.RailGunner, 
				new ItemDisplayRule
				{
					childName = "Head",
					localPos = new Vector3(0F, 0F, 0F),
					localAngles = new Vector3(276.9453F, 185.3213F, 175.2406F),
					localScale = new Vector3(11.75714F, 14.98476F, 24.932F)

				}
			);
			AddDisplayRules(VanillaIDRS.VoidFiend, 
				new ItemDisplayRule
				{
					childName = "ForeArmR",
					localPos = new Vector3(0.10818F, -0.00104F, 0.00097F),
					localAngles = new Vector3(281.6224F, 299.5807F, 298.5557F),
					localScale = new Vector3(28.50867F, 21.97042F, 26.81637F)

				}
			);
		}
	}
}