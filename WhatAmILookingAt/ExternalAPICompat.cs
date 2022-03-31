#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx;
using BetterAPI;
using HarmonyLib;
using R2API.ContentManagement;
using RoR2;
using TILER2;

namespace WhatAmILookingAt
{
	static class ExternalAPICompat
	{
		public static bool GetPluginFromBetterAPI(string identifier, out BepInPlugin? o)
		{
			var assembly = ContentPacks.FindAssembly(identifier);
			if (WhatAmILookingAtPlugin.GetPluginFromAssembly(assembly, out var plugin))
			{
				o = plugin;
				return true;
			}

			o = null;
			return false;
		}

		public static readonly Dictionary<string, Assembly> IdentifierToR2AssemblyMap = new Dictionary<string, Assembly>();
		public static bool GetPluginFromR2API(string identifier, out BepInPlugin? o)
		{
			if (!IdentifierToR2AssemblyMap.ContainsKey(identifier))
				IdentifierToR2AssemblyMap.Add(identifier, 
					R2APIContentManager.ManagedContentPacks.FirstOrDefault(x => x.Identifier == identifier).TiedAssembly);
			var assembly = IdentifierToR2AssemblyMap[identifier];
			//var assembly = R2APIContentManager.GetAssemblyFromContentPack(pack);
			if (WhatAmILookingAtPlugin.GetPluginFromAssembly(assembly, out var plugin))
			{
				o = plugin;
				return true;
			}

			o = null;
			return false;
		}
		
		
		public static readonly Dictionary<object, Assembly> Tiler2Map = new Dictionary<object, Assembly>();
		
		public static void Tiler2AddItemToList(Item __instance)
		{
			var frame = new StackFrame(2);
			var assembly = frame.GetMethod().DeclaringType.Assembly;
			var itemDef = __instance.customItem.ItemDef;
			if (!Tiler2Map.ContainsKey(itemDef))
				Tiler2Map.Add(itemDef, assembly);
		}
		
		public static void Tiler2AddEquipmentToList(Equipment __instance)
		{
			var frame = new StackFrame(2);
			var assembly = frame.GetMethod().DeclaringType.Assembly;
			var equipmentDef = __instance.customEquipment.EquipmentDef;
			if (!Tiler2Map.ContainsKey(equipmentDef))
				Tiler2Map.Add(equipmentDef, assembly);
		}

		public static bool GetPluginFromTiler2(string s, out BepInPlugin? o)
		{
			if (ItemCatalog.itemDefs.TryFirst(x => x.descriptionToken == s || x.pickupToken == s, out var itemDef))
			{
				if (WhatAmILookingAtPlugin.GetPluginFromAssembly(Tiler2Map[itemDef], out var plugin))
				{
					o = plugin;
					return true;
				}
			}
			o = default;
			return false;
		}

		public static void PatchTiler2(Harmony harm)
		{
			var method = typeof(ExternalAPICompat).GetMethod(nameof(Tiler2AddItemToList));
			var patch = typeof(Item).GetMethod(nameof(Item.SetupAttributes));
			harm.Patch(patch, null, new HarmonyMethod(method));
			
			method = typeof(ExternalAPICompat).GetMethod(nameof(Tiler2AddEquipmentToList));
			patch = typeof(Equipment).GetMethod(nameof(Equipment.SetupAttributes));
			harm.Patch(patch, null, new HarmonyMethod(method));
		}
	}
}