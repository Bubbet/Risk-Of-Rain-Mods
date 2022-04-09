#nullable enable
using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
using RoR2;
using RoR2.Items;

namespace BubbetsItems.Items
{
	public class VoidJunkToScrapTier1 : ItemBase
	{
		private static VoidJunkToScrapTier1? _instance;
		private static ConfigEntry<bool>? _canConsumeLastStack;
		private static CostTypeDef.IsAffordableDelegate? _oldCan;
		private static CostTypeDef.PayCostDelegate? _oldCost;
		public override bool RequiresSotv => true;

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			_canConsumeLastStack = configFile!.Bind(ConfigCategoriesEnum.General, "Void Scrap Consume Last Stack", false, "Should the void scrap consume the last stack when being used for scrap.");
		}

		public override string GetFormattedDescription(Inventory inventory, string? token = null)
		{
			return Language.GetStringFormatted(ItemDef.descriptionToken, !_canConsumeLastStack!.Value ? "Cannot consume the last stack. " : "");
		}

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("VOIDJUNKTOSCRAPTIER1_NAME", "Void Scrap");
			AddToken("VOIDJUNKTOSCRAPTIER1_PICKUP", "Prioritized when used with " + "Common ".Style(StyleEnum.White) + "3D Printers. " + "Corrupts all Broken items".Style(StyleEnum.Void) + ".");
			AddToken("VOIDJUNKTOSCRAPTIER1_DESC", "Does nothing. " + "Prioritized when used with " + "Common ".Style(StyleEnum.White) + "3D Printers. {0}" + "Corrupts all Broken items".Style(StyleEnum.Void) + ".");
			AddToken("VOIDJUNKTOSCRAPTIER1_LORE", "");
		}

		public VoidJunkToScrapTier1()
		{
			_instance = this;
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CostTypeCatalog), nameof(CostTypeCatalog.Init))]
		public static void FixBuying()
		{
			try
			{
				var def = CostTypeCatalog.GetCostTypeDef(CostTypeIndex.WhiteItem);
				_oldCan = def.isAffordable;
				def.isAffordable = IsAffordable;
				_oldCost = def.payCost;
				def.payCost = PayCost;
			}
			catch (Exception e)
			{
				BubbetsItemsPlugin.Log.LogError(e);
			}
		}

		private static void PayCost(CostTypeDef typeDef, CostTypeDef.PayCostContext context)
		{
			if (typeDef.itemTier != ItemTier.Tier1)
			{
				_oldCost!(typeDef, context);
				return;
			}

			try
			{
				var inv = context.activatorBody.inventory;

				var highestPriority = new WeightedSelection<ItemIndex>();
				var higherPriority = new WeightedSelection<ItemIndex>();
				var highPriority = new WeightedSelection<ItemIndex>();
				var normalPriority = new WeightedSelection<ItemIndex>();

				var voidAmount = Math.Max(0, inv.GetItemCount(_instance!.ItemDef) - 1);
				if (_canConsumeLastStack!.Value || voidAmount > 0) highestPriority.AddChoice(_instance.ItemDef.itemIndex, voidAmount);

				foreach (var itemIndex in ItemCatalog.tier1ItemList)
				{
					if (itemIndex == context.avoidedItemIndex) continue;
					var count = inv.GetItemCount(itemIndex);
					if (count > 0)
					{
						var itemDef = ItemCatalog.GetItemDef(itemIndex);
						(itemDef.ContainsTag(ItemTag.PriorityScrap) ? higherPriority : itemDef.ContainsTag(ItemTag.Scrap) ? highPriority : normalPriority).AddChoice(itemIndex, count);
					}
				}

				var itemsToTake = new List<ItemIndex>();

				TakeFromWeightedSelection(highestPriority, ref context, ref itemsToTake);
				TakeFromWeightedSelection(higherPriority, ref context, ref itemsToTake);
				TakeFromWeightedSelection(highPriority, ref context, ref itemsToTake);
				TakeFromWeightedSelection(normalPriority, ref context, ref itemsToTake);

				for (var i = itemsToTake.Count; i < context.cost; i++) itemsToTake.Add(context.avoidedItemIndex);

				context.results.itemsTaken = itemsToTake;
				foreach (var itemIndex in itemsToTake) inv.RemoveItem(itemIndex);
				MultiShopCardUtils.OnNonMoneyPurchase(context);
			}
			catch (Exception e)
			{
				BubbetsItemsPlugin.Log.LogError(e);
			}
		}

		private static bool IsAffordable(CostTypeDef typeDef, CostTypeDef.IsAffordableContext context)
		{
			if (_oldCan!(typeDef, context)) return true;
			try
			{
				if (typeDef.itemTier != ItemTier.Tier1) return false;
				var inv = context.activator.GetComponent<CharacterBody>().inventory;
				var voidAmount = Math.Max(0, inv.GetItemCount(_instance!.ItemDef) - (_canConsumeLastStack!.Value ? 0 : 1));
				return inv.GetTotalItemCountOfTier(ItemTier.Tier1) + voidAmount >= context.cost;
			}
			catch (Exception e)
			{
				BubbetsItemsPlugin.Log.LogError(e);
				return false;
			}
		}

		private static void TakeFromWeightedSelection(WeightedSelection<ItemIndex> weightedSelection, ref CostTypeDef.PayCostContext context, ref List<ItemIndex> itemsToTake)
		{
			while (weightedSelection.Count > 0 && itemsToTake.Count < context.cost)
			{
				var choiceIndex = weightedSelection.EvaluateToChoiceIndex(context.rng.nextNormalizedFloat);
				var choice = weightedSelection.GetChoice(choiceIndex);
				var value = choice.value;
				var num = (int)choice.weight;
				num--;
				if (num <= 0)
				{
					weightedSelection.RemoveChoice(choiceIndex);
				}
				else
				{
					weightedSelection.ModifyChoiceWeight(choiceIndex, num);
				}
				itemsToTake.Add(value);
			}
		}


		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			AddVoidPairing("FragileDamageBonusConsumed HealingPotionConsumed ExtraLifeVoidConsumed ExtraLifeConsumed");
		}
	}
}