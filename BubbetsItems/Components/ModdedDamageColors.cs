﻿using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace BubbetsItems
{
	[HarmonyPatch]
	public static class ModdedDamageColors
	{
		[HarmonyPrefix, HarmonyPatch(typeof(DamageColor), nameof(DamageColor.FindColor))]
		public static bool PatchColor(DamageColorIndex colorIndex, ref Color __result)
		{
			if (colorIndex >= (DamageColorIndex) DamageColor.colors.Length) return true;
			__result = DamageColor.colors[(int) colorIndex];
			return false;
		}

		public static bool ReserveColor(Color color, out DamageColorIndex index)
		{
			index = (DamageColorIndex) DamageColor.colors.Length;
			DamageColor.colors = DamageColor.colors.AddItem(color).ToArray();
		}
	}
}