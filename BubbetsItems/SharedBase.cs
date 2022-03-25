#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using UnityEngine;

namespace BubbetsItems
{
    public abstract class SharedBase
    {
        protected virtual void MakeConfigs() {}
        protected virtual void MakeTokens(){}
        protected virtual void MakeBehaviours(){} 
        protected virtual void DestroyBehaviours(){}

        //public virtual void MakeInLobbyConfig(ModConfigEntry modConfigEntry){}
        public virtual void MakeInLobbyConfig(Dictionary<ConfigCategoriesEnum, List<object>> dict){} // Has to be list of object because this class cannot have reference to inlobbyconfig, incase its not loaded

        public ConfigEntry<bool>? Enabled;
        public static readonly List<SharedBase> Instances = new List<SharedBase>();
        public static readonly Dictionary<PickupIndex, SharedBase> PickupIndexes = new Dictionary<PickupIndex, SharedBase>();
        public PickupIndex PickupIndex;

        protected ManualLogSource? Logger;
        protected Harmony? Harmony;
        protected PatchClassProcessor? PatchProcessor;
        protected static readonly List<ContentPack> ContentPacks = new List<ContentPack>();
        private string? _tokenPrefix;
        
        private static ExpansionDef? _sotvExpansion;
        protected ConfigFile configFile;
        protected ConfigEntry<bool> expandedTooltips;
        public ConfigEntry<bool> descInPickup;

        public static ExpansionDef? SotvExpansion
        {
            get
            {
                if (_sotvExpansion == null)
                    _sotvExpansion = ExpansionCatalog.expansionDefs.FirstOrDefault(x => x.nameToken == "DLC1_NAME");
                return _sotvExpansion;
            }
        }
        
        //This is probably bad practice
        public virtual bool RequiresSotv => ((this as ItemBase)?.voidPairings.Count ?? 0) > 0;
        
        public virtual string GetFormattedDescription(Inventory? inventory, string? token = null)
        {
            return "Not Implemented";
        }
        
        public static void Initialize(ManualLogSource manualLogSource, ConfigFile configFile, SerializableContentPack? serializableContentPack = null, Harmony? harmony = null, string tokenPrefix = "")
        {
            var localInstances = new List<SharedBase>();
            var expandedTooltips = configFile.Bind(ConfigCategoriesEnum.General, "Expanded Tooltips", true, "Enables the scaling function in the tooltip.");
            var descInPickup = configFile.Bind(ConfigCategoriesEnum.General, "Description In Pickup", true, "Used the description in the pickup for my items.");
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                //if(type == typeof(SharedBase) || type == typeof(ItemBase) || type == typeof(EquipmentBase)) continue;
                if (!typeof(SharedBase).IsAssignableFrom(type)) continue; // || typeof(SharedBase) == type || typeof(ItemBase) == type || typeof(EquipmentBase) == type) continue;
                SharedBase? shared;
                try
                {
                    shared = Activator.CreateInstance(type) as SharedBase;
                }
                catch (MissingMethodException)
                {
                    continue;
                }

                if (harmony != null)
                {
                    shared!.Harmony = harmony;
                    shared.PatchProcessor = new PatchClassProcessor(harmony, shared.GetType());
                }

                shared!.Logger = manualLogSource;
                shared.configFile = configFile;
                shared.expandedTooltips = expandedTooltips;
                shared.descInPickup = descInPickup;
                shared.MakeConfigs();
                shared._tokenPrefix = tokenPrefix;
                if (!shared.Enabled?.Value ?? false) continue;
                shared.MakeBehaviours();
                shared.PatchProcessor?.Patch();
                localInstances.Add(shared);
            }
            Instances.AddRange(localInstances);

            if (!serializableContentPack) return;
            serializableContentPack!.itemDefs = serializableContentPack.itemDefs
                .Where(x => Instances.FirstOrDefault(y => MatchName(x.name, y.GetType().Name))?.Enabled?.Value ?? false).ToArray();
            serializableContentPack.equipmentDefs = serializableContentPack.equipmentDefs
                .Where(x => Instances.FirstOrDefault(y => MatchName(x.name, y.GetType().Name))?.Enabled?.Value ?? false).ToArray();
            foreach (var instance in localInstances) instance.FillDefsFromSerializableCP(serializableContentPack);
        }

        [SystemInitializer(typeof(EquipmentCatalog), typeof(PickupCatalog))]
        public static void InitializePickups()
        {
            foreach (var instance in Instances)
            {
                instance.FillDefsFromContentPack();
            }
            foreach (var instance in Instances)
            {
                instance.FillPickupIndex();
            }
        }

        protected static bool MatchName(string scriptableObject, string sharedBase)
        {
            return scriptableObject.EndsWith(sharedBase);
        }

        public static void AddContentPack(ContentPack contentPack)
        {
            if (!ContentPacks.Contains(contentPack))
                ContentPacks.Add(contentPack);
        }
        
        ~SharedBase()
        {
            DestroyBehaviours();
        }
        
        public void CheatForItem()
        {
            var master = PlayerCharacterMasterController.instances[0].master;
            PickupDropletController.CreatePickupDroplet(PickupIndex, master.GetBody().corePosition + Vector3.up * 1.5f, Vector3.one * 25f);
            //master.inventory.GiveItem(ItemDef.itemIndex);
        }

        [SystemInitializer(typeof(ExpansionCatalog))]
        public static void FillAllExpansionDefs()
        {
            foreach (var instance in Instances)
            {
                instance.FillRequiredExpansions();
            }
        }

        [SystemInitializer(typeof(BodyCatalog))]
        public static void FillIDRS()
        {
            foreach (var instance in Instances)
            {
                instance.FillItemDisplayRules();
            }
        }

        protected virtual void FillItemDisplayRules(){}

        [SystemInitializer( typeof(ItemCatalog), typeof(EquipmentCatalog))]
        public static void MakeAllTokens()
        {
            foreach (var item in Instances)
            {
                try
                {
                    item.Logger?.LogMessage($"Making tokens for {item}.");
                    item.MakeTokens();
                }
                catch (Exception e)
                {
                    item.Logger?.LogError(e);
                }
            }
        }

        protected void AddToken(string key, string value)
        {
            Language.english.SetStringByToken(_tokenPrefix + key, value);
        }
        
        /* other languages get unloaded on language change, and these keys would be discarded
        protected void AddToken(string language, string key, string value)
        {
            Language.languagesByName[language].SetStringByToken(_tokenPrefix + key, value);
        }*/
        protected virtual void FillDefsFromSerializableCP(SerializableContentPack serializableContentPack) {}
        protected abstract void FillDefsFromContentPack();
        protected abstract void FillPickupIndex();
        protected abstract void FillRequiredExpansions();
        public abstract void AddDisplayRules(VanillaCharacterIDRS which, ItemDisplayRule[] displayRules);
    }
}