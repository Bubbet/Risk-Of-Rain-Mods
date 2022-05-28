using System.Linq;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace BubbetsItems
{
	[HarmonyPatch]
	public static class ColorCatalogPatches
	{
		public static Color32 VoidLunarColor = new(134, 0, 203, 255);

		
		public static void AddNewColors()
		{
			var len = ColorCatalog.indexToColor32.Length;
			BubbetsItemsPlugin.VoidLunarTier.colorIndex = (ColorCatalog.ColorIndex) len;
			BubbetsItemsPlugin.VoidLunarTier.darkColorIndex = (ColorCatalog.ColorIndex) len + 1;
			
			var voidLunarDark = new Color32(83, 0, 126, 255);
            
			ColorCatalog.indexToColor32 = ColorCatalog.indexToColor32.AddItem(VoidLunarColor).AddItem(voidLunarDark).ToArray();
			ColorCatalog.indexToHexString = ColorCatalog.indexToHexString.AddItem(Util.RGBToHex(VoidLunarColor)).AddItem(Util.RGBToHex(voidLunarDark)).ToArray();
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