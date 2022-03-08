using System;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Networking;
using RoR2.UI;
using UnityEngine;

namespace DamageHistory
{
    [HarmonyPatch]
    public static class HarmonyPatches
    {
        [HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.Heal))]
        public static void HealPatch(ILContext ctx)
        {
            var c = new ILCursor(ctx);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<HealthComponent>("health"),
                x => x.MatchLdloc(out _)
            );

            c.Index++;
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldarg_3);
            c.EmitDelegate<HealDele>((hc, amount, notRegen) =>
            {
                //Debug.Log("Logging Health Dele: " + amount);
                onCharacterHealWithRegen?.Invoke(hc, amount, notRegen);
            });
        }
        
        public static event Action<HealthComponent, float, bool> onCharacterHealWithRegen;
        private delegate void HealDele(HealthComponent hc, float amount, bool notRegen);

        //[HarmonyPrefix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.OnClientConnect))]
        public static bool DisableChecks()
        {
            return false;
        }
    }
}