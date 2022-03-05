
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx;
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
				/*
				case "BetterAPI":
				{
					if (ContentPacks.FindAssembly()) //(BetterAPICompat.BetterAPIMap.ContainsKey(ite))
						str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_MOD", BetterAPICompat.BetterAPIMap[ite].Name); // TODO currently this is just contentpack identifier, i need to get to bepinplugin
					else
						goto default;
					break;
				}*/
				case "RoR2.BaseContent":
					str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_VANILLA", "DLC 0");
					break;
				default:
				{
					if (WhatAmILookingAtPlugin.BetterAPIEnabled && BetterAPICompat.GetPluginFromAssembly(identifier, out var plugin))
					{
						str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_MOD", plugin!.Name);
					}
					else if (ContentPackToBepinPluginMap.ContainsKey(identifier))
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
				if (GetPluginFromAssembly(provider.GetType().Assembly, out var plugin))
				{
					if (!ContentPackToBepinPluginMap.ContainsKey(provider.identifier))
						ContentPackToBepinPluginMap.Add(provider.identifier, plugin);
					else
						WhatAmILookingAtPlugin.Log!.LogWarning("Key already exists for " + provider.identifier);
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

		private static void AddObject<T>(int offset, T obj)
		{
			if (obj == null) return;
			var assembly = new StackFrame(offset + 1).GetMethod().DeclaringType?.Assembly;
			if (!HarmonyPatches.GetPluginFromAssembly(assembly, out var plugin)) return;
			R2Map.Add(obj, plugin!);
		}

		[HarmonyPrefix, HarmonyPatch(typeof(ItemAPI), nameof(ItemAPI.Add), typeof(CustomItem))]
		public static void ItemPost(CustomItem? item)
		{
			if (item == null) return;
			AddObject(3, item.ItemDef);
		}
		
		[HarmonyPrefix, HarmonyPatch(typeof(ItemAPI), nameof(ItemAPI.Add), typeof(CustomEquipment))]
		public static void ItemPost(CustomEquipment? item)
		{
			if (item == null) return;
			AddObject(3, item.EquipmentDef);
		}

		[HarmonyPrefix, HarmonyPatch(typeof(LoadoutAPI), nameof(LoadoutAPI.AddSkillDef))]
		public static void SkillPost(SkillDef? s)
		{
			// TODO might need to do AddSkill too
			if (s == null) return;
			AddObject(2, s);
		}
		
		[HarmonyPostfix, HarmonyPatch(typeof(UnlockableAPI), nameof(UnlockableAPI.AddUnlockable), typeof(Type), typeof(Type), typeof(UnlockableDef))]
		public static void UnlockablePost(Type unlockableType, Type serverTrackerType, UnlockableDef unlockableDef, UnlockableDef __result)
		{
			AddObject(4, __result);
		}
		
		[HarmonyPrefix, HarmonyPatch(typeof(ArtifactAPI), nameof(ArtifactAPI.Add), typeof(ArtifactDef))]
		public static void ArtifactPost(ArtifactDef? artifactDef)
		{
			AddObject(2, artifactDef);
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
			AddObject(2, buff.BuffDef);
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

		//public static readonly Assembly BetterAPIAssembly = typeof(BetterAPIPlugin).Assembly;
		public static readonly Assembly WailaAssembly = typeof(WhatAmILookingAtPlugin).Assembly;
		
		/*[HarmonyPostfix, HarmonyPatch(typeof(Items), nameof(Items.Add), typeof(ItemDef), typeof(Items.CharacterItemDisplayRule[]), typeof(string))]
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
		//*/
		public static bool GetPluginFromAssembly(string identifier, out BepInPlugin? o)
		{
			var assembly = ContentPacks.FindAssembly(identifier);
			if (HarmonyPatches.GetPluginFromAssembly(assembly, out var plugin))
			{
				o = plugin;
				return true;
			}

			o = null;
			return false;
		}
	}
}