using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using RoR2;
using RoR2.ContentManagement;

namespace BubbetsItems
{
    public class SharedBase
    {
        protected virtual void MakeConfigs(ConfigFile configFile) {}
        protected virtual void MakeTokens(){}
        protected virtual void MakeBehaviours(){} 
        protected virtual void DestroyBehaviours(){}

        //public virtual void MakeInLobbyConfig(ModConfigEntry modConfigEntry){}
        public virtual void MakeInLobbyConfig(object modConfigEntry){}

        public ConfigEntry<bool> Enabled;
        public static readonly List<SharedBase> Instances = new List<SharedBase>();
        public static readonly Dictionary<PickupIndex, SharedBase> PickupIndexes = new Dictionary<PickupIndex, SharedBase>();

        protected ManualLogSource Logger;
        protected Harmony Harmony;
        protected static readonly List<ContentPack> ContentPacks = new List<ContentPack>();
        private string _tokenPrefix;
        
        public virtual string GetFormattedDescription(Inventory inventory = null)
        {
            return "Not Implemented";
        }
        
        public static void Initialize(ManualLogSource manualLogSource, ConfigFile configFile, SerializableContentPack serializableContentPack = null, Harmony harmony = null, string tokenPrefix = "")
        {
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                if (!typeof(SharedBase).IsAssignableFrom(type) || typeof(SharedBase) == type || typeof(ItemBase) == type || typeof(EquipmentBase) == type) continue;
                var shared = (SharedBase) Activator.CreateInstance(type);
                if (harmony != null)
                    shared.Harmony = harmony;
                shared.Logger = manualLogSource;
                shared.MakeConfigs(configFile);
                shared._tokenPrefix = tokenPrefix;
                if (!shared.Enabled.Value) continue;
                shared.MakeBehaviours();
                Instances.Add(shared);
            }

            if (!serializableContentPack) return;
            serializableContentPack.itemDefs = serializableContentPack.itemDefs
                .Where(x => Instances.Any(y => x.name == y.GetType().Name)).ToArray();
            serializableContentPack.equipmentDefs = serializableContentPack.equipmentDefs
                .Where(x => Instances.Any(y => x.name == y.GetType().Name)).ToArray();
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
        
        [SystemInitializer(typeof(Language))]
        public static void MakeAllTokens()
        {
            foreach (var item in Instances)
            {
                item.MakeTokens();
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