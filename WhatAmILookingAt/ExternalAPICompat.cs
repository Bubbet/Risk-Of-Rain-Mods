#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BetterAPI;
using R2API.ContentManagement;

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
				IdentifierToR2AssemblyMap.Add(identifier, R2APIContentManager.ManagedContentPacks.FirstOrDefault(x => x.Identifier == identifier).TiedAssembly);
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
	}
}