using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Components
{
	[HarmonyPatch]
	public static class CommonBodyPatches
	{
		public delegate void ExtraStatsDelegate(ref ExtraStats obj);
		public static event ExtraStatsDelegate? CollectExtraStats;
		
		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void FixBarrier(CharacterBody __instance)
		{
			if (CollectExtraStats is null) return;
			var inv = __instance.inventory;
			if (!inv) return;
			var stats = new ExtraStats
			{
				body = __instance,
				inventory = inv
			};
			var list = new List<KeyValuePair<ExtraStatsDelegate, ExtraStats>>();
			var stat = stats;
			foreach (var @delegate in CollectExtraStats.GetInvocationList())
			{
				var dele = (ExtraStatsDelegate)@delegate;
				dele.Invoke(ref stat);
				list.Add(new KeyValuePair<ExtraStatsDelegate, ExtraStats>(dele, stat));
			}
			foreach (var pair in list.OrderBy(x => x.Value.priority))
			{
				var dele = pair.Key;
				dele.Invoke(ref stats);
			}

			//CollectExtraStats?.Invoke(stats);
			if (stats.barrierDecayMult >= 0)
			{
				stats.body.barrierDecayRate *= Mathf.Max(1f - stats.barrierDecayMult, 0);
				stats.body.barrierDecayRate += stats.barrierDecayAdd;
			}
			else
			{
				stats.body.barrierDecayRate *= stats.barrierDecayMult;
				stats.body.barrierDecayRate += stats.barrierDecayAdd;
			}
		}

		public struct ExtraStats
		{
			public CharacterBody body;
			public Inventory inventory;
			public float barrierDecayMult;
			public float barrierDecayAdd;
			public int priority;
			
			public static ExtraStats operator +(ExtraStats stats1, ExtraStats stats2)
			{
				stats1.barrierDecayMult += stats2.barrierDecayMult;
				stats1.barrierDecayAdd += stats2.barrierDecayAdd;
				return stats1;
			}
		}
	}
}