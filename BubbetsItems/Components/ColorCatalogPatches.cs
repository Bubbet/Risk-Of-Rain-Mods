using System;
using System.Linq;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace BubbetsItems
{
	[HarmonyPatch]
	public static class ColorCatalogPatches
	{
		
		public static (Action<ColorCatalog.ColorIndex>, Color32)[] colors =
		{
			(index => BubbetsItemsPlugin.VoidLunarTier.colorIndex = index, new Color32(134, 0, 203, 255)),
			(index => BubbetsItemsPlugin.VoidLunarTier.darkColorIndex = index, new Color32(83, 0, 126, 255)),
			(index =>
			{
				foreach (var eq in EquipmentBase.Equipments.Where(x => x.EquipmentDef is BubVoidEquipmentDef))
					eq.EquipmentDef.colorIndex = index;
			}, new Color32(100, 255, 100, 255)), // TODO replace this with an actual color
		};
		
		public static Color32 VoidLunarColor = colors[0].Item2;

		public static void AddNewColors()
		{
			var len = ColorCatalog.indexToColor32.Length;

			foreach (var valueTuple in colors)
			{
				valueTuple.Item1((ColorCatalog.ColorIndex) len);
				len++;
			}

			ColorCatalog.indexToColor32 = ColorCatalog.indexToColor32.AddRangeToArray(colors.Select(x => x.Item2).ToArray());
			ColorCatalog.indexToHexString = ColorCatalog.indexToHexString.AddRangeToArray(colors.Select(x => Util.RGBToHex(x.Item2)).ToArray());
		}

		[HarmonyPrefix, HarmonyPatch(typeof(ColorCatalog), nameof(ColorCatalog.GetColor))]
		public static bool PatchGetColor(ColorCatalog.ColorIndex colorIndex, ref Color32 __result)
		{
			var ind = (int) colorIndex;
			if (ind >= ColorCatalog.indexToColor32.Length) return true;
			__result = ColorCatalog.indexToColor32[ind];
			return false;
		}
		
		[HarmonyPrefix, HarmonyPatch(typeof(ColorCatalog), nameof(ColorCatalog.GetColorHexString))]
		public static bool GetColorHexString(ColorCatalog.ColorIndex colorIndex, ref string __result)
		{
			var ind = (int) colorIndex;
			if (ind >= ColorCatalog.indexToHexString.Length) return true;
			__result = ColorCatalog.indexToHexString[ind];
			return false;
		}
	}
}