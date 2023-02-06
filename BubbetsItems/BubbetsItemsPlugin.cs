using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using Aetherium;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using BetterUI;
using BubbetsItems.Behaviours;
using BubbetsItems.Components;
using EntityStates;
using HarmonyLib;
using R2API;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Path = System.IO.Path;
using SearchableAttribute = HG.Reflection.SearchableAttribute;

[assembly: SearchableAttribute.OptIn]

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace BubbetsItems
{
    [BepInPlugin("bubbet.bubbetsitems", "Bubbets Items", "1.8.9")]
    [BepInDependency(RecalculateStatsAPI.PluginGUID)]
    //[BepInDependency(R2API.R2API.PluginGUID, BepInDependency.DependencyFlags.SoftDependency)]//, R2API.Utils.R2APISubmoduleDependency(nameof(R2API.RecalculateStatsAPI))]
    [BepInDependency(AetheriumPlugin.ModGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("bubbet.zioriskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("bubbet.zioconfigfile", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.xoxfaby.BetterUI", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Moffein.ItemStats", BepInDependency.DependencyFlags.SoftDependency)] // Required to make sure my pickup description hook runs after
    public class BubbetsItemsPlugin : BaseUnityPlugin
    {
        private const string AssetBundleName = "MainAssetBundle";
        
        public static ContentPack ContentPack;
        public static AssetBundle AssetBundle;
        public List<SharedBase> forwardTest => SharedBase.Instances;

        public static PickupIndex[] VoidLunarItems => _voidLunarItems ??= ItemCatalog.allItemDefs
            .Where(x => x.tier == VoidLunarTier.tier)
            .Select(x => PickupCatalog.FindPickupIndex(x.itemIndex)).ToArray();

        public static BubbetsItemsPlugin instance;
        public static ManualLogSource Log;
        
        private static ExpansionDef? _bubExpansion;
        private static ExpansionDef? _bubSotvExpansion;

        public static ExpansionDef BubExpansion
        {
            get
            {
                if (_bubExpansion is null)
                {
                    _bubExpansion = ContentPack.expansionDefs.First(x => x.nameToken == "BUB_EXPANSION");
                    _bubExpansion.disabledIconSprite = BubSotvExpansion.disabledIconSprite;
                }

                return _bubExpansion;
            }
        }

        public static ExpansionDef BubSotvExpansion
        {
            get
            {
                if (_bubSotvExpansion is null)
                {
                    _bubSotvExpansion = ContentPack.expansionDefs.First(x => x.nameToken == "BUB_EXPANSION_VOID");
                    var sotv = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();//ExpansionCatalog.expansionDefs.First(x => x.nameToken == "DLC1_NAME");
                    _bubSotvExpansion.requiredEntitlement = sotv.requiredEntitlement;
                    _bubSotvExpansion.disabledIconSprite = sotv.disabledIconSprite;
                }

                return _bubSotvExpansion;
            }
        }

        public void Awake()
        {
            Conf.Init(Config);
            if (Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
                MakeRiskOfOptions();
            
            instance = this;
            Log = Logger;
            RoR2Application.isModded = true;
            var harm = new Harmony(Info.Metadata.GUID);
            LoadContentPack(harm);
            
            VoidLunarShopController.Init();

            InLobbyConfigCompat.Init();
            RiskOfOptionsCompat.Init();

            new PatchClassProcessor(harm, typeof(CommonBodyPatches)).Patch();
            new PatchClassProcessor(harm, typeof(HarmonyPatches)).Patch();
            new PatchClassProcessor(harm, typeof(PickupTooltipFormat)).Patch();
            new PatchClassProcessor(harm, typeof(LogBookPageScalingGraph)).Patch();
            new PatchClassProcessor(harm, typeof(ModdedDamageColors)).Patch();
            new PatchClassProcessor(harm, typeof(CustomItemTierDefs)).Patch();
            new PatchClassProcessor(harm, typeof(ColorCatalogPatches)).Patch();
            ColorCatalogPatches.AddNewColors();
            
            // Do not use SystemInitializers in PatchClassProcessors, because patch triggers the static constructor of SearchableAttribute breaking mods that load after
            new PatchClassProcessor(harm, typeof(EquipmentBase)).Patch();
            new PatchClassProcessor(harm, typeof(ItemBase)).Patch(); // Only used for filling void items.

            new PatchClassProcessor(harm, typeof(VoidLunarShopController)).Patch();
            new PatchClassProcessor(harm, typeof(ExtraHealthBarSegments)).Patch();

            RoR2Application.onLoad += onLoad;
            //NotSystemInitializer.Hook(harm);
            
            //Fucking bepinex pack constantly changing and now loading too late for searchableAttributes scan.
            //it changed again and no longer needs this
            //SearchableAttribute.ScanAssembly(Assembly.GetExecutingAssembly());
            Language.collectLanguageRootFolders += list =>
            {
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Language";
                if (File.Exists(path))
                    list.Add(path);
            };

            //PickupTooltipFormat.Init(harm);
            ItemStatsCompat.Init();
        }

        private void onLoad()
        {
            if (Chainloader.PluginInfos.ContainsKey("bubbet.zioconfigfile"))
            {
                ZioConfigSetup();
                if (Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
                    Conf.MakeRiskOfOptionsZio();
            }
            ConfigCategories.Init();
            if (Chainloader.PluginInfos.ContainsKey("com.xoxfaby.BetterUI"))
                AddItemTierToBetterUI();
        }

        private void ZioConfigSetup()
        {
            //zConfigFile = new Z ioConfigFile.ZioConfigFile(RoR2Application.cloudStorage, "/BubbetsItems.cfg", true, this);
            Conf.InitZio(Config); //zConfigFile); // TODO create wrapper that can handle both zio and normal.
        }

        private void AddItemTierToBetterUI()
        {
            ItemSorting.tierMap.Add(VoidLunarTier.tier, 3);
        }

        private void MakeRiskOfOptions()
        {
            riskOfOptionsEnabled = true;
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
            Conf.MakeRiskOfOptions();
        }

        private static uint _bankID;
        public static ItemTierDef VoidLunarTier;
        private static PickupIndex[]? _voidLunarItems;
        //private ZioConfigFile.ZioConfigFile zConfigFile;
        private SharedBase.SharedInfo sharedInfo;
        public static bool riskOfOptionsEnabled;

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
            Language.english.SetStringByToken("BUB_DEFAULT_CONVERT", "Corrupts all {0}.");
        }

        public static class Conf
        {
            public static ConfigEntry<bool> AmmoPickupAsOrbEnabled;
            public static ConfigEntry<bool> VoidCoinShareOnPickup;
            public static ConfigEntry<float> VoidCoinDropChanceStart;
            public static ConfigEntry<float> VoidCoinDropChanceMult;
            public static ConfigEntry<bool> VoidCoinBarrelDrop;
            public static ConfigEntry<bool> VoidCoinVoidFields;
            public static ConfigEntry<float> EffectVolume;
            //public static bool RequiresR2Api;

            internal static void Init(ConfigFile configFile)
            {
                AmmoPickupAsOrbEnabled = configFile.Bind(ConfigCategoriesEnum.DisableModParts, "Ammo Pickup As Orb", true,  "Should the Ammo Pickup as an orb be enabled.");
                VoidCoinShareOnPickup = configFile.Bind(ConfigCategoriesEnum.General, "Share Void Coin On Pickup", false, "Should void coins share on pickup.");
                VoidCoinDropChanceStart = configFile.Bind(ConfigCategoriesEnum.General, "Void Coin Drop Chance", 10f, "Not used if using Released from the void. Starting drop chance of void coins from void guys.");
                VoidCoinDropChanceMult = configFile.Bind(ConfigCategoriesEnum.General, "Void Coin Drop Chance Mult", 0.5f, "Not used if using Released from the void. Drop chance multiplier to chance upon getting coin.");
                VoidCoinBarrelDrop = configFile.Bind(ConfigCategoriesEnum.General, "Void Coin Drop From Void Barrel", true, "Not used if using Released from the void. Should the void coin drop from barrels.");
                VoidCoinVoidFields = configFile.Bind(ConfigCategoriesEnum.General, "Void Coin Drop From Void Fields", true, "Should the void coin drop from void fields.");
            }

            internal static void InitZio(ConfigFile configFile)
            {
                EffectVolume = configFile.Bind(ConfigCategoriesEnum.General, "Effect Volume", 50f, "Volume of the sound effects in my mod.", networked: false);
                EffectVolume.SettingChanged += (_, _) => AkSoundEngine.SetRTPCValue("Volume_Effects", EffectVolume.Value);
                AkSoundEngine.SetRTPCValue("Volume_Effects", EffectVolume.Value);
                
                instance.sharedInfo.MakeZioOptions(configFile);
            }

            internal static void MakeRiskOfOptions()
            {
                RiskOfOptions.ModSettingsManager.AddOption(new CheckBoxOption(VoidCoinShareOnPickup));
                RiskOfOptions.ModSettingsManager.AddOption(new SliderOption(VoidCoinDropChanceStart));
                RiskOfOptions.ModSettingsManager.AddOption(new SliderOption(VoidCoinDropChanceMult, new SliderConfig {min = 0, max = 1, formatString = "{0:0.##%}"}));
                RiskOfOptions.ModSettingsManager.AddOption(new CheckBoxOption(VoidCoinBarrelDrop, true));
                RiskOfOptions.ModSettingsManager.AddOption(new CheckBoxOption(VoidCoinVoidFields));
            }

            internal static void MakeRiskOfOptionsZio()
            {
                RiskOfOptions.ModSettingsManager.AddOption(new SliderOption(EffectVolume));
            }
        }

        private void LoadContentPack(Harmony harmony)
        {
            var path = Path.GetDirectoryName(Info.Location);
            AssetBundle = AssetBundle.LoadFromFile(Path.Combine(path, AssetBundleName));
            var serialContent = AssetBundle.LoadAsset<BubsItemsContentPackProvider>("MainContentPack");
            CustomItemTierDefs.Init(serialContent);

            var states = new List<SerializableEntityStateType>();
            foreach (var typ in Assembly.GetCallingAssembly().GetTypes())
            {
                if (!typeof(EntityState).IsAssignableFrom(typ)) continue;
                states.Add(new SerializableEntityStateType(typ));
            }
            serialContent.entityStateTypes = states.ToArray();
            
            SharedBase.Initialize(Logger, Config, out sharedInfo, serialContent, harmony, "BUB_");
            ContentPack = serialContent.CreateContentPack();
            SharedBase.AddContentPack(ContentPack);
            ContentPackProvider.Initialize(Info.Metadata.GUID, ContentPack, sharedInfo);

            if (!Conf.AmmoPickupAsOrbEnabled.Value) return;
            var go = AssetBundle.LoadAsset<GameObject>("AmmoPickupOrb");
            //go.AddComponent<HarmonyPatches.AmmoPickupOrbBehavior>(); // Doing this at runtime to avoid reimporting component to unity
            ContentPack.effectDefs.Add(new[] {new EffectDef(go)});
        }

        private class ContentPackProvider : IContentPackProvider
        {
            private static ContentPack contentPack;
            private static string _identifier;
            private static SharedBase.SharedInfo info;
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
                info.Expansion = BubExpansion;
                info.SotVExpansion = BubSotvExpansion;
                yield break;
            }

            internal static void Initialize(string identifier, ContentPack pack, SharedBase.SharedInfo sharedInfo)
            {
                _identifier = identifier;
                contentPack = pack;
                info = sharedInfo;
                ContentManager.collectContentPackProviders += AddCustomContent;
            }

            private static void AddCustomContent(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
            {
                addContentPackProvider(new ContentPackProvider());
            }
        }
    }
}