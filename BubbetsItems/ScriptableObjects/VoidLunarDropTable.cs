using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace BubbetsItems
{
	[CreateAssetMenu(menuName = "BubbetsItems/VoidLunarDropTable")]
	public class VoidLunarDropTable : PickupDropTable
	{
		private WeightedSelection<PickupIndex> selector = new();
		private void Add(PickupIndex[] sourceDropList, float chance)
		{
			var pickupIndices = sourceDropList;
			if (chance <= 0f || !pickupIndices.Any())
			{
				return;
			}
			foreach (PickupIndex pickupIndex in pickupIndices)
			{
				selector.AddChoice(pickupIndex, chance);
			}
		}
		
		public override void Regenerate(Run run)
		{
			base.Regenerate(run);
			selector.Clear();;
			//Add(ItemBase.Items.Where(x => x.ItemDef.tier == BubbetsItemsPlugin.VoidLunarTier.tier).Select(x => x.PickupIndex), 1);
			Add(BubbetsItemsPlugin.VoidLunarItems, 1);
		}

		public override int GetPickupCount()
		{
			return selector.Count;
		}

		public override PickupIndex GenerateDropPreReplacement(Xoroshiro128Plus rng)
		{
			return GenerateDropFromWeightedSelection(rng, selector);
		}
		
		public override PickupIndex[] GenerateUniqueDropsPreReplacement(int maxDrops, Xoroshiro128Plus rng)
		{
			return GenerateUniqueDropsFromWeightedSelection(maxDrops, rng, selector);
		}
	}
}