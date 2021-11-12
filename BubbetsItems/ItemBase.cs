using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using NCalc;
using RoR2;
using UnityEngine;

namespace BubbetsItems
{
    public class ItemBase : SharedBase
    {
        //protected virtual void MakeTokens(){} // Description is supposed to have % and + per item, pickup is a brief message about what the item does
        
        protected override void MakeConfigs(ConfigFile configFile)
        {
            var name = GetType().Name;
            Enabled = configFile.Bind("Disable Items", name, true, "Should this item be enabled.");
            
            if (defaultScalingFunction == null) return;
            scaleConfig = configFile.Bind("Balancing Functions", name, defaultScalingFunction, "Scaling function for item. ;" + (!string.IsNullOrEmpty(defaultScalingDesc) ? defaultScalingDesc: "[a] = amount"));
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

        public override string GetFormattedDescription(Inventory inventory = null)
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
        
        [SystemInitializer(typeof(ItemCatalog))]
        public static void AssignAllItemDefs()
        {
            var items = Instances.OfType<ItemBase>().ToList();
            foreach (var pack in ContentPacks)
            {
                foreach (var itemDef in pack.itemDefs)
                {
                    foreach (var item in items.Where(item => itemDef.name == item.GetType().Name))
                    {
                        item.ItemDef = itemDef;
                    }
                }
            }

            foreach (var x in items)
            {
                try
                {
                    PickupIndexes.Add(PickupCatalog.FindPickupIndex(x.ItemDef.itemIndex), x);
                }
                catch (NullReferenceException e)
                {
                    x.Logger.LogError("Item " + x.GetType().Name + " threw a NRE when filling pickup indexes, this could mean its not defined in your content pack:\n" + e);
                }
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