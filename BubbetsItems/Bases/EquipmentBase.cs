using System;
using System.Collections.Generic;
using System.Linq;
using BubbetsItems.Helpers;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine.Networking;

namespace BubbetsItems.Bases
{
    [HarmonyPatch]
    public abstract class EquipmentBase : SharedBase
    {
        protected override void MakeConfigs()
        {
            var name = GetType().Name;
            Enabled = configFile!.Bind("Disable Equipments", name, true, "Should this equipment be enabled.");
        }

        public virtual bool PerformEquipment(EquipmentSlot equipmentSlot) { return false; }
        public virtual void OnUnEquip(Inventory inventory, EquipmentState newEquipmentState) {}
        public virtual void OnEquip(Inventory inventory, EquipmentState? oldEquipmentState) {}
        public virtual bool UpdateTargets(EquipmentSlot equipmentSlot) { return false; }
        protected virtual void PostEquipmentDef() {}
        
        public EquipmentDef? EquipmentDef;
        
        private static IEnumerable<EquipmentBase>? _equipments;
        public static IEnumerable<EquipmentBase> Equipments => _equipments ??= Instances.OfType<EquipmentBase>();

        [HarmonyPrefix, HarmonyPatch(typeof(EquipmentSlot), nameof(EquipmentSlot.RpcOnClientEquipmentActivationRecieved))]
        // ReSharper disable thrice InconsistentNaming
        public static void PerformEquipmentActionRpc(EquipmentSlot __instance) // third
        {
            if (NetworkServer.active) return;
            if (__instance.characterBody.hasEffectiveAuthority) return;
            var boo = false;
            PerformEquipmentAction(__instance, EquipmentCatalog.GetEquipmentDef(__instance.equipmentIndex), ref boo);
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(EquipmentSlot), nameof(EquipmentSlot.CallCmdExecuteIfReady))]
        public static void PerformEquipmentActionClient(EquipmentSlot __instance) // first
        {
            if (!__instance.characterBody.hasEffectiveAuthority) return;
            if (__instance.equipmentIndex == EquipmentIndex.None || __instance.stock <= 0) return;
            var boo = false;
            PerformEquipmentAction(__instance, EquipmentCatalog.GetEquipmentDef(__instance.equipmentIndex), ref boo);
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(EquipmentSlot), nameof(EquipmentSlot.PerformEquipmentAction))]
        public static bool PerformEquipmentAction(EquipmentSlot __instance, EquipmentDef equipmentDef, ref bool __result) // second
        {
            var equipment = Equipments.FirstOrDefault(x => x.EquipmentDef == equipmentDef);
            if (equipment == null) return true;
            
            try
            {
                __result = equipment.PerformEquipment(__instance);
            }
            catch (Exception e)
            {
                equipment.Logger?.LogError(e);
            }

            return false;
        }

        [HarmonyILManipulator, HarmonyPatch(typeof(EquipmentSlot), nameof(EquipmentSlot.UpdateTargets))]
        public static void UpdateTargetsIL(ILContext il)
        {
            var c = new ILCursor(il);
            var activeFlag = -1;
            c.GotoNext( MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<EquipmentSlot>("targetIndicator"),
                x => x.MatchLdloc(out activeFlag)
            );
            c.Index--;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<EquipmentSlot, bool>>(UpdateTargetsHook);
            c.Emit(OpCodes.Ldloc, activeFlag);
            c.Emit(OpCodes.Or);
            c.Emit(OpCodes.Stloc, activeFlag);
        }
        public static bool UpdateTargetsHook(EquipmentSlot __instance) // this is probably the most expensive function in my mod, its mostly because of the linq inside a update function which is pretty ew but im not smart enough to change it
        {
            var equipment = Equipments.FirstOrDefault(x => x.EquipmentDef!.equipmentIndex == __instance.equipmentIndex);
            if (equipment == null) return false;
            
            try
            {
                return equipment.UpdateTargets(__instance);
            }
            catch (Exception e)
            {
                equipment.Logger?.LogError(e);
            }

            return false;
        }

        public override string GetFormattedDescription(Inventory? inventory = null, string? token = null)
        {
            return Language.GetString(token ?? EquipmentDef!.descriptionToken);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Inventory), nameof(Inventory.SetEquipmentInternal))]
        // ReSharper disable once InconsistentNaming
        public static void OnEquipmentSwap(Inventory __instance, EquipmentState equipmentState, uint slot)
        {
            EquipmentState? oldState = null;
            if (__instance.equipmentStateSlots.Length > slot)
                oldState = __instance.equipmentStateSlots[(int) slot];
            if (oldState.Equals(equipmentState)) return;
            if (oldState?.equipmentIndex == equipmentState.equipmentIndex) return;

            var oldDef = oldState?.equipmentDef;
            var newDef = equipmentState.equipmentDef;

            var oldEquip = Equipments.FirstOrDefault(x => x.EquipmentDef == oldDef);
            var newEquip = Equipments.FirstOrDefault(x => x.EquipmentDef == newDef);

            try
            {
                oldEquip?.OnUnEquip(__instance, equipmentState);
            }
            catch (Exception e)
            {
                oldEquip?.Logger?.LogError(e);
            }

            try
            {
                newEquip?.OnEquip(__instance, oldState);
            }
            catch (Exception e)
            {
                newEquip?.Logger?.LogError(e);
            }
        }
        
        public override void AddDisplayRules(VanillaCharacterIDRS which, ItemDisplayRule[] displayRules)
        {
            IDRHelper.GetRuleSet(which)?.keyAssetRuleGroups.AddItem(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                displayRuleGroup = new DisplayRuleGroup {rules = displayRules},
                keyAsset = EquipmentDef
            });
        }

        /*
        public void RenderPickup()
        {
            PickupRenderer.PickupRenderer.RenderPickupIcon(new ConCommandArgs {userArgs = new List<string> {EquipmentDef.name}});
        }*/

        protected override void FillDefsFromSerializableCP(SerializableContentPack serializableContentPack)
        {
            base.FillDefsFromSerializableCP(serializableContentPack);
            var name = GetType().Name;
            foreach (var equipmentDef in serializableContentPack.equipmentDefs)
            {
                if (MatchName(equipmentDef.name, name)) EquipmentDef = equipmentDef;
            }
            if (EquipmentDef == null)
            {
                Logger?.LogWarning($"Could not find EquipmentDef for item {this} in serializableContentPack, class/equipmentdef name are probably mismatched. This will throw an exception later.");
            }
        }

        protected override void FillDefsFromContentPack()
        {
            foreach (var pack in ContentPacks)
            {
                if (EquipmentDef != null) continue;
                var name = GetType().Name;
                foreach (var equipmentDef in pack.equipmentDefs)
                    if (MatchName(equipmentDef.name, name))
                        EquipmentDef = equipmentDef;
            }
            
            if (EquipmentDef == null) 
                Logger?.LogWarning(
                    $"Could not find EquipmentDef for item {this}, class/equipmentdef name are probably mismatched. This will throw an exception later.");
        }

        protected override void FillPickupIndex()
        {
            try
            {
                var pickup = PickupCatalog.FindPickupIndex(EquipmentDef!.equipmentIndex);
                PickupIndex = pickup;
                PickupIndexes.Add(pickup, this);
            }
            catch (NullReferenceException e)
            {
                Logger?.LogError("Equipment " + GetType().Name +
                                 " threw a NRE when filling pickup indexes, this could mean its not defined in your content pack:\n" +
                                 e);
            }
        }

        protected override void FillRequiredExpansions()
        {
            if (RequiresSotv)
                EquipmentDef!.requiredExpansion = SotvExpansion;
        }
    }
}