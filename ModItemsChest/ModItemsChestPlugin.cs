using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace ModItemsChest
{
    [R2APISubmoduleDependency("LanguageAPI")]//, "PrefabAPI")]
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("bubbet.plugins.moditemschest", "ModItemsChest", "1.0.5.0")]
    public class ModItemsChestPlugin : BaseUnityPlugin
    {
        private static Mesh _categorySymbol;
        private static Texture2D _categoryTexture;
        private static ConfigEntry<float> _bias;
        private static ConfigEntry<int> chestCost;
        public static ConfigEntry<float> tier1Chance;
        public static ConfigEntry<float> tier2Chance;
        public static ConfigEntry<float> tier3Chance;
        
/*
        static void DumpChildren(ManualLogSource log, Transform parent, int depth = 0)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                log.LogInfo("Depth(" + depth + "):" + child);
                DumpChildren(log, child, depth + 1);
            }
        }
*/
        private static Transform FindChild(Transform parent, string name)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var chi = parent.GetChild(i);
                if (chi.name == name)
                {
                    return chi;
                }

                var chi2 = FindChild(chi, name);
                if (chi2 != null) return chi2;
            }

            return null;
        }

        public void Awake()
        {
            _bias = Config.Bind("General", "Bias", 0.5f,
                "Used to sway the generation of chests to include more vanilla items, 0 = all mod items, 1 = all vanilla items");
            chestCost = Config.Bind("General", "Chest Cost", 25, "Cost of the modded chest.");
            tier1Chance = Config.Bind("General", "Tier 1 Chance", 0.8f, "Chance for tier 1 mod item from the chest");
            tier2Chance = Config.Bind("General", "Tier 2 Chance", 0.2f, "Chance for tier 2 mod item from the chest");
            tier3Chance = Config.Bind("General", "Tier 3 Chance", 0.01f, "Chance for tier 3 mod item from the chest");

            new Harmony("bubbet.plugins.moditemschest").PatchAll();
            
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ModItemsChest.modchests"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                const string root = "assets/bundledassets/modchests/";
                _categorySymbol = bundle.LoadAsset<Mesh>(root + "CategorySymbolModItem.obj");
                _categoryTexture = bundle.LoadAsset<Texture2D>(root + "texTrimSheetConstructionOrange.png");
            }

            LanguageAPI.Add("CATEGORYCHEST_MODITEM_NAME", "Chest - Mod Item");
            LanguageAPI.Add("CATEGORYCHEST_MODITEM_CONTEXT", "Open Chest - Mod Item");

            SceneDirector.onGenerateInteractableCardSelection += (director, selection) =>
            {
                selection.AddCard(0, ModItemsChestPlugin.get_moddedChestDirectorCard(selection));
            };
        }
        

        private static DirectorCard _moddedChestDirectorCard;
        public static int ratio;
        private static GameObject _oldPrefab;

        public static GameObject makeNewPrefab()
        {
            var prefab = Instantiate(_oldPrefab);
            
            prefab.transform.position = new Vector3(0,-100000,0);
            
            var symbol = FindChild(prefab.transform, "CategorySymbol"); //prefab.transform.Find("CategorySymbol");
            symbol.GetComponent<MeshRenderer>().material.color = new Color(1f, 0.7f, 0.2f);
            symbol.GetComponent<MeshFilter>().mesh = _categorySymbol;
            prefab.GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = _categoryTexture;

            prefab.name = "Modded Chest";
            Destroy(prefab.GetComponent<ChestBehavior>());
            prefab.AddComponent<ModChestBehavior>();

            var purchaseInteraction = prefab.GetComponent<PurchaseInteraction>();
            purchaseInteraction.cost = chestCost.Value;
            purchaseInteraction.displayNameToken = "CATEGORYCHEST_MODITEM_NAME";
            purchaseInteraction.contextToken = "CATEGORYCHEST_MODITEM_CONTEXT";

            return prefab;
        }
        public static DirectorCard get_moddedChestDirectorCard(DirectorCardCategorySelection selection)
        {
            if (_moddedChestDirectorCard == null)
            {
                var oldSpawnCard = Resources.Load<InteractableSpawnCard>("SpawnCards/InteractableSpawnCard/iscCategoryChestDamage");
                var spawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
                _oldPrefab = oldSpawnCard.prefab;

                spawnCard.eliteRules = oldSpawnCard.eliteRules;
                spawnCard.forbiddenFlags = oldSpawnCard.forbiddenFlags;
                spawnCard.hullSize = oldSpawnCard.hullSize;
                spawnCard.occupyPosition = oldSpawnCard.occupyPosition;
                spawnCard.requiredFlags = oldSpawnCard.requiredFlags;
                spawnCard.nodeGraphType = oldSpawnCard.nodeGraphType;
                spawnCard.sendOverNetwork = true; //oldSpawnCard.sendOverNetwork;
                spawnCard.orientToFloor = oldSpawnCard.orientToFloor;
                spawnCard.slightlyRandomizeOrientation = oldSpawnCard.slightlyRandomizeOrientation;
                spawnCard.skipSpawnWhenSacrificeArtifactEnabled = true;//spawnCard.skipSpawnWhenSacrificeArtifactEnabled;

                spawnCard.directorCreditCost = 15;

                float vanilla = DisableModItemsInNormalChests.VanillaItemIndexes.Count;
                float modded = DisableModItemsInNormalChests.ModdedItemDefs.Count;

                var rat = Mathf.Clamp01(vanilla / (vanilla + modded) + (_bias.Value - 0.5f));
                var oldWeight = selection.categories[0].cards[0].selectionWeight;
                ratio = (int) (oldWeight * rat);
                
                _moddedChestDirectorCard = new DirectorCard
                {
                    spawnCard = spawnCard, selectionWeight = (int) (oldWeight * (1 - rat))
                };
/*
                if (false)
                {
                    DumpChildren(Logger, spawnCard.prefab.transform);
                    foreach (var dircard in selection.categories[0].cards)
                    {
                        Logger.LogInfo(dircard.spawnCard.prefab.name);
                        Logger.LogInfo(dircard.cost);
                        Logger.LogInfo(dircard.selectionWeight);
                    }

                    Logger.LogInfo("Ratios:");
                    Logger.LogInfo(ratio);
                    Logger.LogInfo(oldWeight * ratio);
                    Logger.LogInfo(oldWeight * (1 - ratio));
                    Logger.LogInfo(DisableModItemsInNormalChests.ModdedItemDefs.Count);
                    Logger.LogInfo(DisableModItemsInNormalChests.VanillaItemIndexes.Count);
                }
*/
            }

            _moddedChestDirectorCard.spawnCard.prefab = makeNewPrefab();
            selection.categories[0].cards[0].selectionWeight = ratio;
            return _moddedChestDirectorCard;
        }
    }
}