#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using R2API.ContentManagement;
using RoR2;
using RoR2.ContentManagement;
using RoR2.UI;
using SimpleJSON;
using UnityEngine;
using Path = RoR2.Path;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace WhatAmILookingAt // TODO waila in world might fail to find r2api etc version if they only add networked objects
{
	// needs to be prefixed with aaaa so it loads before all the mods that require r2api
	[BepInPlugin("aaaa.bubbet.whatamilookingat", "What Am I Looking At", "1.6.1")]
	[BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.SoftDependency)]
	//[BepInDependency("com.ThinkInvisible.TILER2", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("com.xoxfaby.BetterAPI", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("com.xoxfaby.BetterUI", BepInDependency.DependencyFlags.SoftDependency)]
	public class WhatAmILookingAtPlugin : BaseUnityPlugin
	{
		public enum InWorldOptions
		{
			Disabled,
			WhileScoreboardOpen,
			AlwaysOn
		}
		
		public static ConfigEntry<string>? TextColor;
		public static ConfigEntry<InWorldOptions>? RequireTABForInWorld;
		public static ConfigEntry<bool>? StageOnlyInTab;
		
		public static WhatAmILookingAtPlugin? Instance;
		public static ManualLogSource? Log;
		private static readonly Type UnityPlugin = typeof(BaseUnityPlugin);
		
		public static bool BetterUIEnabled;
		public static bool BetterAPIEnabled;
		public static bool R2APIEnabled;
		//public static bool TILER2Enabled;

		/// <summary>
		/// Contentpack Identifier to BepinPluginMap
		/// Populated in hook when parsing content pack providers.
		/// Needed for all TK related packs.
		/// </summary>
		public static readonly Dictionary<string, BepInPlugin> ContentPackToBepinPluginMap = new Dictionary<string, BepInPlugin>();
		/// <summary>
		/// ItemDef, EquipmentDef, etc to Content Pack Identifier
		/// </summary>
		public static readonly Dictionary<object, string?> IdentifierMap = new Dictionary<object, string?>();
		private static readonly List<string> UnknownIdentifiers = new List<string>();
		public static readonly Dictionary<SkinDef, BepInPlugin> skinDefMap = new();


		public static List<HUD> HUDs = new List<HUD>();
		
		public void Awake()
		{
			Instance = this;
			Log = Logger;
			var harm = new Harmony(Info.Metadata.GUID);
			new PatchClassProcessor(harm, typeof(HarmonyPatches)).Patch();
			
			BetterAPIEnabled = Chainloader.PluginInfos.ContainsKey("com.xoxfaby.BetterAPI");
			BetterUIEnabled = Chainloader.PluginInfos.ContainsKey("com.xoxfaby.BetterUI");
			R2APIEnabled = Chainloader.PluginInfos.ContainsKey(R2APIContentManager.PluginGUID); 
			/*TILER2Enabled = Chainloader.PluginInfos.ContainsKey("com.ThinkInvisible.TILER2");
			
			if (TILER2Enabled)
			{
				ExternalAPICompat.PatchTiler2(harm);
			}*/

			//var createInstance = typeof(ScriptableObject).GetMethod("CreateInstance", new[] {typeof(Type)});
			//if(createInstance != null)
				//harm.Patch(createInstance, null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod(nameof(HarmonyPatches.SkinDefConstructor), BindingFlags.Public | BindingFlags.Static)));
			

			RoR2Application.onLoad += ExtraTokens;
			TextColor = Config.Bind("General", "Text Color", "#0055FF", "Color of the text displaying what mod something is from.");
			RequireTABForInWorld = Config.Bind("General", "In World Setting", InWorldOptions.AlwaysOn, "When should the in world waila be displayed");
			StageOnlyInTab = Config.Bind("General", "Stage Only In Tab", false, "In world waila only displays the stage when holding tab");
			GenerateChecks();
			
			HUD.shouldHudDisplay += CreateHud;
		}
		
		private static void CreateHud(HUD hud, ref bool shoulddisplay)
		{
			if (HUDs.Contains(hud)) return;
			hud.gameObject.AddComponent<WailaHud>();
			HUDs.Add(hud);
		}


		//[SystemInitializer(typeof(Language))] fuck you then i'll subscribe to onload
		public static void ExtraTokens()
		{
			Language.english.SetStringByToken("BUB_WAILA_TOOLTIP_MOD", "<color={0}>From Mod: {1}</color>");
			Language.english.SetStringByToken("BUB_WAILA_TOOLTIP_VANILLA", "<color={0}>From Vanilla: {1}</color>");
			Language.english.SetStringByToken("BUB_WAILA_TOOLTIP_UNKNOWN", "<color={0}>From Unknown (Report To Mod Author)</color>");
		}

		private static Dictionary<Assembly, string> manifestNameMap = new();
		public static string? TryGetManifestName(Assembly assembly)
		{
			if (manifestNameMap.ContainsKey(assembly)) return manifestNameMap[assembly];
			var path = System.IO.Path.GetDirectoryName(assembly.Location);
			try
			{
				var fpath = path;
				var mpath = System.IO.Path.Combine(fpath, "manifest.json");
				while (!File.Exists(mpath))
				{
					fpath = Directory.GetParent(fpath).ToString();
					mpath = System.IO.Path.Combine(fpath, "manifest.json");
				}
				var file = File.OpenText(mpath);

				var jsonNode = JSON.Parse(file!.ReadToEnd());
				var desc = jsonNode["name"].Value;
				manifestNameMap[assembly] = desc;
				return desc;
			}
			catch (Exception)
			{
				// ignored
			}
			return null;
		}
		private static string? TryGetManifestName(BepInPlugin plugin)
		{
			return BepinPluginToAssemblyMap.TryGetValue(plugin, out var assembly) ? TryGetManifestName(assembly) : null;
		}
		
		/// <summary>
		/// Get the text that goes at the bottom of the tooltip from a contentpack identifier.
		/// </summary>
		/// <param name="identifier">The contentpack identifier.</param>
		/// <returns>The completed and translated string from the identifier.</returns>
		public static string? GetModString(string identifier)
		{
			string? str;
			
			switch (identifier)
			{
				case "RoR2.BaseContent":
					str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_VANILLA", TextColor!.Value, "DLC 0");
					break;
				case "RoR2.DLC1":
					str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_VANILLA", TextColor!.Value, $"DLC 1 ({Language.GetString("DLC1_NAME")})");
					break;
				case "RoR2.JunkContent":
					str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_VANILLA", TextColor!.Value, "DLC 0 Junk");
					break;
				default:
				{
					if (BetterAPIEnabled && ExternalAPICompat.GetPluginFromBetterAPI(identifier, out var plugin))
					{
						var name = TryGetManifestName(plugin!) ?? plugin.Name;
						str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_MOD", TextColor!.Value, name);
					}
					else if (R2APIEnabled && ExternalAPICompat.GetPluginFromR2API(identifier, out var pluginr2))
					{
						var name = TryGetManifestName(pluginr2!) ?? pluginr2.Name;
						str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_MOD", TextColor!.Value, name);
					}
					else if (ContentPackToBepinPluginMap.ContainsKey(identifier))
					{
						var pluginc = ContentPackToBepinPluginMap[identifier];
						var name = TryGetManifestName(pluginc) ?? pluginc.Name;
						str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_MOD", TextColor!.Value, name);
					}
					else
					{
						if (!UnknownIdentifiers.Contains(identifier))
						{
							Log!.LogWarning($"Failed to find mod for {identifier}");
							UnknownIdentifiers.Add(identifier);
						}

						str = Language.GetStringFormatted("BUB_WAILA_TOOLTIP_UNKNOWN", TextColor!.Value);
					}

					break;
				}
			}

			return str;
		}

		private static void GenerateChecks()
		{
			WhatAmILookingAtBodyChecks.Register(ref BodyChecks);
			WhatAmILookingAtBodyTextChecks.Register(ref BodyTextChecks);
			WailaInWorldChecks.Register();
		}

		/// <summary> Delegate for finding a identifier from a token. </summary>
		/// <param name="body">The body token, title token, then body text.</param>
		/// <param name="identifier">The return value, our identifier.</param>
		public delegate void StringTest(string body, ref string? identifier);
		/// <summary> Body Token to ItemDef, EquipmentDef, etc </summary>
		public static readonly Dictionary<string, string?> BodyTokenMap = new Dictionary<string, string?>();
		/// <summary>
		/// Check the body token.
		/// First subscription to set the second string to something wins.
		/// </summary>
		public static event StringTest? BodyChecks;
		/// <summary> Title Token to ItemDef, EquipmentDef, etc </summary>
		public static readonly Dictionary<string, string?> TitleTokenMap = new Dictionary<string, string?>();
		/// <summary>
		/// Check the title token.
		/// First subscription to set the second string to something wins.
		/// </summary>
		public static event StringTest? TitleChecks;
		/// <summary> Body Text to ItemDef, EquipmentDef, etc </summary>
		public static readonly Dictionary<string, string?> BodyTextMap = new Dictionary<string, string?>();

		public static Dictionary<BepInPlugin, Assembly> BepinPluginToAssemblyMap = new();

		/// <summary>
		/// Check the body text, last resort.
		/// First subscription to set the second string to something wins.
		/// </summary>
		public static event StringTest? BodyTextChecks;

		public static string? FindItem(TooltipProvider provider, string bodyText)
		{
			var body = provider.bodyToken;
			var titleToken = provider.titleToken;

			string? ite = null;
			BuffIcon icon;

			if (BetterUIEnabled && (icon = provider.GetComponent<BuffIcon>()) != null)
				ite = GetIdentifier(icon.buffDef);
			else if (!string.IsNullOrEmpty(body) && BodyChecks != null)
			{
				if (!BodyTokenMap.ContainsKey(body))
				{
					foreach (var check in BodyChecks.GetInvocationList())
					{
						(check as StringTest)?.Invoke(body, ref ite);
						if (ite != null) break;
					}
					if (ite != null)
						BodyTokenMap.Add(body, ite!);
				}
				BodyTokenMap.TryGetValue(body, out ite);
			}
			else if (!string.IsNullOrEmpty(titleToken) && TitleChecks != null)
			{
				if (!TitleTokenMap.ContainsKey(titleToken))
				{
					foreach (var check in TitleChecks.GetInvocationList())
					{
						(check as StringTest)?.Invoke(body, ref ite);
						if (ite != null) break;
					}
					if (ite != null)
						TitleTokenMap.Add(body, ite!);
				}
				TitleTokenMap.TryGetValue(body, out ite);
			}
			else if (!string.IsNullOrEmpty(bodyText) && BodyTextChecks != null)
			{
				if (!BodyTextMap.ContainsKey(bodyText))
				{
					foreach (var check in BodyTextChecks.GetInvocationList())
					{
						(check as StringTest)?.Invoke(bodyText, ref ite);
						if (ite != null) break;
					}
					if (ite != null)
						BodyTextMap.Add(bodyText, ite);
				}
				BodyTextMap.TryGetValue(bodyText, out ite);
			}
			
			return ite;
		}
		
		private static string? FindIdentifier<T>(T item)
		{
			foreach (var pack in ContentManager.allLoadedContentPacks)
			{
				foreach (var assetCollection in pack.src.assetCollections)
				{
					//var typ = assetCollection.GetType().GenericTypeArguments[0];
					//if (!typ.IsInstanceOfType(item)) continue;
					if (!(assetCollection is NamedAssetCollection<T> coll)) continue;
					if (coll.Contains(item))
						return pack.identifier;
				}
			}

			Log!.LogWarning("Failed to find pack for " + item);
			return null;
		}

		/// <summary>
		/// Get identifier from ItemDef, EquipmentDef, etc
		/// </summary>
		/// <param name="item">ItemDef, EquipmentDef, etc used to find identifier</param>
		/// <typeparam name="T">Type of item we're using to look for the identifier</typeparam>
		/// <returns>The identifier, or null if it can't find one.</returns>
		public static string? GetIdentifier<T>(T item)
		{
			if (item == null) return null;
			if (!IdentifierMap.ContainsKey(item!))
				IdentifierMap.Add(item, FindIdentifier(item));
			return IdentifierMap[item];
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
				Log!.LogInfo(assembly);
				Log.LogError(e);
				plugin = null;
				return false;
			}
		}
	}
}