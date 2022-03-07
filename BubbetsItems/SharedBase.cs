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
        protected virtual void MakeConfigs(ConfigFile configFile) {}
        protected virtual void MakeTokens(){}
        protected virtual void MakeBehaviours(){} 
        protected virtual void DestroyBehaviours(){}

        //public virtual void MakeInLobbyConfig(ModConfigEntry modConfigEntry){}
        public virtual void MakeInLobbyConfig(object modConfigEntry){}

        public ConfigEntry<bool>? Enabled;
        public static readonly List<SharedBase> Instances = new List<SharedBase>();
        public static readonly Dictionary<PickupIndex, SharedBase> PickupIndexes = new Dictionary<PickupIndex, SharedBase>();
        public PickupIndex PickupIndex;

        protected ManualLogSource? Logger;
        protected Harmony? Harmony;
        protected static readonly List<ContentPack> ContentPacks = new List<ContentPack>();
        private string? _tokenPrefix;
        
        private static ExpansionDef? _sotvExpansion;
        public static ExpansionDef? SotvExpansion
        {
            get
            {
                if (_sotvExpansion == null)
                    _sotvExpansion = ExpansionCatalog.expansionDefs.FirstOrDefault(x => x.nameToken == "DLC1_NAME");
                return _sotvExpansion;
            }
        }
        public virtual bool RequiresSOTV { get; protected set; } = false;
        
        public virtual string GetFormattedDescription(Inventory? inventory)
        {
            return "Not Implemented";
        }
        
        public static void Initialize(ManualLogSource manualLogSource, ConfigFile configFile, SerializableContentPack? serializableContentPack = null, Harmony? harmony = null, string tokenPrefix = "")
        {
            var localInstances = new List<SharedBase>();
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
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
                    shared!.Harmony = harmony;
                shared!.Logger = manualLogSource;
                shared.MakeConfigs(configFile);
                shared._tokenPrefix = tokenPrefix;
                if (!shared.Enabled.Value) continue;
                shared.MakeBehaviours();
                localInstances.Add(shared);
            }
            Instances.AddRange(localInstances);

            if (!serializableContentPack) return;
            serializableContentPack!.itemDefs = serializableContentPack.itemDefs
                .Where(x => Instances.FirstOrDefault(y => MatchName(x.name, y.GetType().Name))?.Enabled.Value ?? false).ToArray();
            serializableContentPack.equipmentDefs = serializableContentPack.equipmentDefs
                .Where(x => Instances.FirstOrDefault(y => MatchName(x.name, y.GetType().Name))?.Enabled.Value ?? false).ToArray();
            foreach (var instance in localInstances) instance.FillDefs(serializableContentPack);
        }

        protected virtual void FillDefs(SerializableContentPack serializableContentPack) {}

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
        
        [SystemInitializer( typeof(ItemCatalog), typeof(EquipmentCatalog))]
        public static void MakeAllTokens()
        {
            foreach (var item in Instances)
            {
                try
                {
                    item.Logger.LogMessage($"Making tokens for {item}.");
                    item.MakeTokens();
                }
                catch (Exception e)
                {
                    item.Logger.LogError(e);
                }
            }
        }

        protected void AddToken(string key, string value)
        {
            Language.english.SetStringByToken(_tokenPrefix + key, value);
        }
        
        protected void AddToken(string language, string key, string value)
        {
            Language.languagesByName[language].SetStringByToken(_tokenPrefix + key, value);
        }
    }
}