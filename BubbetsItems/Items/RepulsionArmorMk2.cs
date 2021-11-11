using System.Linq;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
using InLobbyConfig;
using InLobbyConfig.Fields;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NCalc;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Items
{
    public class RepulsionArmorMk2 : ItemBase
    {
        private static RepulsionArmorMk2 _instance;
        private static ConfigEntry<bool> _reductionOnTrue;
        private static ConfigEntry<string> _reductionScalingConfig;
        private static ConfigEntry<string> _armorScalingConfig;

        protected override void MakeConfigs(ConfigFile configFile)
        {
            base.MakeConfigs(configFile);
            _reductionOnTrue = configFile.Bind("General", "Reduction On True", true,  "Makes the item behave more like mk1 and give a flat reduction in damage taken if set to true.");
            _instance = this;
            var name = GetType().Name;
            _reductionScalingConfig = configFile.Bind("Balancing Functions", name + " Reduction", "[d] - (20 + [p] * (4 + [a]))", "Scaling function for item. [a] = amount, [p] = plate amount, [d] = damage");
            _armorScalingConfig = configFile.Bind("Balancing Functions", name + " Armor", "20 + [p] * (4 + [a])", "Scaling function for item. [a] = amount, [p] = plate amount");
            UpdateScalingFunction();
        }

        
        public override void MakeInLobbyConfig(object modConfigEntryObj)
        {
            base.MakeInLobbyConfig(modConfigEntryObj);
            var modConfigEntry = (ModConfigEntry) modConfigEntryObj;
            var list = modConfigEntry.SectionFields["Scaling Functions"].ToList();
            var reduction = new StringConfigField(_reductionScalingConfig.Definition.Key, () => _reductionScalingConfig.Value, newValue => {
                    try
                    {
                        _reductionScalingConfig.Value = newValue;
                        UpdateScalingFunction();
                    } catch (EvaluationException) { }
            });
            
            var armor = new StringConfigField(_armorScalingConfig.Definition.Key, () => _armorScalingConfig.Value, newValue => {
                    try
                    {
                        _armorScalingConfig.Value = newValue;
                        UpdateScalingFunction();
                    } catch (EvaluationException) { } 
            });
            var toggle = new BooleanConfigField(_reductionOnTrue.Definition.Key, () => _reductionOnTrue.Value, newValue => {
                    try
                    {
                        _reductionOnTrue.Value = newValue;
                        UpdateScalingFunction();
                    } catch (EvaluationException) { }
            });
            list.Add(reduction);
            list.Add(armor);
            list.Add(toggle);
            modConfigEntry.SectionFields["Scaling Functions"] = list;
        }

        public void UpdateScalingFunction()
        {
            scalingFunction = _reductionOnTrue.Value ? new Expression(_reductionScalingConfig.Value).ToLambda<ExpressionContext, float>() : new Expression(_armorScalingConfig.Value).ToLambda<ExpressionContext, float>();
        }

        public override float GraphScalingFunction(int itemCount)
        {
            return _reductionOnTrue.Value ? -ScalingFunction(itemCount,1) : ScalingFunction(itemCount);
        }

        public override string GetFormattedDescription(Inventory inventory = null)
        {
            var amount = inventory?.GetItemCount(ItemDef) ?? 0;
            var plate = inventory?.GetItemCount(RoR2Content.Items.ArmorPlate) ?? 0;
            if (_reductionOnTrue.Value)
            {
                if (amount == 0)
                    return Language.GetStringFormatted("BUB_REPULSION_ARMOR_MK2_DESC_REDUCTION", _reductionScalingConfig.Value, -ScalingFunction(1, plate, 0));
                return Language.GetStringFormatted("BUB_REPULSION_ARMOR_MK2_DESC_REDUCTION", _reductionScalingConfig.Value, -ScalingFunction(amount, plate));
            }
            return Language.GetStringFormatted("BUB_REPULSION_ARMOR_MK2_DESC_ARMOR", _armorScalingConfig.Value, ScalingFunction(amount, plate));
        }

        public override float ScalingFunction(int itemCount)
        {
            return ScalingFunction(itemCount, 0, 0);
        }
        
        public float ScalingFunction(int itemCount, int plateAmount) // Armor based // 20 + [p] * (4 + [a])
        {
            return ScalingFunction(itemCount, plateAmount, 0);
        }
        
        public float ScalingFunction(int itemCount, int plateAmount, float damage) // Damage based //Max(1f, [d] - (20 + [p] * (4 + [a])))
        {
            return scalingFunction(new ExpressionContext{a = itemCount, p = plateAmount, d = damage});
        }

        protected override void MakeTokens()
        {
            base.MakeTokens();
            AddToken("REPULSION_ARMOR_MK2_NAME", "Repulsion Armor Plate Mk2");
            //AddToken("REPULSION_ARMOR_MK2_DESC", "Placeholder, swapped out with config value at runtime."); //pickup);

            AddToken("REPULSION_ARMOR_MK2_DESC_REDUCTION",
                $"Reduce all {"incoming damage".Style(StyleEnum.Damage)} by {"{1}".Style(StyleEnum.Damage)}. Cannot be reduced below {"1".Style(StyleEnum.Damage)}. Scales with the amount of {"Mk1 plates".Style(StyleEnum.Utility)} you have.\n{{0}}"); //"Reduce all <style=cIsDamage>incoming damage</style> by <style=cIsDamage>{1}</style>. Cannot be reduced below 1. Scales with the amount of Mk1 plates your have.\n{0}");//"Reduce all <style=cIsDamage>incoming damage</style> by <style=cIsDamage>20</style> and by <style=cIsDamage>5<style=cStack> (+1 per stack of Mk2)</style></style> for every Mk1 plate. Cannot be reduced below <style=cIsDamage>1</style>.");
            AddToken("REPULSION_ARMOR_MK2_DESC_ARMOR", $"Grant {"{1}".Style(StyleEnum.Heal)} armor. Scales with the amount of {"Mk1 plates".Style(StyleEnum.Utility)} you have.\n{{0}}");//"Grant <style=cIsDamage>{1}</style> armor. Scales with the amount of Mk1 plates your have.\n{0}");//"Grant <style=cIsDamage>20</style> armor plus an additional <style=cIsDamage>5<style=cStack> (+1 per stack of Mk2)</style></style> for every Mk1 plate.");
            // <style=cIsDamage>incoming damage</style> by <style=cIsDamage>5<style=cStack> (+5 per stack)</style></style>
            AddToken("REPULSION_ARMOR_MK2_PICKUP", "Reduce incoming damage for each mk1 plate.");
            AddToken("REPULSION_ARMOR_MK2_LORE", @"Order: Experimental Repulsion Armour Augments - Mk. 2
Tracking number: 07 **
Estimated Delivery: 10/23/2058
Shipping Method: Secure, High Priority
Shipping Address: System Police Station 13/ Port of Marv, Ganymede
Shipping Details:

The order contains cutting-edge experimental technology aimed at reducing risk of harm for the users even in the most harsh of conditions. On top of providing protection Mk. 2's smart nano-bot network enhances already existing protection that the user has installed. This kind of equipment might prove highly necessary as crime rates had seen a rise in the Port of Marv area around station 13, higher risk of injury for stationing officers necessitates an increase in measures used to ensure their safety.

The cost of purchase and production associated with Mk2 is considerably higher than that of its prior iterations, however the considerable step-up in efficiency covers for the costs, as drastic as they might be.");
        }
        

        [HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
        private static void RecalcStats(CharacterBody __instance)
        {
            if (!_instance.Enabled.Value) return;
            if (_reductionOnTrue.Value) return;
            var inv = __instance.inventory;
            if (!inv) return;
            var amount = inv.GetItemCount(_instance.ItemDef);
            if (amount > 0)
            {
                var plateAmount = inv.GetItemCount(RoR2Content.Items.ArmorPlate);
                // 20 + inv.GetItemCount(RoR2Content.Items.ArmorPlate) * (4 + amount);
                __instance.armor += _instance.ScalingFunction(amount, plateAmount);
            }
        }
        
        private static bool DoMk2ArmorPlates(HealthComponent hc, ref float damage)
        {
            if (hc == null) return false;
            if (hc.body == null) return false;
            if (hc.body.inventory == null) return false;
            var amount = hc.body.inventory.GetItemCount(_instance.ItemDef);
            if (amount <= 0) return false;
            var plateAmount = hc.body.inventory.GetItemCount(RoR2Content.Items.ArmorPlate);
            //damage = Mathf.Max(1f, damage - (20 + plateAmount * (4 + amount)));
            damage = Mathf.Max(1f, _instance.ScalingFunction(amount, plateAmount, damage));
            return true;
        }

        private delegate bool ArmorPlateDele(HealthComponent hc, ref float damage);

        [HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
        public static void TakeDamageHook(ILContext il)
        {
            if (!_instance.Enabled.Value) return;
	        if (!_reductionOnTrue.Value) return;
            var c = new ILCursor(il);
            ILLabel jumpInstruction = null;
            int damageNum = -1;
            c.GotoNext(
                x => x.MatchLdcR4(out _),
                x => x.MatchLdloc(out damageNum),
                x => x.MatchLdcR4(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<HealthComponent>("itemCounts"),
                x => x.OpCode == OpCodes.Ldfld && ((FieldReference) x.Operand).Name == "armorPlate"
            );
            c.GotoPrev(
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<HealthComponent>("itemCounts"),
                x => x.OpCode == OpCodes.Ldfld && ((FieldReference) x.Operand).Name == "armorPlate",
                x => x.MatchLdcI4(0),
                x => x.MatchBle(out jumpInstruction)
            );
            if (damageNum == -1 || jumpInstruction == null) return;
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca, damageNum);
            c.EmitDelegate<ArmorPlateDele>(DoMk2ArmorPlates);
            c.Emit(OpCodes.Brfalse, jumpInstruction.Target);
        }
    }
}