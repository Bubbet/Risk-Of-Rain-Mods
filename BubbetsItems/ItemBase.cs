using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using NCalc;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using RoR2.Items;
using UnityEngine;

namespace BubbetsItems
{
    [HarmonyPatch]
    public abstract class ItemBase : SharedBase
    {
        //protected virtual void MakeTokens(){} // Description is supposed to have % and + per item, pickup is a brief message about what the item does
        
        protected override void MakeConfigs(ConfigFile configFile)
        {
            var name = GetType().Name;
            Enabled = configFile.Bind("Disable Items", name, true, "Should this item be enabled.");
            
            if (defaultScalingFunction == null) return;
            scaleConfig = configFile.Bind(ConfigCategoriesEnum.BalancingFunctions, name, defaultScalingFunction, "Scaling function for item. ;" + (!string.IsNullOrEmpty(defaultScalingDesc) ? defaultScalingDesc: "[a] = amount"));
            scalingFunction = new Expression(scaleConfig.Value).ToLambda<ExpressionContext, float>();
        }

        public ItemDef ItemDef;

        private static IEnumerable<ItemBase> _items;
        public static IEnumerable<ItemBase> Items => _items ?? (_items = Instances.OfType<ItemBase>());

        public string defaultScalingFunction;
        public Func<ExpressionContext, float> scalingFunction;
        public ConfigEntry<string> scaleConfig;
        protected string defaultScalingDesc;

        public virtual float ScalingFunction(int itemCount)
        {
            return scalingFunction(new ExpressionContext{ a = itemCount });
        }

        public virtual float GraphScalingFunction(int itemCount)
        {
            return ScalingFunction(itemCount);
        }

        public override string GetFormattedDescription([CanBeNull] Inventory inventory)
        {
            // ReSharper disable once Unity.NoNullPropagation
            if (scalingFunction != null)
            {
                var amount = inventory?.GetItemCount(ItemDef) ?? 0;
                return Language.GetStringFormatted(ItemDef.descriptionToken,  "\n\n" + scaleConfig.Value + "\n" + scaleConfig.Description.Description.Split(';')[1],
                    amount > 0 ? ScalingFunction(amount) : ScalingFunction(1));
            }

            return Language.GetString(ItemDef.descriptionToken);
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