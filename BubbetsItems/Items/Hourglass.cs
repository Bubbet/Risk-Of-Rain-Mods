using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aetherium;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine.Networking;

namespace BubbetsItems.Items
{
    [HarmonyPatch]
    public class Hourglass : ItemBase
    {
        private MethodInfo _aetheriumOrig;
        private MethodInfo _aetheriumOrig1;
        public ConfigEntry<string> buffBlacklist;

        private IEnumerable<BuffDef> buffDefBlacklist;
        private static bool AetheriumEnabled => Chainloader.PluginInfos.ContainsKey(AetheriumPlugin.ModGuid);

        protected override void MakeTokens()
        {
            AddToken("TIMED_BUFF_DURATION_ITEM_NAME", "Abundant Hourglass");
            AddToken("TIMED_BUFF_DURATION_ITEM_DESC", "Duration of " + "buffs ".Style(StyleEnum.Damage) + "are multiplied by " + "{0}".Style(StyleEnum.Utility) + ".");
            AddToken("TIMED_BUFF_DURATION_ITEM_DESC_SIMPLE", "Increases the duration of " + "buffs ".Style(StyleEnum.Damage) + "by " + "125% ".Style(StyleEnum.Utility) + "(+10% per stack)".Style(StyleEnum.Stack) + ".");
            SimpleDescriptionToken = "TIMED_BUFF_DURATION_ITEM_DESC_SIMPLE";
            AddToken("TIMED_BUFF_DURATION_ITEM_PICKUP", "Duration of " + "buffs ".Style(StyleEnum.Damage) + "are increased.");
            AddToken("TIMED_BUFF_DURATION_ITEM_LORE", "BUB_TIMED_BUFF_DURATION_ITEM_LORE");
            base.MakeTokens();
        }
        protected override void MakeConfigs()
        {
            base.MakeConfigs();
            AddScalingFunction("[a] * 0.1 + 1.15", "Buff Duration");
        }

        // lmao i'm just going to abuse the timing of this because im lazy B)
        protected override void FillRequiredExpansions()
        {
            var defaultValue = "bdBearVoidCooldown bdElementalRingsCooldown bdElementalRingVoidCooldown bdVoidFogMild bdVoidFogStrong bdVoidRaidCrabWardWipeFog bdMedkitHeal";
            buffBlacklist = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Hourglass Buff Blacklist", defaultValue, "Blocks debuffs automatically. These are all considered buffs by the game and there is no way to tell if they're used as a timed buff it'll just do nothing if its not, Valid values: " +  string.Join(" ", BuffCatalog.nameToBuffIndex.Where(x => !BuffCatalog.GetBuffDef(x.Value).isDebuff).Select(x => x.Key).ToList()));
            buffBlacklist.SettingChanged += (_, _) => SettingChanged();
            SettingChanged();
            
            if (BubbetsItemsPlugin.riskOfOptionsEnabled)
            {
                MakeRiskOfOptionsLate();
            }
            
            base.FillRequiredExpansions();
        }

        private void MakeRiskOfOptionsLate()
        {
            ModSettingsManager.AddOption(new StringInputFieldOption(buffBlacklist));
        }

        private void SettingChanged()
        {
            buffDefBlacklist = from str in buffBlacklist.Value.Split(' ') select BuffCatalog.FindBuffIndex(str) into index where index != BuffIndex.None select BuffCatalog.GetBuffDef(index);
        }

        //*
        protected override void MakeBehaviours()
        {
            base.MakeBehaviours();
            if (!AetheriumEnabled) return;
            PatchAetherium();
        }
        
        private void PatchAetherium() // This needs to be its own function because for some reason typeof() was being called at the start of the function and it was throwing file not found exception
        {
            _aetheriumOrig1 = typeof(AetheriumPlugin).Assembly.GetType("Aetherium.Utils.ItemHelpers").GetMethod("RefreshTimedBuffs", new[] {typeof(CharacterBody), typeof(BuffDef), typeof(float)})!;
            _aetheriumOrig = typeof(AetheriumPlugin).Assembly.GetType("Aetherium.Utils.ItemHelpers").GetMethod("RefreshTimedBuffs", new[] {typeof(CharacterBody), typeof(BuffDef), typeof(float), typeof(float)})!;
            sharedInfo.Harmony?.Patch(_aetheriumOrig1, new HarmonyMethod(GetType().GetMethod("AetheriumTimedBuffHook1")));
            sharedInfo.Harmony?.Patch(_aetheriumOrig, new HarmonyMethod(GetType().GetMethod("AetheriumTimedBuffHook")));
        }

        protected override void DestroyBehaviours()
        {
            base.DestroyBehaviours();
            if (!AetheriumEnabled) return;
            sharedInfo.Harmony?.Unpatch(_aetheriumOrig, HarmonyPatchType.Prefix);
        }//*/

        
        // ReSharper disable once InconsistentNaming
        [HarmonyPrefix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.AddTimedBuff), typeof(BuffDef), typeof(float))]
        public static void TimedBuffHook(CharacterBody __instance, BuffDef buffDef, ref float duration)
        {
            if (!NetworkServer.active) return;
            duration = DoDurationPatch(__instance, buffDef, duration);
        }
        
        // Hooked in awake
        public static void AetheriumTimedBuffHook1(CharacterBody body, BuffDef buffDef, ref float duration)
        {
            duration = DoDurationPatch(body, buffDef, duration);
        }
        public static void AetheriumTimedBuffHook(CharacterBody body, BuffDef buffDef, ref float taperStart, ref float taperDuration)
        {
            taperStart = DoDurationPatch(body, buffDef, taperStart);
            taperDuration = DoDurationPatch(body, buffDef, taperDuration);
        }

        public static float DoDurationPatch(CharacterBody cb, BuffDef def, float duration)
        {
            if (def.isDebuff) return duration;
            var hourglass = GetInstance<Hourglass>();
            if (hourglass.buffDefBlacklist.Contains(def)) return duration;
            var inv = cb.inventory;
            if (!inv) return duration;
            var amount = cb.inventory.GetItemCount(hourglass.ItemDef);
            if (amount <= 0) return duration;
            duration *= hourglass.scalingInfos[0].ScalingFunction(amount);
            return duration;
        }
    }
}