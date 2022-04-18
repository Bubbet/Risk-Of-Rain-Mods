using System.Collections.Generic;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace BubbetsItems
{
	[HarmonyPatch]
	public static class ModdedDamageColors
	{
		private static Dictionary<DamageColorIndex, Color> damageColors = new()
		{
			{(DamageColorIndex) 145, new Color(1, 0.4f, 0)}
		};
		
		[HarmonyPrefix, HarmonyPatch(typeof(DamageColor), nameof(DamageColor.FindColor))]
		public static bool PatchColor(DamageColorIndex colorIndex, ref Color __result)
		{
			if (!damageColors.ContainsKey(colorIndex)) return true;
			__result = damageColors[colorIndex];
			return false;
		}
	}
}