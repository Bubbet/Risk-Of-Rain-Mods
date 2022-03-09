#nullable enable
using System;
using System.Collections.Generic;
using HarmonyLib;
using RoR2.ContentManagement;
using RoR2.UI;

namespace WhatAmILookingAt
{
	[HarmonyPatch]
	public class HarmonyPatches
	{
		[HarmonyPostfix, HarmonyPatch(typeof(TooltipProvider), "get_bodyText")]
		public static void FixToken(TooltipProvider __instance, ref string __result)
		{
			try
			{
				var identifier = WhatAmILookingAtPlugin.FindItem(__instance, __result); // WhatAmILookingAtPlugin.GetIdentifier();
				if (identifier == null) return;
				string? str = WhatAmILookingAtPlugin.GetModString(identifier);

				if (str != null)
					__result += "\n\n" + str;
			}
			catch (Exception e)
			{
				WhatAmILookingAtPlugin.Log!.LogError(e);
			}
		}

		[HarmonyPostfix, HarmonyPatch(typeof(ContentManager.ContentPackLoader), MethodType.Constructor, typeof(List<IContentPackProvider>))]
		public static void FetchBepinPlugins(List<IContentPackProvider> contentPackProviders)
		{
			foreach (var provider in contentPackProviders)
			{
				if (WhatAmILookingAtPlugin.GetPluginFromAssembly(provider.GetType().Assembly, out var plugin))
				{
					if (!WhatAmILookingAtPlugin.ContentPackToBepinPluginMap.ContainsKey(provider.identifier))
						WhatAmILookingAtPlugin.ContentPackToBepinPluginMap.Add(provider.identifier, plugin!);
					else
						WhatAmILookingAtPlugin.Log!.LogWarning("Key already exists for " + provider.identifier);
				}
			}
		}
	}
}