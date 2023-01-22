using System;
using HarmonyLib;
using RoR2;

namespace BubbetsItems.Components
{
	[HarmonyPatch]
	public static class CommonBodyPatches
	{
		public static event Action<ExtraStats>? CollectExtraStats;
		
		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void FixBarrier(CharacterBody __instance)
		{
			var inv = __instance.inventory;
			if (!inv) return;
			var stats = new ExtraStats()
			{
				body = __instance,
				inventory = inv
			};
			CollectExtraStats?.Invoke(stats);
			stats.body.barrierDecayRate *= 1f - stats.barrierDecay;
		}

		public class ExtraStats
		{
			public CharacterBody body;
			public Inventory inventory;
			public float barrierDecay;
		}
	}
}