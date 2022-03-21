using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using InLobbyConfig.Fields;
using JetBrains.Annotations;
using NCalc;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using RoR2.Items;
using UnityEngine;

#nullable enable

namespace BubbetsItems
{
    [HarmonyPatch]
    public abstract class ItemBase : SharedBase
    {
        //protected virtual void MakeTokens(){} // Description is supposed to have % and + per item, pickup is a brief message about what the item does
        
        protected override void MakeConfigs()
        {
            var name = GetType().Name;
            Enabled = configFile.Bind("Disable Items", name, true, "Should this item be enabled.");
        }

        public ItemDef ItemDef;

        private static IEnumerable<ItemBase> _items;
        public static IEnumerable<ItemBase> Items => _items ??= Instances.OfType<ItemBase>();

        public List<ScalingInfo> scalingInfos = new();
        
        protected void AddScalingFunction(string defaultValue, string name, ExpressionContext? defaultContext = null, string? desc = null)
        {
            scalingInfos.Add(new ScalingInfo(configFile, defaultValue, name, defaultContext, desc));
        }

        public override string GetFormattedDescription([CanBeNull] Inventory inventory)
        {
            // ReSharper disable twice Unity.NoNullPropagation

            if (scalingInfos.Count <= 0) return Language.GetString(ItemDef.descriptionToken);
            
            var formatArgs = scalingInfos.Select(info => info.ScalingFunction()).Cast<object>().ToArray();
            var ret = Language.GetStringFormatted(ItemDef.descriptionToken, formatArgs);
            ret += "\n\n" + string.Join("\n", scalingInfos.Select(info => info.ToString()));
            return ret;
        }

        public override void MakeInLobbyConfig(Dictionary<ConfigCategoriesEnum, List<object>> scalingFunctions)
        {
            base.MakeInLobbyConfig(scalingFunctions);
            foreach (var info in scalingInfos)
            {
                info.MakeInLobbyConfig(scalingFunctions[ConfigCategoriesEnum.BalancingFunctions]);
            }
        }

        protected override void FillDefs(SerializableContentPack serializableContentPack)
        {
            base.FillDefs(serializableContentPack);
            var name = GetType().Name;
            foreach (var itemDef in serializableContentPack.itemDefs)
            {
                if (MatchName(itemDef.name, name)) ItemDef = itemDef;
            }
            if (ItemDef == null)
            {
                Logger?.LogWarning($"Could not find ItemDef for item {this} in serializableContentPack, class/itemdef name are probably mismatched. This will throw an exception later.");
            }
        }

        [SystemInitializer(typeof(ItemCatalog), typeof(PickupCatalog))]
        public static void GetPickupIndexes()
        {
            try
            {
                var items = Instances.OfType<ItemBase>().Where(x => x.Enabled?.Value ?? false).ToArray();
                foreach (var pack in ContentPacks)
                {
                    foreach (var itemBase in items)
                    {
                        if (itemBase.ItemDef != null) continue;
                        var name = itemBase.GetType().Name;
                        foreach (var itemDef in pack.itemDefs)
                            if (MatchName(itemDef.name, name))
                                itemBase.ItemDef = itemDef;
                        if (itemBase.ItemDef == null)
                        {
                            itemBase.Logger?.LogWarning($"Could not find ItemDef for item {itemBase}, class/itemdef name are probably mismatched. This will throw an exception later.");
                        }
                    }
                }
                
                foreach (var x in items)
                {
                    try
                    {
                        var pickup = PickupCatalog.FindPickupIndex(x.ItemDef.itemIndex);
                        x.PickupIndex = pickup;
                        PickupIndexes.Add(pickup, x);
                    }
                    catch (NullReferenceException e)
                    {
                        x.Logger?.LogError("Item " + x.GetType().Name +
                                           " threw a NRE when filling pickup indexes, this could mean its not defined in your content pack:\n" +
                                           e);
                    }
                }
            }
            catch (Exception e)
            {
                BubbetsItemsPlugin.Log.LogError(e);
            }
        }
        
        
        [SystemInitializer(typeof(ExpansionCatalog))]
        public static void FillRequiredExpansions()
        {
            foreach (var itemBase in Items)
            {
                try
                {
                    if (itemBase.RequiresSotv)
                        itemBase.ItemDef.requiredExpansion =
                            ExpansionCatalog.expansionDefs.FirstOrDefault(x => x.nameToken == "DLC1_NAME");
                }
                catch (Exception e)
                {
                    itemBase.Logger?.LogError(e);
                }
            }
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(ContagiousItemManager), nameof(ContagiousItemManager.Init))]
        public static void FillVoidItems()
        {
            var pairs = new List<ItemDef.Pair>();
            foreach (var itemBase in Items)
            {
                itemBase.FillVoidConversions(pairs);
            }

            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog
                .itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddRangeToArray(pairs.ToArray());
        }

        protected virtual void FillVoidConversions(List<ItemDef.Pair> pairs) {}


        public class ScalingInfo
        {
            private readonly string _description;
            private readonly ConfigEntry<string> _configEntry;
            private Func<ExpressionContext, float> _function;
            private string _oldValue;
            private readonly string _name;
            private readonly ExpressionContext _defaultContext;
            public readonly ExpressionContext WorkingContext;

            public ScalingInfo(ConfigFile configFile, string defaultValue, string name, ExpressionContext? defaultContext, string? desc)
            {
                _description = desc ?? "[a] = item count";
                _name = name;
                _defaultContext = defaultContext ?? new ExpressionContext();
                _defaultContext.a = 1f;
                WorkingContext = new ExpressionContext();
                
                _configEntry = configFile.Bind(ConfigCategoriesEnum.BalancingFunctions, name, defaultValue, "Scaling function for item. ;" + _description);
                _oldValue = _configEntry.Value;
                _function = new Expression(_oldValue).ToLambda<ExpressionContext, float>();
                _configEntry.SettingChanged += EntryChanged;
            }

            public float ScalingFunction(ExpressionContext? context = null)
            {
                return _function(context ?? _defaultContext);
            }
            public float ScalingFunction(int itemCount)
            {
                WorkingContext.a = itemCount;
                return ScalingFunction(WorkingContext);
            }

            public override string ToString()
            {
                return _oldValue + "\n(" + _name + ": " + _description + ")";
            }

            public void MakeInLobbyConfig(List<object> modConfigEntryObj)
            {
                modConfigEntryObj.Add(ConfigFieldUtilities.CreateFromBepInExConfigEntry(_configEntry));
            }

            private void EntryChanged(object sender, EventArgs e)
            {
                if (_configEntry.Value == _oldValue) return;
                _function = new Expression(_configEntry.Value).ToLambda<ExpressionContext, float>();
                _oldValue = _configEntry.Value;
            }
        }

        public class ExpressionContext
        {
            // yes this is terrible but im not smart enough to figure out another way.
            public float a;
            public float b;
            public float c;
            public float d;
            public float e;
            public float f;
            public float g;
            public float h;
            public float i;
            public float j;
            public float k;
            public float l;
            public float m;
            public float n;
            public float o;
            public float p;
            public float q;
            public float r;
            public float s;
            public float t;
            public float u;
            public float v;
            public float w;
            public float x;
            public float y;
            public float z;

            public int RoundToInt(float x)
            {
                return Mathf.RoundToInt(x);
            }

            public float Log(float x)
            {
                return Mathf.Log(x);
            }

            public float Max(float x, float y)
            {
                return Mathf.Max(x, y);
            }

            public float Min(float x, float y)
            {
                return Mathf.Min(x, y);
            }
        }
    }
}