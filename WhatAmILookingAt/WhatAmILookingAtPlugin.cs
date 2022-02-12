using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using RoR2;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
//[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace WhatAmILookingAt
{
	// needs to be prefixed with aaaa so it loads before all the mods that require r2api
	[BepInPlugin("aaaa.bubbet.whatamilookingat", "What Am I Looking At", "1.1.0")]
	[BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("com.xoxfaby.BetterAPI", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("com.xoxfaby.BetterUI", BepInDependency.DependencyFlags.SoftDependency)]
	public class WhatAmILookingAtPlugin : BaseUnityPlugin
	{
		public static WhatAmILookingAtPlugin instance;
		public static ManualLogSource Log;
		//public static Dictionary<string, ContentPack> ContentPacksForward => HarmonyPatches.contentPacks;
		public static HarmonyPatches test = new HarmonyPatches();
		public static bool BetterUIEnabled;

		public void Awake()
		{
			instance = this;
			Log = Logger;
			var harm = new Harmony(Info.Metadata.GUID);
			new PatchClassProcessor(harm, typeof(HarmonyPatches)).Patch();
			//harm.PatchAll();

			if (Chainloader.PluginInfos.ContainsKey("com.bepis.r2api")) // Dynamically patch r2api methods instead of via attributes
			{
				Log.LogInfo("Patching R2Api");
				var processor = new PatchClassProcessor(harm, typeof(R2APICompat), false);
				processor.Patch();
			}
			
			if (Chainloader.PluginInfos.ContainsKey("com.xoxfaby.BetterAPI")) // Dynamically patch betterapi methods instead of via attributes
			{
				Log.LogInfo("Patching BetterAPI");
				var processor = new PatchClassProcessor(harm, typeof(BetterAPICompat), false);
				processor.Patch();
			}

			BetterUIEnabled = Chainloader.PluginInfos.ContainsKey("com.xoxfaby.BetterUI");

			//var methodInfo = typeof(ContentManager).GetMethod(nameof(ContentManager.LoadContentPacks))?.ReturnType.GetMethod(nameof(IEnumerator.MoveNext));

			//var where = new HarmonyMethod(typeof(HarmonyPatches).GetMethod(nameof(HarmonyPatches.UnlockablePost)));
			//var what = typeof(UnlockableAPI).GetMethod(nameof(UnlockableAPI.AddUnlockable), new[] {typeof(bool)});
			//harm.Patch(what, null, where);
			/*
			Debug.Log(what);
			var methodInfo = what?.MakeGenericMethod(typeof(BaseAchievement));
			harm.Patch(methodInfo, null, null, null, null, where);
			methodInfo = what?.MakeGenericMethod(typeof(IModdedUnlockableDataProvider));
			harm.Patch(methodInfo, null, null, null, null, where);
			*/
		}
		

		//[SystemInitializer(typeof(Language))] fuck you then i'll use a hook
		public static void ExtraTokens()
		{
			// BUB_WAILA_TOOLTIP_MOD
			// BUB_WAILA_TOOLTIP_VANILLA
			// BUB_WAILA_TOOLTIP_UNKNOWN
			
			Language.english.SetStringByToken("BUB_WAILA_TOOLTIP_MOD", "<color=#0055FF>From Mod: {0}</color>");
			Language.english.SetStringByToken("BUB_WAILA_TOOLTIP_VANILLA", "<color=#0055FF>From Vanilla: {0}</color>");
			Language.english.SetStringByToken("BUB_WAILA_TOOLTIP_UNKNOWN", "<color=#0055FF>From Unknown (Report To Mod Author)</color>");
			
			//Language.english.SetStringByToken("BUB_TOOLTIP_WAILA", "<color=#0055FF>From Mod: {0}</color>");
			//Language.english.SetStringByToken("BUB_VANILLA", "<color=#0055FF>From Vanilla</color>");
		}
	}
}