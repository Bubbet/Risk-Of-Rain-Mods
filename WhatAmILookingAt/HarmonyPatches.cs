
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BetterAPI;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace WhatAmILookingAt
{
	[HarmonyPatch]
	public class HarmonyPatches
	{
		public static readonly Type UnityPlugin = typeof(BaseUnityPlugin);

		public static readonly Dictionary<string, BepInPlugin> ContentPackToBepinPluginMap = new Dictionary<string, BepInPlugin>();

		public static readonly Dictionary<object, string?> IdentifierMap = new Dictionary<object, string?>();

		public static Dictionary<object, BepInPlugin> R2Map => R2APICompat.R2Map;
		public static Dictionary<object, BepInPlugin> BetterAPIMap => BetterAPICompat.BetterAPIMap;

		[HarmonyPostfix, HarmonyPatch(typeof(TooltipProvider), "get_bodyText")]
		public static void FixToken(TooltipProvider __instance, ref string __result)
		{
			var body = __instance.bodyToken;
			var bodyText = __result;
			//var nameToken = __instance.titleToken;

			BuffIcon icon;
			
			object? ite = null;
			string? identifier = null;
			if (WhatAmILookingAtPlugin.BetterUIEnabled && (icon = __instance.GetComponent<BuffIcon>()) != null)
			{
				var buff = icon.buffDef;
				ite = buff;
				identifier = GetIdentifier(buff);
			} 
			else if (!string.IsNullOrEmpty(body))
			{
				if (ItemCatalog.itemDefs.TryFirst(x => x.descriptionToken == body || x.pickupToken == body,
					out var item))
				{
					ite = item;
					identifier = GetIdentifier(item);
				}

				else if (EquipmentCatalog.equipmentDefs.TryFirst(
					x => x.descriptionToken == body || x.pickupToken == body, out var eq))
				{
					ite = eq;
					identifier = GetIdentifier(eq);
				}

				else if (ArtifactCatalog.artifactDefs.TryFirst(x => x.descriptionToken == body, out var arti))
				{
					ite = arti;
					identifier = GetIdentifier(arti);
				} else if (SkillCatalog.allSkillDefs.TryFirst(x => x.skillDescriptionToken == body, out var skill))
				{
					ite = skill;
					identifier = GetIdentifier(skill);
				}
			}
			else if (!string.IsNullOrEmpty(bodyText))
			{
				if (SkillCatalog.allSkillDefs.TryFirst(x => x.skillDescriptionToken != "" && bodyText.StartsWith(Language.GetString(x.skillDescriptionToken)),
					out var skill))
				{
					ite = skill;
					identifier = GetIdentifier(skill);
				}

				else if (UnlockableCatalog.indexToDefTable.TryFirst(
					x => x.getUnlockedString() == bodyText || x.getHowToUnlockString() == bodyText, out var unlockable))
				{
					ite = unlockable;
					identifier = GetIdentifier(unlockable);
				}
			}

			//Debug.Log(identifier);
			//Debug.Log(ite);
			
			if (identifier == null) return;
			string? str = null;

			switch (identifier)
			{
				case "R2API":
				{
					if (R2APICompat.R2Map.ContainsKey(ite))
						str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_MOD", R2APICompat.R2Map[ite].Name);
					else
						goto default;
					break;
				}
				case "BetterAPI":
				{
					if (BetterAPICompat.BetterAPIMap.ContainsKey(ite))
						str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_MOD", BetterAPICompat.BetterAPIMap[ite].Name); // TODO currently this is just contentpack identifier, i need to get to bepinplugin
					else
						goto default;
					break;
				}
				case "RoR2.BaseContent":
					str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_VANILLA", "DLC 0");
					break;
				default:
				{
					if (ContentPackToBepinPluginMap.ContainsKey(identifier))
					{
						str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_MOD", ContentPackToBepinPluginMap[identifier].Name);
					}
					else
					{
						str = Language.GetString("BUB_WAILA_TOOLTIP_UNKNOWN");
					}

					break;
				}
			}

			if (str != null)
				__result += "\n\n" + str;
		}
		
		private static string? FindIdentifier<T>(T item)
		{
			foreach (var pack in ContentManager.allLoadedContentPacks)//contentPacks.Values)
			{
				foreach (var assetCollection in pack.src.assetCollections)//pack.assetCollections)
				{
					var typ = assetCollection.GetType().GenericTypeArguments[0];
					if (!typ.IsInstanceOfType(item)) continue;
					if (assetCollection is NamedAssetCollection<T> coll && coll.Contains(item))
						return pack.identifier;
				}
			}

			WhatAmILookingAtPlugin.Log.LogWarning("Failed to find pack for " + item);
			return null;
		}

		private static string? GetIdentifier<T>(T item)
		{
			if (!IdentifierMap.ContainsKey(item!))
				IdentifierMap.Add(item, FindIdentifier(item));
			return IdentifierMap[item];
		}
		
		/*
		private static BepInPlugin? GetPlugin<T>(T item)
		{
			if (item == null) return null;
			var identifier = GetIdentifier(item!);
			switch (identifier)
			{
				case null:
					return null;
				case "R2API" when r2Map.ContainsKey(item):
					return r2Map[item];
				default:
					return contentPackToBepinPluginMap.ContainsKey(identifier) ? contentPackToBepinPluginMap[identifier] : null;
			}
		}*/


		[HarmonyPostfix, HarmonyPatch(typeof(Language), nameof(Language.Init))]
		public static void FuckYouSystemInitializersNotWorking()
		{
			WhatAmILookingAtPlugin.ExtraTokens();
		}

		[HarmonyPostfix, HarmonyPatch(typeof(ContentManager.ContentPackLoader), MethodType.Constructor, typeof(List<IContentPackProvider>))]
		public static void FetchBepinPlugins(List<IContentPackProvider> contentPackProviders)
		{
			foreach (var provider in contentPackProviders)
			{
				if (GetPluginFromAssembly(provider.GetType().Assembly, out var plugin))
				{
					if (!ContentPackToBepinPluginMap.ContainsKey(provider.identifier))
						ContentPackToBepinPluginMap.Add(provider.identifier, plugin);
					else
						WhatAmILookingAtPlugin.Log.LogWarning("Key already exists for " + provider.identifier);
				}
			}
		}

		public static bool GetPluginFromAssembly(Assembly? assembly, out BepInPlugin? plugin)
		{
			try
			{
				var types = assembly?.GetTypes(); // Had to way overengineer this because there is some people that think they need to derive baseunityplugin for a lot of their classes
				var unity = types?.FirstOrDefault(x => UnityPlugin.IsAssignableFrom(x) && x.GetCustomAttributes(false).FirstOrDefault(x2 => x2 is BepInPlugin) != null);
				plugin = unity?.GetCustomAttributes(false).FirstOrDefault(x => x is BepInPlugin) as BepInPlugin;
				return plugin != null;
			}
			catch (ReflectionTypeLoadException e)
			{
				WhatAmILookingAtPlugin.Log.LogInfo(assembly);
				WhatAmILookingAtPlugin.Log.LogError(e);
				plugin = null;
				return false;
			}
		}
	}

	[HarmonyPatch]
	static class R2APICompat
	{
		public static readonly Dictionary<object, BepInPlugin> R2Map = new Dictionary<object, BepInPlugin>();

		[HarmonyPrefix, HarmonyPatch(typeof(ItemAPI), nameof(ItemAPI.Add), typeof(CustomItem))]
		public static void ItemPost(CustomItem? item)
		{
			if (item == null) return;
			var assembly = new StackTrace().GetFrame(3).GetMethod().DeclaringType?.Assembly;
			if (!HarmonyPatches.GetPluginFromAssembly(assembly, out var plugin)) return;
			if (item.ItemDef != null)
				R2Map.Add(item.ItemDef, plugin!);
		}
		
		[HarmonyPrefix, HarmonyPatch(typeof(ItemAPI), nameof(ItemAPI.Add), typeof(CustomEquipment))]
		public static void ItemPost(CustomEquipment? item)
		{
			if (item == null) return;
			var assembly = new StackTrace().GetFrame(3).GetMethod().DeclaringType?.Assembly;
			if (!HarmonyPatches.GetPluginFromAssembly(assembly, out var plugin)) return;
			if (item.EquipmentDef != null)
				R2Map.Add(item.EquipmentDef, plugin!);
		}

		[HarmonyPrefix, HarmonyPatch(typeof(LoadoutAPI), nameof(LoadoutAPI.AddSkillDef))]
		public static void SkillPost(SkillDef? s)
		{
			// TODO might need to do AddSkill too
			if (s == null) return;
			var assembly = new StackTrace().GetFrame(2).GetMethod().DeclaringType?.Assembly;
			if (!HarmonyPatches.GetPluginFromAssembly(assembly, out var plugin)) return;
			R2Map.Add(s, plugin!);
		}
		
		[HarmonyPostfix, HarmonyPatch(typeof(UnlockableAPI), nameof(UnlockableAPI.AddUnlockable), typeof(Type), typeof(Type), typeof(UnlockableDef))]
		public static void UnlockablePost(Type unlockableType, Type serverTrackerType, UnlockableDef unlockableDef, UnlockableDef __result)
		{
			var assembly = new StackTrace().GetFrame(4).GetMethod().DeclaringType?.Assembly;
			if (!HarmonyPatches.GetPluginFromAssembly(assembly, out var plugin)) return;
			R2Map.Add(__result, plugin!);
		}
		
		[HarmonyPrefix, HarmonyPatch(typeof(ArtifactAPI), nameof(ArtifactAPI.Add), typeof(ArtifactDef))]
		public static void ArtifactPost(ArtifactDef? artifactDef)
		{
			if (artifactDef == null) return;
			var assembly = new StackTrace().GetFrame(2).GetMethod().DeclaringType?.Assembly;
			if (!HarmonyPatches.GetPluginFromAssembly(assembly, out var plugin)) return;
			R2Map.Add(artifactDef, plugin!);
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(ArtifactAPI), nameof(ArtifactAPI.Add), typeof(string), typeof(string), typeof(string), typeof(GameObject), typeof(Sprite), typeof(Sprite), typeof(UnlockableDef))]
		public static void ArtifactPost(ILContext il)
		{
			var c = new ILCursor(il);
			var what = -1;
			c.GotoNext( MoveType.After,
				x => x.OpCode == OpCodes.Ldsfld,
				x => x.MatchLdloc(out what)//,
				//x => x.OpCode == OpCodes.Callvirt
			);
			c.Emit(OpCodes.Ldloc, what);
			c.EmitDelegate<Action<ArtifactDef>>(ArtifactPost);
		}

		[HarmonyPrefix, HarmonyPatch(typeof(BuffAPI), nameof(BuffAPI.Add))]
		public static void BuffPost(CustomBuff? buff)
		{
			if (buff == null) return;
			var assembly = new StackTrace().GetFrame(2).GetMethod().DeclaringType?.Assembly;
			if (!HarmonyPatches.GetPluginFromAssembly(assembly, out var plugin)) return;
			R2Map.Add(buff.BuffDef!, plugin!);
		}
	}

	[HarmonyPatch]
	static class BetterAPICompat
	{
		public static readonly Dictionary<object, BepInPlugin> BetterAPIMap = new Dictionary<object, BepInPlugin>();
		
		//ItemDef Add(ItemDef itemDef, Items.CharacterItemDisplayRule[] characterItemDisplayRules = null, string contentPackIdentifier = null)
		//[HarmonyILManipulator, HarmonyPatch(typeof(Items), nameof(Items.Add), typeof(ItemDef), typeof(Items.CharacterItemDisplayRule[]), typeof(string))]
		public static void ItemPost(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext( MoveType.After,
				x => x.MatchCall<Assembly>(nameof(Assembly.GetCallingAssembly))
				//x => x.OpCode == OpCodes.Callvirt //x.MatchCallvirt<AssemblyName>(nameof(AssemblyName.Name)), // This might cause key not found
			);
			c.Emit(OpCodes.Dup);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Action<Assembly, ItemDef>>((a, def) =>
			{
				if (HarmonyPatches.GetPluginFromAssembly(a, out var plugin))
					BetterAPIMap.Add(def, plugin!);
				else
					WhatAmILookingAtPlugin.Log.LogWarning("BetterAPI failed to find plugin for item: " + def);
			});
		}

		public static readonly Assembly BetterAPIAssembly = typeof(BetterAPIPlugin).Assembly;
		public static readonly Assembly WailaAssembly = typeof(WhatAmILookingAtPlugin).Assembly;
		
		[HarmonyPostfix, HarmonyPatch(typeof(Items), nameof(Items.Add), typeof(ItemDef), typeof(Items.CharacterItemDisplayRule[]), typeof(string))]
		public static void ItemPost(ItemDef itemDef, string? contentPackIdentifier)
		{
			var trace = new StackTrace();
			var i = 0;
			var assembly = trace.GetFrame(i).GetMethod().DeclaringType.Assembly;
			while (assembly == BetterAPIAssembly || assembly == WailaAssembly)
			{
				i++;
				assembly = trace.GetFrame(i).GetMethod().DeclaringType.Assembly;
			}

			if (HarmonyPatches.GetPluginFromAssembly(assembly, out var plugin))
			{
				var key = contentPackIdentifier ?? assembly.GetName().Name;
				if (!HarmonyPatches.ContentPackToBepinPluginMap.ContainsKey(key))
					HarmonyPatches.ContentPackToBepinPluginMap.Add(key, plugin!);
				//BetterAPIMap.Add(itemDef, plugin!);
			}
			else
				WhatAmILookingAtPlugin.Log.LogWarning("BetterAPI failed to find plugin for item: " + itemDef);
		}
	}
}