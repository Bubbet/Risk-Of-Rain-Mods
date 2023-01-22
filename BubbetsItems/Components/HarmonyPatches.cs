using System;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Audio;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace BubbetsItems
{
    [HarmonyPatch]
    public static class HarmonyPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(NetworkSoundEventCatalog), nameof(NetworkSoundEventCatalog.Init))]
        public static void LoadSoundbank()
        {
            BubbetsItemsPlugin.LoadSoundBank();
        }
        [HarmonyILManipulator, HarmonyPatch(typeof(GlobalEventManager), nameof(GlobalEventManager.OnCharacterDeath))]
        private static void AmmoPickupPatch(ILContext il)
        {
            if (!BubbetsItemsPlugin.Conf.AmmoPickupAsOrbEnabled.Value) return;
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdstr("Prefabs/NetworkedObjects/AmmoPack"),
                x => x.OpCode == OpCodes.Call// && (x.Operand as MethodInfo)?.Name == "Load",
                //x => x.MatchLdloc(out _)
            );
            var start = c.Index;
            c.GotoNext(MoveType.After,
                x => x.MatchCall<NetworkServer>("Spawn")
            );
            var end = c.Index;
            c.Index = start;
            c.RemoveRange(end - start);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<AmmoPickupDele>(DoAmmoPickupAsOrb);
        }
        private static void DoAmmoPickupAsOrb(DamageReport report)
        {
            OrbManager.instance.AddOrb(new AmmoPickupOrb {
                origin = report.victim.transform.position,
                target = report.attackerBody.mainHurtBox,
            });
        }
        delegate void AmmoPickupDele(DamageReport report);
    }
}