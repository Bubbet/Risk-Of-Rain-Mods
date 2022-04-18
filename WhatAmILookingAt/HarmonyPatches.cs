#nullable enable
using System;
using System.Collections.Generic;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.ContentManagement;
using RoR2.UI;
using RoR2.UI.LogBook;

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
				
				/*
				if (WhatAmILookingAtPlugin.TILER2Enabled) // Yes, i hate it too.
				{
					if (ExternalAPICompat.GetPluginFromTiler2(__instance.bodyToken, out var plu))
						str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_MOD", WhatAmILookingAtPlugin.TextColor!.Value, plu!.Name);
				}*/

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
		
		[HarmonyILManipulator, HarmonyPatch(typeof(PageBuilder), nameof(PageBuilder.AddSimplePickup))]
		public static void PagebuilderPatch(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext( MoveType.After,
				x => x.MatchLdfld<ItemDef>("descriptionToken")
			);
			c.Index-=1;
			c.Emit(OpCodes.Dup);
			c.Index += 2;
			c.EmitDelegate<Func<ItemDef, string, string>>((def, str) => str  + "\n" + WhatAmILookingAtPlugin.GetModString(WhatAmILookingAtPlugin.GetIdentifier(def) ?? "Unknown"));
            
			c.GotoNext( MoveType.After,
				x => x.MatchLdfld<EquipmentDef>("descriptionToken")
			);
			c.Index-=1;
			c.Emit(OpCodes.Dup);
			c.Index += 2;
			c.EmitDelegate<Func<EquipmentDef, string, string>>((def, str) => str  + "\n" + WhatAmILookingAtPlugin.GetModString(WhatAmILookingAtPlugin.GetIdentifier(def) ?? "Unknown"));
		}
	}
}