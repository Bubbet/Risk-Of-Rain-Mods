using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
using InLobbyConfig.Fields;
using NCalc;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Items;
using UnityEngine;
using Random = System.Random;

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
            Enabled = sharedInfo.ConfigFile.Bind("Disable Items", name, true, "Should this item be enabled.");
        }

        public ItemDef ItemDef;

        private static IEnumerable<ItemBase>? _items;
        public static IEnumerable<ItemBase> Items => _items ??= Instances.OfType<ItemBase>();

        public List<ScalingInfo> scalingInfos = new();
        public VoidPairing? voidPairing;
        protected string SimpleDescriptionToken;

        protected void AddScalingFunction(string defaultValue, string name, string? desc = null, string? oldDefault = null)
        {
            scalingInfos.Add(new ScalingInfo(sharedInfo.ConfigFile, defaultValue, name, new StackFrame(1).GetMethod().DeclaringType, desc, oldDefault));
        }

        private static Regex formatArgParams = new Regex(@"({\d:?.*?})+", RegexOptions.Compiled);
        public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
        {
            // ReSharper disable twice Unity.NoNullPropagation

            if (scalingInfos.Count <= 0) return Language.GetString(ItemDef.descriptionToken);

            var formatArgs = scalingInfos.Select(info => info.ScalingFunction(inventory?.GetItemCount(ItemDef)))
                .Cast<object>().ToArray();

            var corruption = "";
            if (voidPairing != null)
            {
                if (!voidPairing.IsDefault)
                {
                    corruption = "Corrupts all " + string.Join(", ", voidPairing.itemDefs.Select(x =>
                    {
                        var str = Language.GetString(x.nameToken);
                        if (Language.currentLanguage == Language.english)
                            str += "s";
                        return str;
                    })) + ".";
                    corruption = corruption.Style(StyleEnum.Void);
                }
                else
                {
                    corruption = Language.GetString(ItemDef.nameToken.Sub(0, -"_NAME".Length) + "_CONVERT");
                }
            }

            if (sharedInfo.UseSimpleDescIfApplicable.Value && scalingInfos.All(x => x.IsDefault) && !string.IsNullOrEmpty(SimpleDescriptionToken))
            {
                var ret = Language.GetString(sharedInfo.TokenPrefix + SimpleDescriptionToken);
                if (sharedInfo.ItemStatsInSimpleDesc.Value && !forceHideExtended)
                {
                    var para = new List<string>();

                    ret += corruption;
                    
                    ret += "\n";
                    
                    // Holy fuck i hate regex in c#
                    var match = formatArgParams.Matches(Language.GetString(token ?? ItemDef.descriptionToken));
                    foreach (Match matchGroupCapture in match)
                    {
                        var val = matchGroupCapture.Value;
                        if (!string.IsNullOrEmpty(val))
                            para.Add(val);
                    }
                    

                    para = para.OrderBy(x => x[1]).ToList();

                    foreach (var param in para)
                    {
                        if (int.TryParse(param[1].ToString(), out var i))
                            ret += "\n" + scalingInfos[i]._name + ": " + param;
                    }

                    ret = string.Format(ret, formatArgs);
                }

                return ret;
            }
            else
            {
                var ret = Language.GetStringFormatted(token ?? ItemDef.descriptionToken, formatArgs) + corruption;
                if (sharedInfo.ExpandedTooltips.Value && !forceHideExtended)
                    ret += "\n\nTooltip updates automatically with these fully configurable Scaling Functions:\n" +
                           string.Join("\n", scalingInfos.Select(info => info.ToString()));
                return ret;
            }
        }

        public override void MakeInLobbyConfig(Dictionary<ConfigCategoriesEnum, List<object>> scalingFunctions)
        {
            base.MakeInLobbyConfig(scalingFunctions);
            foreach (var info in scalingInfos)
            {
                info.MakeInLobbyConfig(scalingFunctions[ConfigCategoriesEnum.BalancingFunctions]);
            }
        }
        
        public override void MakeRiskOfOptions()
        {
            base.MakeRiskOfOptions();
            foreach (var info in scalingInfos)
            {
                info.MakeRiskOfOptions();
            }
        }

        protected override void FillDefsFromSerializableCP(SerializableContentPack serializableContentPack)
        {
            base.FillDefsFromSerializableCP(serializableContentPack);
            var name = GetType().Name;
            foreach (var itemDef in serializableContentPack.itemDefs)
            {
                if (MatchName(itemDef.name, name)) ItemDef = itemDef;
            }
            if (ItemDef == null)
            {
                sharedInfo.Logger?.LogWarning($"Could not find ItemDef for item {this} in serializableContentPack, class/itemdef name are probably mismatched. This will throw an exception later.");
            }
        }
        
        public override void AddDisplayRules(VanillaIDRS which, ItemDisplayRule[] displayRules)
        {
            var set = IDRHelper.GetRuleSet(which);
            var rules = set?.keyAssetRuleGroups;
            if (rules is null) return;
            rules = rules.AddItem(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                displayRuleGroup = new DisplayRuleGroup {rules = displayRules},
                keyAsset = ItemDef
            }).ToArray();
            set!.keyAssetRuleGroups = rules;
        }
        public override void AddDisplayRules(ModdedIDRS which, ItemDisplayRule[] displayRules)
        {
            var set = IDRHelper.GetRuleSet(which);
            var rules = set?.keyAssetRuleGroups;
            if (rules is null) return;
            rules = rules.AddItem(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                displayRuleGroup = new DisplayRuleGroup {rules = displayRules},
                keyAsset = ItemDef
            }).ToArray();
            set!.keyAssetRuleGroups = rules;
        }
        

        protected override void FillDefsFromContentPack()
        {
            foreach (var pack in ContentPacks)
            {
                if (ItemDef != null) continue;
                var name = GetType().Name;
                foreach (var itemDef in pack.itemDefs)
                    if (MatchName(itemDef.name, name))
                        ItemDef = itemDef;
            }

            if (ItemDef == null) 
                sharedInfo.Logger?.LogWarning(
                    $"Could not find ItemDef for item {this}, class/itemdef name are probably mismatched. This will throw an exception later.");
        }

        protected override void FillPickupIndex()
        {
            try
            {
                var pickup = PickupCatalog.FindPickupIndex(ItemDef.itemIndex);
                PickupIndex = pickup;
                PickupIndexes.Add(pickup, this);
            }
            catch (NullReferenceException e)
            {
                sharedInfo.Logger?.LogError("Equipment " + GetType().Name +
                                            " threw a NRE when filling pickup indexes, this could mean its not defined in your content pack:\n" +
                                            e);
            }
        }

        public override bool RequiresSotv => voidPairing != null;
        protected override void FillRequiredExpansions()
        {
            var pairs = new List<ItemDef.Pair>();
            FillVoidConversions(pairs);

            if (RequiresSotv)
                ItemDef.requiredExpansion = sharedInfo.SotVExpansion ? sharedInfo.SotVExpansion : SotvExpansion;
            else
                ItemDef.requiredExpansion = sharedInfo.Expansion;
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(ContagiousItemManager), nameof(ContagiousItemManager.Init))]
        public static void FillVoidItems()
        {
            var pairs = new List<ItemDef.Pair>();
            /*
            foreach (var itemBase in Items)
            {
                itemBase.FillVoidConversions(pairs);
            }
             */
            
            foreach (var itemBase in Items)
            {
                itemBase.voidPairing?.SettingChanged();
            }
            
            // ContagiousItemManager.originalToTransformed
            // ContagiousItemManager.itemsToCheck
            // ArrayUtils.ArrayAppend<ContagiousItemManager.TransformationInfo>(ref ContagiousItemManager._transformationInfos, transformationInfo);
            
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog
                .itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddRangeToArray(pairs.ToArray());
        }

        protected virtual void FillVoidConversions(List<ItemDef.Pair> pairs) {}


        public static void CheatForAllItems()
        {
            foreach (var itemBase in Items)
            {
                itemBase.CheatForItem(UnityEngine.Random.onUnitSphere);
            }
        }
        
        public class ScalingInfo
        {
            private readonly string _description;
            private readonly ConfigEntry<string> _configEntry;
            private Func<ExpressionContext, float> _function;
            private string _oldValue;
            public readonly string _name;
            private readonly ExpressionContext _defaultContext;
            public readonly ExpressionContext WorkingContext;
            private string _defaultValue;

            public string Value
            {
                get => _configEntry.Value;
                set => _configEntry.Value = value;
            }

            public bool IsDefault => _configEntry.Value.Trim() == _defaultValue.Trim();

            public ScalingInfo(ConfigFile configFile, string defaultValue, string name, Type callingType, string? desc = null, string? oldDefault = null)
            {
                _description = desc ?? "[a] = item count";
                _name = name;
                WorkingContext = new ExpressionContext();

                _configEntry = configFile.Bind(ConfigCategoriesEnum.BalancingFunctions, callingType.Name + "_" + name, defaultValue,   callingType.Name + "; " + _name +"; Scaling function for item. ;" + _description, oldDefault);
                _defaultValue = defaultValue;
                _oldValue = _configEntry.Value;
                _function = new Expression(_oldValue).ToLambda<ExpressionContext, float>();
                _configEntry.SettingChanged += EntryChanged;
            }

            public float ScalingFunction(ExpressionContext? context = null)
            {
                return _function(context ?? _defaultContext);
            }
            public float ScalingFunction(int? itemCount)
            {
                WorkingContext.a = itemCount ?? 1;
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

            public void MakeRiskOfOptions()
            {
                ModSettingsManager.AddOption(new StringInputFieldOption(_configEntry));
            }

            private void EntryChanged(object sender, EventArgs e)
            {
                if (_configEntry.Value == _oldValue) return;
                try
                {
                    _function = new Expression(_configEntry.Value).ToLambda<ExpressionContext, float>();
                    _oldValue = _configEntry.Value;
                } catch(EvaluationException){}
            }
        }

        public class VoidPairing
        {
            public static string ValidEntries = string.Join(" ", ItemCatalog.itemNames);
            private ConfigEntry<string> configEntry;
            private ItemBase Parent;
            public ItemDef[] itemDefs;
            private string _default;
            public bool IsDefault => _default.Trim() == configEntry.Value.Trim();

            public VoidPairing(string defaultValue, ItemBase parent, string? oldDefault = null)
            {
                Parent = parent;
                _default = defaultValue;
                configEntry = parent.sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Void Conversions: " + parent.GetType().Name, defaultValue, "Valid values: " + ValidEntries, oldDefault);
                configEntry.SettingChanged += (_, _) => SettingChanged();
                //SettingChanged();
            }

            public void SettingChanged()
            {
                var pairs = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].Where(x => x.itemDef2 != Parent.ItemDef);
                itemDefs = configEntry.Value.Split(' ').Select(str => ItemCatalog.FindItemIndex(str)).Where(index => index != ItemIndex.None).Select(index => ItemCatalog.GetItemDef(index)).ToArray();
                var newPairs = from def in itemDefs select new ItemDef.Pair {itemDef1 = def, itemDef2 = Parent.ItemDef};
                ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = pairs.Union(newPairs).ToArray();
            }
        }
        
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
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

            public float Epsilon = Mathf.Epsilon;
            public float Infinity = Mathf.Infinity;
            public float Deg2Rad = Mathf.Deg2Rad;
            public float NegativeInfinity = Mathf.NegativeInfinity;
            public float PI = Mathf.PI;
            public float Rad2Deg = Mathf.Rad2Deg;
            public float Abs(float x) => Mathf.Abs(x);
            public float Acos(float x) => Mathf.Acos(x);
            public float Asin(float x) => Mathf.Asin(x);
            public float Atan(float x) => Mathf.Atan(x);
            public float Atan2(float x, float y) => Mathf.Atan2(x, y);
            public float Ceil(float x) => Mathf.Ceil(x);
            public float Clamp(float a, float x, float y) => Mathf.Clamp(a, x, y);
            public float Clamp01(float x) => Mathf.Clamp01(x);
            public float Cos(float x) => Mathf.Cos(x);
            public float Exp(float x) => Mathf.Exp(x);
            public float Floor(float x) => Mathf.Floor(x);
            public float Gamma(float x, float y, float z) => Mathf.Gamma(x, y, z);
            public float Lerp(float x, float y, float z) => Mathf.Lerp(x, y, z);
            public float Log(float x) => Mathf.Log(x);
            public float Log(float x, float y) => Mathf.Log(x, y);
            public float Log10(float x) => Mathf.Log10(x);
            public float Max(float x, float y) => Mathf.Max(x, y);
            public float Min(float x, float y) => Mathf.Min(x, y);
            public float Pow(float x, float y) => Mathf.Pow(x, y);
            public float Repeat(float x, float y) => Mathf.Repeat(x, y);
            public float Round(float x) => Mathf.Round(x);
            public float Sign(float x) => Mathf.Sign(x);
            public float Sin(float x) => Mathf.Sin(x);
            public float Sqrt(float x) => Mathf.Sqrt(x);
            public float Tan(float x) => Mathf.Tan(x);
            public float DeltaAngle(float x, float y) => Mathf.DeltaAngle(x, y);
            public float InverseLerp(float x, float y, float z) => Mathf.InverseLerp(x, y, z);
            public float LerpAngle(float x, float y, float z) => Mathf.LerpAngle(x, y, z);
            public float LerpUnclamped(float x, float y, float z) => Mathf.LerpUnclamped(x, y, z);
            public float MoveTowards(float x, float y, float z) => Mathf.MoveTowards(x, y, z);
            public float PerlinNoise(float x, float y) => Mathf.PerlinNoise(x, y);
            public float PingPong(float x, float y) => Mathf.PingPong(x, y);
            //public float SmoothDamp(float x, float y, ref float z, float w, float m, float d) => Mathf.SmoothDamp(x, y, ref z, w, m, d);
            public float SmoothStep(float x, float y, float z) => Mathf.SmoothStep(x, y, z);
            public float MoveTowardsAngle(float x, float y, float z) => Mathf.MoveTowardsAngle(x, y, z);
            //public float SmoothDampAngle(float x, float y, ref float z, float w, float m) => Mathf.SmoothDampAngle(x, y, ref z, w, m);
            public float GammaToLinearSpace(float x) => Mathf.GammaToLinearSpace(x);
            public float LinearToGammaSpace(float x) => Mathf.LinearToGammaSpace(x);
        }

        public void AddVoidPairing(string defaultValue, string? oldDefault = null)
        {
            voidPairing = new VoidPairing(defaultValue, this, oldDefault);
        }
    }
}