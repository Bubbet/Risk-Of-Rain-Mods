using System.Collections.Generic;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
using InLobbyConfig.Fields;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Items
{
    [HarmonyPatch]
    public class EscapePlan : ItemBase
    {
        public static ConfigEntry<float> Granularity;
        
        
        private static BuffDef? _buffDef;
        private static BuffDef? BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefEscapePlan");
        
        protected override void MakeTokens()
        {
            AddToken("ESCAPE_PLAN_NAME", "Escape Plan");
            AddToken("ESCAPE_PLAN_PICKUP", "Increases " + "movement speed ".Style(StyleEnum.Utility) + "the closer you are to death.");
            AddToken("ESCAPE_PLAN_DESC", "Move up to " + "{0:0%} faster".Style(StyleEnum.Utility) + ". Increases the closer you are to " + "death".Style(StyleEnum.Death) + ".");
            AddToken("ESCAPE_PLAN_LORE", "Escape Plan");
            base.MakeTokens();
        } 

        protected override void MakeConfigs()
        {
            //if (ItemEnabled.Value) RepulsionArmorPlateMk2Plugin.Conf.RequiresR2Api = true;
            base.MakeConfigs();
            AddScalingFunction("-Log(1 - (1 - [h])) * (0.65 + 0.1 * [a])", "Movement Speed", "[a] = amount, [h] = health");
            Granularity = sharedInfo.ConfigFile!.Bind(ConfigCategoriesEnum.BalancingFunctions, GetType().Name + " Granularity", 25f, "Value to multiply the scaling function by before its rounded, and then value to divide the buff count by.");
            /*
            if (!Chainloader.PluginInfos.ContainsKey(R2API.R2API.PluginGUID))
                ItemEnabled.Value = false;*/
        }
        public override void MakeInLobbyConfig(Dictionary<ConfigCategoriesEnum, List<object>> scalingFunctions)
        {
            base.MakeInLobbyConfig(scalingFunctions);
            scalingFunctions[ConfigCategoriesEnum.BalancingFunctions].Add(ConfigFieldUtilities.CreateFromBepInExConfigEntry(Granularity));
        }

        public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
        {
            if (inventory)
            {
                scalingInfos[0].WorkingContext.h = inventory.GetComponent<CharacterMaster>()?.GetBody()
                    ?.GetComponent<HealthComponent>()
                    ?.combinedHealthFraction ?? 1f / 500f;
            }
            return base.GetFormattedDescription(inventory, token, forceHideExtended);
        }
        /* TODO
        public override string GetFormattedDescription([CanBeNull] Inventory inventory) // TODO Fill this
        {
            if (!inventory) return base.GetFormattedDescription(inventory);
            
            var scale = "\n\n" + scaleConfig.Value + "\n" + scaleConfig.Description.Description.Split(';')[1];
            return Language.GetStringFormatted(ItemDef.descriptionToken, scale,
                ScalingFunction(inventory.GetItemCount(ItemDef),
                    inventory.GetComponent<CharacterMaster>()?.GetBody()?.GetComponent<HealthComponent>()
                        ?.combinedHealthFraction ?? 1f / 500f));

        }*/
        
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

        protected override void MakeBehaviours()
        {
            base.MakeBehaviours();
            GlobalEventManager.onServerDamageDealt += DamageDealt;
            //HealthComponent.onCharacterHealServer += HealServer;
        }

        protected override void DestroyBehaviours()
        {
            base.DestroyBehaviours();
            GlobalEventManager.onServerDamageDealt -= DamageDealt;
            //HealthComponent.onCharacterHealServer -= HealServer;
        }

        /*
        private void HealServer(HealthComponent healthComponent, float arg2)
        {
            SetBuff(healthComponent.body);
        }*/
        
        [HarmonyPostfix, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.Heal))]
        private static void HealServer(HealthComponent __instance)
        {
            SetBuff(__instance.body);
        }
        
        private void DamageDealt(DamageReport obj)
        {
            if (!obj.victim || !obj.victimBody) return;
            SetBuff(obj.victimBody);
        }
        private static void SetBuff(CharacterBody body)
        {
            var inv = body.inventory;
            if (!inv) return;
            var escapePlan = GetInstance<EscapePlan>();
            var amt = body.inventory.GetItemCount(escapePlan.ItemDef);
            if (amt <= 0) return;
            //_instance.Logger.LogInfo("DamageDealt And Item");
            /*
            var buff = -Mathf.Log(-(1 - body.healthComponent.combinedHealthFraction) + 1f) * (0.65f + 0.1f * amt);
            var buffI = Mathf.RoundToInt(buff * 25);*/
            
            //_instance.Logger.LogInfo(buffI + " : " + buff);
            var info = escapePlan.scalingInfos[0];
            info.WorkingContext.h = body.healthComponent.combinedHealthFraction;
            body.SetBuffCount(BuffDef!.buffIndex, Mathf.RoundToInt(info.ScalingFunction(amt) * Granularity.Value ));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
        private static void RecalcStats(CharacterBody __instance)
        {
            var amt = __instance.GetBuffCount(BuffDef);
            if (amt > 0)
            {
                __instance.moveSpeed *= 1f + amt / Granularity.Value;
            }

            //SetBuff(__instance);
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
        // 0.5 * x * 1(amount) = 1.25 
        // 0.1 * x * 1(amount) = 1.5
        
        // 1.25 / 0.5 / 1 = x
        // x = 2.5
        // 1.5 / 0.1 = 15
    }
}