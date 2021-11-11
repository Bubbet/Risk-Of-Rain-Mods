using System.Reflection;
using Aetherium;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
using RoR2;
using UnityEngine.Networking;

namespace BubbetsItems.Items
{
    [HarmonyPatch]
    public class AbundantHourglass : ItemBase
    {
        private MethodInfo _aetheriumOrig;
        private static AbundantHourglass _instance;
        private static bool AetheriumEnabled => Chainloader.PluginInfos.ContainsKey(AetheriumPlugin.ModGuid);

        protected override void MakeConfigs(ConfigFile configFile)
        {
            defaultScalingFunction = "[a] * 0.1 + 1.15";
            base.MakeConfigs(configFile);
            _instance = this;
        }

        protected override void MakeBehaviours()
        {
            base.MakeBehaviours();
            if (!AetheriumEnabled) return;
            PatchAetherium();
        }

        private void PatchAetherium() // This needs to be its own function because for some reason typeof() was being called at the start of the function and it was throwing file not found exception
        {
            _aetheriumOrig = typeof(AetheriumPlugin).Assembly.GetType("Aetherium.Utils.ItemHelpers").GetMethod("RefreshTimedBuffs", new[] {typeof(CharacterBody), typeof(BuffDef), typeof(float), typeof(float)});
            Harmony.Patch(_aetheriumOrig, new HarmonyMethod(GetType().GetMethod("AetheriumTimedBuffHook")));
        }

        protected override void DestroyBehaviours()
        {
            base.DestroyBehaviours();
            if (!AetheriumEnabled) return;
            Harmony.Unpatch(_aetheriumOrig, HarmonyPatchType.Prefix);
        }

        protected override void MakeTokens()
        {
            AddToken("TIMED_BUFF_DURATION_ITEM_NAME", "Abundant Hourglass");
            AddToken("TIMED_BUFF_DURATION_ITEM_PICKUP", "Increases the duration of timed buffs.");
            AddToken("TIMED_BUFF_DURATION_ITEM_DESC", $"Multiplies the {"duration".Style(StyleEnum.Utility)} of timed buffs by {"{1}".Style(StyleEnum.Utility)}\n{{0}}");// {scaleConfig.Value} (1: {ScalingFunction(1)}, 2: {ScalingFunction(2)}, 3: {ScalingFunction(3)})");
            AddToken("TIMED_BUFF_DURATION_ITEM_LORE", "BUB_TIMED_BUFF_DURATION_ITEM_LORE");
            base.MakeTokens();
        }
        
        // ReSharper disable once InconsistentNaming
        [HarmonyPrefix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.AddTimedBuff), typeof(BuffDef), typeof(float))]
        public static void TimedBuffHook(CharacterBody __instance, BuffDef buffDef, ref float duration)
        {
            if (!_instance.Enabled.Value) return;
            if (!NetworkServer.active) return;
            duration = DoDurationPatch(__instance, buffDef, duration);
        }
        
        // Hooked in awake
        public static void AetheriumTimedBuffHook(CharacterBody body, BuffDef buffDef, ref float taperStart)
        {
            taperStart = DoDurationPatch(body, buffDef, taperStart);
        }

        private static float DoDurationPatch(CharacterBody cb, BuffDef def, float duration)
        {
            if (def.isDebuff) return duration;
            var inv = cb.inventory;
            if (!inv) return duration;
            var amount = cb.inventory.GetItemCount(_instance.ItemDef);
            if (amount <= 0) return duration;
            //scalingFunc.Parameters["a"] = amount;
            //var cont = new ExpressionC();
            duration *= _instance.ScalingFunction(amount);
            //duration *= amount * 0.10f + 1.15f;
            //_instance.Logger.LogError(duration);
            return duration;
        }
    }
}