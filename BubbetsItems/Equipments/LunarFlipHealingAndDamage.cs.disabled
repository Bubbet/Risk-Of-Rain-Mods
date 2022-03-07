using System;
using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BubbetsItems.Equipments
{
    public class LunarFlipHealingAndDamage : EquipmentBase
    {
        protected override void MakeConfigs(ConfigFile configFile)
        {
            base.MakeConfigs(configFile);
            Enabled.Value = false;
        }

        public override bool PerformEquipment(EquipmentSlot equipmentSlot)
        {
            return equipmentSlot.inventory.gameObject.GetComponent<LunarFlipHealthAndDamageBehaviour>().Enable();
        }

        public override void OnUnEquip(Inventory inventory, EquipmentState newEquipmentState)
        {
            base.OnUnEquip(inventory, newEquipmentState);
            Object.Destroy(inventory.gameObject.GetComponent<LunarFlipHealthAndDamageBehaviour>());
        }

        public override void OnEquip(Inventory inventory, EquipmentState? oldEquipmentState)
        {
            base.OnEquip(inventory, oldEquipmentState);
            inventory.gameObject.AddComponent<LunarFlipHealthAndDamageBehaviour>();
        }

        /*
        [HarmonyPrefix, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
        public static void TakeDamage(HealthComponent __instance, ref DamageInfo damageInfo)
        {
            if (__instance.body.master is null) return;
            var comp = __instance.body.master.GetComponent<LunarFlipHealthAndDamageBehaviour>();
            if (comp && comp.flipped)
            {
                var damage = (damageInfo.crit ? 2f : 1f) * damageInfo.damage;
                comp.damageTaken += damage;
                damageInfo.damage = comp.healingTaken;
                if (comp.damageTaken > 0 && !(damageInfo is FlipDamageInfo))
                {
                    var mask = new ProcChainMask();
                    mask.AddProc(ProcType.LightningStrikeOnHit);
                    __instance.Heal(0, mask);
                }

                comp.healingTaken -= comp.healingTaken;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.Heal))]
        public static void Heal(HealthComponent __instance, ref float amount, ProcChainMask procChainMask)
        {
            if (__instance.body.master is null) return;
            var comp = __instance.body.master.GetComponent<LunarFlipHealthAndDamageBehaviour>();
            if (comp && comp.flipped)
            {
                comp.healingTaken += amount;
                amount = comp.damageTaken;
                if (comp.healingTaken > 0 && !procChainMask.HasProc(ProcType.LightningStrikeOnHit))
                {
                    var info = new FlipDamageInfo();
                    info.attacker = __instance.gameObject;
                    info.inflictor = __instance.gameObject;
                    __instance.TakeDamage(info);
                }

                comp.damageTaken -= comp.damageTaken;
            }
        }*/

        [HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
        public static void FixPlanula(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel jumpTarg = null;
            c.GotoNext( MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<HealthComponent>("itemCounts"),
                x => x.MatchLdfld<HealthComponent.ItemCounts>("parentEgg"),
                x => x.MatchLdcI4(0),
                x => x.MatchBle(out jumpTarg)
            );
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<DamageInfo, bool>>(info => info is FlipDamageInfo);
            c.Emit(OpCodes.Brtrue_S, jumpTarg);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
        public static void TakeDamage(HealthComponent __instance, ref DamageInfo damageInfo)
        {
            if (__instance.body.master is null) return;
            var comp = __instance.body.master.GetComponent<LunarFlipHealthAndDamageBehaviour>();
            if (comp && comp.flipped)
            {
                if (!(damageInfo is FlipDamageInfo))
                {
                    comp.damageTaken += damageInfo.damage * (damageInfo.crit ? 2f : 1f);
                    damageInfo.damage = 0;
                    
                    var mask = new ProcChainMask(); 
                    mask.AddProc(ProcType.LightningStrikeOnHit);
                    __instance.Heal(0, mask);
                }
                else
                {
                    damageInfo.damage = comp.healingTaken;
                    comp.healingTaken -= damageInfo.damage;
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.Heal))]
        public static void Heal(HealthComponent __instance, ref float amount, ProcChainMask procChainMask)
        {
            if (__instance.body.master is null) return;
            var comp = __instance.body.master.GetComponent<LunarFlipHealthAndDamageBehaviour>();
            if (comp && comp.flipped)
            {
                if (!procChainMask.HasProc(ProcType.LightningStrikeOnHit))
                {
                    comp.healingTaken += amount;
                    amount = 0;
                    
                    var info = new FlipDamageInfo();
                    info.attacker = __instance.gameObject;
                    info.inflictor = __instance.gameObject;
                    __instance.TakeDamage(info);
                }else
                {
                    amount = comp.damageTaken;
                    comp.damageTaken -= amount;
                }
            }
        }
    }

    public class FlipDamageInfo : DamageInfo
    {
        
    }

    public class LunarFlipHealthAndDamageBehaviour : MonoBehaviour
    {
        public bool flipped;
        public float damageTaken;
        public float healingTaken;

        public bool Enable()
        {
            flipped = !flipped;
            return false;
        }
    }
}