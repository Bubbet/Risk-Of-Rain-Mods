using System.Linq;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
//using InLobbyConfig;
//using InLobbyConfig.Fields;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Items
{
    [HarmonyPatch]
    public class EscapePlan : ItemBase
    {
        private static EscapePlan _instance;
        public static ConfigEntry<float> Granularity;

        protected override void MakeConfigs(ConfigFile configFile)
        {
            //if (ItemEnabled.Value) RepulsionArmorPlateMk2Plugin.Conf.RequiresR2Api = true;
            defaultScalingFunction = "-Log(1 - (1 - [h])) * (0.65 + 0.1 * [a])";
            defaultScalingDesc = "[a] = amount, [h] = health";
            base.MakeConfigs(configFile);
            Granularity = configFile.Bind("Balancing Functions", GetType().Name + " Granularity", 25f, "Value to multiply the scaling function by before its rounded, and then value to divide the buff count by.");
            _instance = this;
            /*
            if (!Chainloader.PluginInfos.ContainsKey(R2API.R2API.PluginGUID))
                ItemEnabled.Value = false;*/
        }

        /*
        public override void MakeInLobbyConfig(object modConfigEntryObj)
        {
            base.MakeInLobbyConfig(modConfigEntryObj);
            var modConfigEntry = (ModConfigEntry) modConfigEntryObj;
            var list = modConfigEntry.SectionFields["Scaling Functions"].ToList();
            list.Add(ConfigFieldUtilities.CreateFromBepInExConfigEntry(Granularity));
            modConfigEntry.SectionFields["Scaling Functions"] = list;
        }*/

        public override string GetFormattedDescription(Inventory inventory = null) // TODO Fill this
        {
            if (inventory)
            {
                var scale = "\n\n" + scaleConfig.Value + "\n" + scaleConfig.Description.Description.Split(';')[1];
                return Language.GetStringFormatted(ItemDef.descriptionToken, scale,
                    ScalingFunction(inventory.GetItemCount(ItemDef),
                        inventory.GetComponent<CharacterMaster>()?.GetBody()?.GetComponent<HealthComponent>()
                            ?.combinedHealthFraction ?? 1f / 500f));
            }
            else
                return base.GetFormattedDescription(inventory);
        }

        public float ScalingFunction(int itemCount, float health)
        {
            return scalingFunction(new ExpressionContext{ a = itemCount, h = health });
        }

        public override float ScalingFunction(int itemCount)
        {
            return ScalingFunction(itemCount, 1/500f);
        }
        
        /*
        protected override void MakeBehaviours()
        {
            RecalculateStatsAPI.GetStatCoefficients += RecalcStats;
            base.MakeBehaviours();
        }

        protected override void DestroyBehaviours()
        {
            RecalculateStatsAPI.GetStatCoefficients -= RecalcStats;
            base.DestroyBehaviours();
        }

        
        private void RecalcStats(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (!sender.inventory) return;
            var amount = sender.inventory.GetItemCount(ItemDef);
            args.moveSpeedMultAdd += 0.25f + 0.025f * amount * (1 - sender.healthComponent.combinedHealthFraction);
        }*/
        public override float GraphScalingFunction(int itemCount)
        {
            return ScalingFunction(itemCount, 1/500f);
        }

        protected override void MakeBehaviours()
        {
            base.MakeBehaviours();
            GlobalEventManager.onServerDamageDealt += DamageDealt;
            HealthComponent.onCharacterHealServer += HealServer;
        }

        protected override void DestroyBehaviours()
        {
            base.DestroyBehaviours();
            GlobalEventManager.onServerDamageDealt -= DamageDealt;
            HealthComponent.onCharacterHealServer -= HealServer;
        }

        /*
        private void HealServer(HealthComponent healthComponent, float arg2)
        {
            SetBuff(healthComponent.body);
        }*/
        
        private void HealServer(HealthComponent healthComponent, float arg2, ProcChainMask arg3)
        {
            SetBuff(healthComponent.body);
        }
        
        private void DamageDealt(DamageReport obj)
        {
            if (!obj.victim || !obj.victimBody) return;
            SetBuff(obj.victimBody);
        }
        private static void SetBuff(CharacterBody body)
        {
            if (!_instance.Enabled.Value) return;
            var amt = body.inventory != null ? body.inventory.GetItemCount(_instance.ItemDef) : 0;
            if (amt <= 0) return;
            //_instance.Logger.LogInfo("DamageDealt And Item");
            /*
            var buff = -Mathf.Log(-(1 - body.healthComponent.combinedHealthFraction) + 1f) * (0.65f + 0.1f * amt);
            var buffI = Mathf.RoundToInt(buff * 25);*/
            
            //_instance.Logger.LogInfo(buffI + " : " + buff);
            body.SetBuffCount(BubbetsItemsPlugin.ContentPack.buffDefs[0].buffIndex, Mathf.RoundToInt(_instance.ScalingFunction(amt, body.healthComponent.combinedHealthFraction) * Granularity.Value));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
        private static void RecalcStats(CharacterBody __instance)
        {
            var amt = __instance.GetBuffCount(BubbetsItemsPlugin.ContentPack.buffDefs[0]); // TODO replace this with some automatic system
            if (amt > 0)
            {
                __instance.moveSpeed *= 1f + amt / Granularity.Value;
            }

            SetBuff(__instance);
        }
        
            /*
            var inv = __instance.inventory;
            if (!__instance.inventory) return;
            _instance.Logger.LogInfo("Inventory yes");
            var amt = inv.GetItemCount(_instance.ItemDef);
            if (amt <= 0) return;
            _instance.Logger.LogInfo("EscapePlan Before: " + __instance.moveSpeed);
            //1.0f + 0.025f * (amt+1) * (1 - __instance.healthComponent.combinedHealthFraction);
            var buff = -Mathf.Log(-(1-__instance.healthComponent.combinedHealthFraction) + 1f) * (0.65f + 0.1f * amt);
            __instance.moveSpeed *= 1f + buff;
            _instance.Logger.LogInfo("EscapePlan After: " + __instance.moveSpeed + " : " + buff);
        }*/

        protected override void MakeTokens()
        {
            AddToken("ESCAPE_PLAN_NAME", "Escape Plan");
            AddToken("ESCAPE_PLAN_DESC", $"Get {"{1:P} extra move speed".Style(StyleEnum.Utility)}. Increases the closer to {"death".Style(StyleEnum.Health)} you are.\n{{0}}"); //"Get 75% (+10% per item) movement speed (at 0% hp scaling logarithmically) the lower your health is.");
            AddToken("ESCAPE_PLAN_PICKUP", "Get movement speed the lower your health is.");
            AddToken("ESCAPE_PLAN_LORE", "Escape Plan");
            base.MakeTokens();
        } 
        // 0.5 * x * 1(amount) = 1.25 
        // 0.1 * x * 1(amount) = 1.5
        
        // 1.25 / 0.5 / 1 = x
        // x = 2.5
        // 1.5 / 0.1 = 15
    }
}