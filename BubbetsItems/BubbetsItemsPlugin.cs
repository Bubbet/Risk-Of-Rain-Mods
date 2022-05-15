using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
//using Aetherium; TODO readd support
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using BubbetsItems.Behaviours;
using EntityStates;
using HarmonyLib;
using RiskOfOptions.Options;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using Path = System.IO.Path;
using SearchableAttribute = HG.Reflection.SearchableAttribute;
[assembly: SearchableAttribute.OptIn]

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace BubbetsItems
{
    [BepInPlugin("bubbet.bubbetsitems", "Bubbets Items", "1.7.5")]
    //[BepInDependency(R2API.R2API.PluginGUID, BepInDependency.DependencyFlags.SoftDependency)]//, R2API.Utils.R2APISubmoduleDependency(nameof(R2API.RecalculateStatsAPI))]
    //[BepInDependency(AetheriumPlugin.ModGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.xoxfaby.BetterUI", BepInDependency.DependencyFlags.SoftDependency)]
    public class BubbetsItemsPlugin : BaseUnityPlugin
    {
        private const string AssetBundleName = "MainAssetBundle";
        
        public static ContentPack ContentPack;
        public static AssetBundle AssetBundle;
        public List<SharedBase> forwardTest => SharedBase.Instances;
        public static BubbetsItemsPlugin instance;
        public static ManualLogSource Log;

        public void Awake()
        {
            if (Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
                MakeRiskOfOptions();
            
            instance = this;
            Log = Logger;
            RoR2Application.isModded = true;
            Conf.Init(Config);
            var harm = new Harmony(Info.Metadata.GUID);
            LoadContentPack(harm);
            InLobbyConfigCompat.Init();
            RiskOfOptionsCompat.Init();

            new PatchClassProcessor(harm, typeof(HarmonyPatches)).Patch();
            new PatchClassProcessor(harm, typeof(PickupTooltipFormat)).Patch();
            new PatchClassProcessor(harm, typeof(LogBookPageScalingGraph)).Patch();
            new PatchClassProcessor(harm, typeof(ModdedDamageColors)).Patch();
            
            // Do not use SystemInitializers in PatchClassProcessors, because patch triggers the static constructor of SearchableAttribute breaking mods that load after
            new PatchClassProcessor(harm, typeof(EquipmentBase)).Patch();
            new PatchClassProcessor(harm, typeof(ItemBase)).Patch(); // Only used for filling void items.

            new PatchClassProcessor(harm, typeof(ExtraHealthBarSegments)).Patch();

            //NotSystemInitializer.Hook(harm);

            //Fucking bepinex pack constantly changing and now loading too late for searchableAttributes scan.
            //it changed again and no longer needs this
            //SearchableAttribute.ScanAssembly(Assembly.GetExecutingAssembly());

            //PickupTooltipFormat.Init(harm);
        }

        private void MakeRiskOfOptions()
        {
            RiskOfOptions.ModSettingsManager.AddOption(new GenericButtonOption("Report An Issue", "General", "If you find a bug in the mod, reporting an issue is the best way to ensure it gets fixed.","Open Link", () =>
            {
                Application.OpenURL("https://github.com/Bubbet/Risk-Of-Rain-Mods/issues/new");
            }));
            RiskOfOptions.ModSettingsManager.AddOption(new GenericButtonOption("Donate to Bubbet", "General", "Donate to the programmer of bubbet's items.","Open Link", () =>
            {
                Application.OpenURL("https://ko-fi.com/bubbet");
            }));
            RiskOfOptions.ModSettingsManager.AddOption(new GenericButtonOption("Donate to GEMO", "General", "Donate to the modeller of bubbet's items.", "Open Link", () =>
            {
                Application.OpenURL("https://ko-fi.com/snakeygemo/gallery");
            }));
        }

        private static uint _bankID;

        //[SystemInitializer]
        public static void LoadSoundBank()
        {
            if (Application.isBatchMode) return;
            try
            {
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                AkSoundEngine.AddBasePath(path);
                var result = AkSoundEngine.LoadBank("BubbetsItems.bnk", out _bankID);
                if (result != AKRESULT.AK_Success)
                    Debug.LogError("[Bubbets Items] SoundBank Load Failed: " + result);
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }
        }

        [SystemInitializer]
        public static void ExtraTokens()
        {
            Language.english.SetStringByToken("BUB_HOLD_TOOLTIP", "Hold Capslock for more.");
            
            Language.english.SetStringByToken("BUB_EXPANSION", "Bubbet's Content");
            Language.english.SetStringByToken("BUB_EXPANSION_DESC", "Adds content from 'Bubbets Items' to the game.");
            Language.english.SetStringByToken("BUB_EXPANSION_VOID", "Bubbet's Void Content");
            Language.english.SetStringByToken("BUB_EXPANSION_VOID_DESC", "Adds the void content from 'Bubbets Items' and requires 'Survivors of the Void'.");
        }

        public static class Conf
        {
            public static ConfigEntry<bool> AmmoPickupAsOrbEnabled;
            //public static bool RequiresR2Api;

            internal static void Init(ConfigFile configFile)
            {
                AmmoPickupAsOrbEnabled = configFile.Bind(ConfigCategoriesEnum.DisableModParts, "Ammo Pickup As Orb", true,  "Should the Ammo Pickup as an orb be enabled.");
            }
        }

        private void LoadContentPack(Harmony harmony)
        {
            var path = Path.GetDirectoryName(Info.Location);
            AssetBundle = AssetBundle.LoadFromFile(Path.Combine(path, AssetBundleName));
            var serialContent = AssetBundle.LoadAsset<BubsItemsContentPackProvider>("MainContentPack");

            var states = new List<SerializableEntityStateType>();
            foreach (var typ in Assembly.GetCallingAssembly().GetTypes())
            {
                if (!typeof(EntityState).IsAssignableFrom(typ)) continue;
                states.Add(new SerializableEntityStateType(typ));
            }
            serialContent.entityStateTypes = states.ToArray();
            
            SharedBase.Initialize(Logger, Config, serialContent, harmony, "BUB_");
            ContentPack = serialContent.CreateContentPack();
            SharedBase.AddContentPack(ContentPack);
            ContentPackProvider.Initialize(Info.Metadata.GUID, ContentPack);

            if (!Conf.AmmoPickupAsOrbEnabled.Value) return;
            var go = AssetBundle.LoadAsset<GameObject>("AmmoPickupOrb");
            //go.AddComponent<HarmonyPatches.AmmoPickupOrbBehavior>(); // Doing this at runtime to avoid reimporting component to unity
            ContentPack.effectDefs.Add(new[] {new EffectDef(go)});
        }

        private class ContentPackProvider : IContentPackProvider
        {
            private static ContentPack contentPack;
            private static string _identifier;
            public string identifier => _identifier;

            public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
            {
                //ContentPack.identifier = identifier;
                args.ReportProgress(1f);
                yield break;
            }

            public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
            {
                ContentPack.Copy(contentPack, args.output);
                //Log.LogError(ContentPack.identifier);
                args.ReportProgress(1f);
                yield break;
            }

            public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
            {
                args.ReportProgress(1f);
                Log.LogInfo("Contentpack finished");
                yield break;
            }

            internal static void Initialize(string identifier, ContentPack pack)
            {
                _identifier = identifier;
                contentPack = pack;
                ContentManager.collectContentPackProviders += AddCustomContent;
            }

            private static void AddCustomContent(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
            {
                addContentPackProvider(new ContentPackProvider());
            }
        }
    }
}