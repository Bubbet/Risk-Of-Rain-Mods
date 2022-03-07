using System.Linq;
using BubbetsItems.Helpers;
using HarmonyLib;
using RoR2;

namespace BubbetsItems.Items
{
	public class VoidJunkToScrapTier1 : ItemBase
	{
		public override bool RequiresSOTV { get; protected set; } = true;

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("VOIDJUNKTOSCRAPTIER1_NAME", "Void Scrap");
			AddToken("VOIDJUNKTOSCRAPTIER1_PICKUP", $"{"Corrupts all broken items".Style(StyleEnum.Void)} into scrap.");
			AddToken("VOIDJUNKTOSCRAPTIER1_DESC", $"{"Corrupts all broken items".Style(StyleEnum.Void)} and converts them into usable {"White scrap".Style(StyleEnum.White)}.");
			AddToken("VOIDJUNKTOSCRAPTIER1_LORE", "");
		}

		protected override void FillVoidConversions()
		{
			ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddRangeToArray(new []{new ItemDef.Pair
				{
					itemDef1 = DLC1Content.Items.FragileDamageBonusConsumed, 
					itemDef2 = ItemDef
				},
				new ItemDef.Pair
				{
					itemDef1 = DLC1Content.Items.HealingPotionConsumed,
					itemDef2 = ItemDef
				},
				new ItemDef.Pair
				{
					itemDef1 = DLC1Content.Items.ExtraLifeVoidConsumed,
					itemDef2 = ItemDef
				},
				new ItemDef.Pair
				{
					itemDef1 = RoR2Content.Items.ExtraLifeConsumed,
					itemDef2 = ItemDef
				}
			});
		}
	}
}