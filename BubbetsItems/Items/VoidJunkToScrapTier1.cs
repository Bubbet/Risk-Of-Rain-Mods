#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using BubbetsItems.Helpers;
using HarmonyLib;
using RoR2;
using RoR2.Items;
using UnityEngine;

namespace BubbetsItems.Items
{
	public class VoidJunkToScrapTier1 : ItemBase
	{
		private static VoidJunkToScrapTier1 instance;
		public override bool RequiresSOTV { get; protected set; } = true;

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("VOIDJUNKTOSCRAPTIER1_NAME", "Void Scrap");
			AddToken("VOIDJUNKTOSCRAPTIER1_PICKUP", $"{"Corrupts all broken items".Style(StyleEnum.Void)} into scrap.");
			AddToken("VOIDJUNKTOSCRAPTIER1_DESC", $"{"Corrupts all broken items".Style(StyleEnum.Void)} and converts them into usable {"White scrap".Style(StyleEnum.White)}.");
			AddToken("VOIDJUNKTOSCRAPTIER1_LORE", "");
			instance = this;
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CostTypeCatalog), nameof(CostTypeCatalog.Init))]
		public static void FixBuying()
		{
			try
			{
				var def = CostTypeCatalog.GetCostTypeDef(CostTypeIndex.WhiteItem);
				var oldCan = def.isAffordable;
				def.isAffordable = (typeDef, context) =>
				{
					try
					{
						return typeDef.itemTier == ItemTier.Tier1
							? oldCan(typeDef, context) || context.cost <= context.activator
								.GetComponent<CharacterBody>()
								.inventory.GetItemCount(instance.ItemDef) - 1
							: oldCan(typeDef, context);
					}
					catch (Exception e)
					{
						BubbetsItemsPlugin.Log.LogError(e);
						return oldCan(typeDef, context);
					}
				};
				var oldCost = def.payCost;
				def.payCost = (typeDef, context) =>
				{
					try
					{
						var inv = context.activatorBody.inventory;
						if (typeDef.itemTier == ItemTier.Tier1 && context.cost == 1 &&
						    (inv != null ? inv.GetItemCount(instance.ItemDef) : 0) > 1)
						{
							inv!.RemoveItem(instance.ItemDef);
							context.results.itemsTaken = new List<ItemIndex> {instance.ItemDef.itemIndex};
							MultiShopCardUtils.OnNonMoneyPurchase(context);
						}
						else
						{
							oldCost(typeDef, context);
						}
					}
					catch (Exception e)
					{
						BubbetsItemsPlugin.Log.LogError(e);
						oldCost(typeDef, context);
					}
				};
			}
			catch (Exception e)
			{
				BubbetsItemsPlugin.Log.LogError(e);
			}
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