using System.Collections.Generic;
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
    public class RepulsionPlateMk2 : ItemBase
    {
        private static RepulsionPlateMk2 _instance;
        private static ConfigEntry<bool> _reductionOnTrue;
        private static ScalingInfo _reductionScalingConfig;
        private static ScalingInfo _armorScalingConfig;

        protected override void MakeConfigs()
        {
            base.MakeConfigs();
            _reductionOnTrue = configFile.Bind(ConfigCategoriesEnum.General, "Reduction On True", true,  "Makes the item behave more like mk1 and give a flat reduction in damage taken if set to true.");
            _instance = this;
            var name = GetType().Name;;
            AddScalingFunction("[d] - (20 + [p] * (4 + [a]))", name + " Reduction", new ExpressionContext {d = 1, p = 1}, "[a] = amount, [p] = plate amount, [d] = damage");
            AddScalingFunction("20 + [p] * (4 + [a])", name + " Armor", new ExpressionContext {p = 1}, "[a] = amount, [p] = plate amount");
            _reductionScalingConfig = scalingInfos[0];
            _armorScalingConfig = scalingInfos[1];
            
            //_reductionScalingConfig = configFile.Bind(ConfigCategoriesEnum.BalancingFunctions, name + " Reduction", "[d] - (20 + [p] * (4 + [a]))", "Scaling function for item. ;");
            //_armorScalingConfig = configFile.Bind(ConfigCategoriesEnum.BalancingFunctions, name + " Armor", "", "Scaling function for item. ;");
            UpdateScalingFunction();
        }

        private void UpdateScalingFunction()
        {
            scalingInfos.Clear();
            scalingInfos.Add(_reductionOnTrue.Value ? _reductionScalingConfig : _armorScalingConfig);
        }


        public override void MakeInLobbyConfig(Dictionary<ConfigCategoriesEnum, List<object>> scalingFunctions)
        {
            base.MakeInLobbyConfig(scalingFunctions);
            var toggle = new BooleanConfigField(_reductionOnTrue.Definition.Key, () => _reductionOnTrue.Value, newValue => {
                try
                {
                    _reductionOnTrue.Value = newValue;
                    UpdateScalingFunction();
                } catch (EvaluationException) { }
            });
            var list = scalingFunctions[ConfigCategoriesEnum.BalancingFunctions];
            list.Add(toggle);
        }

        /*
        public void UpdateScalingFunction()
        {
            scalingFunction = _reductionOnTrue.Value ? new Expression(_reductionScalingConfig.Value).ToLambda<ExpressionContext, float>() : new Expression(_armorScalingConfig.Value).ToLambda<ExpressionContext, float>();
        }

        public override float GraphScalingFunction(int itemCount)
        {
            return _reductionOnTrue.Value ? -ScalingFunction(itemCount,1) : ScalingFunction(itemCount);
        }*/
        
        
        public override string GetFormattedDescription(Inventory? inventory, string? token = null)
        {
            //ItemDef.descriptionToken = _reductionOnTrue.Value ? "BUB_REPULSION_ARMOR_MK2_DESC_REDUCTION" :  "BUB_REPULSION_ARMOR_MK2_DESC_ARMOR"; Cannot do this, it breaks the token matching from the tooltip patch
            var context = scalingInfos[0].WorkingContext;
            context.p = inventory?.GetItemCount(RoR2Content.Items.ArmorPlate) ?? 0;
            context.d = 0f;

            var tokenChoice = _reductionOnTrue.Value
                ? "BUB_REPULSION_ARMOR_MK2_DESC_REDUCTION"
                : "BUB_REPULSION_ARMOR_MK2_DESC_ARMOR";
            
            return base.GetFormattedDescription(inventory, tokenChoice);
        }

        protected override void MakeTokens()
        {
            base.MakeTokens();
            AddToken("REPULSION_ARMOR_MK2_NAME", "Repulsion Armor Plate Mk2");
            //AddToken("REPULSION_ARMOR_MK2_DESC", "Placeholder, swapped out with config value at runtime."); //pickup);

            // this mess #,###;#,###;0 is responsible for throwing away the negative sign when in the tooltip from the scaling function
            AddToken("REPULSION_ARMOR_MK2_DESC_REDUCTION", "Reduce all " + "incoming damage ".Style(StyleEnum.Damage) + "by " + "{0:#,###;#,###;0}".Style(StyleEnum.Damage) + ". Cannot be reduced below " + "1".Style(StyleEnum.Damage) + ". Scales with how much " + "Repulsion Armor Plates ".Style(StyleEnum.Utility) + "you have.");
            AddToken("REPULSION_ARMOR_MK2_DESC_ARMOR", "Increase armor ".Style(StyleEnum.Heal) + "by " + "{0} ".Style(StyleEnum.Heal) + ". Scales with how much " + "Repulsion Armor Plates ".Style(StyleEnum.Utility) + "you have.");

            // <style=cIsDamage>incoming damage</style> by <style=cIsDamage>5<style=cStack> (+5 per stack)</style></style>
            AddToken("REPULSION_ARMOR_MK2_PICKUP", "Receive damage reduction from all attacks depending on each " + "Repulsion Plate".Style(StyleEnum.Utility) + ".");
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
                var info = _instance.scalingInfos[0];
                info.WorkingContext.p = plateAmount; 
                __instance.armor += info.ScalingFunction(amount);
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
            var info = _instance.scalingInfos[0];
            info.WorkingContext.p = plateAmount;
            info.WorkingContext.d = damage;
            damage = Mathf.Max(1f, info.ScalingFunction(amount));
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