using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using RoR2;

namespace BubbetsItems
{
    [HarmonyPatch]
    public class EquipmentBase : SharedBase
    {
        protected override void MakeConfigs(ConfigFile configFile)
        {
            var name = GetType().Name;
            Enabled = configFile.Bind("Disable Equipments", name, true, "Should this equipment be enabled.");
        }

        public virtual bool PerformEquipment(EquipmentSlot equipmentSlot) { return false; }
        public virtual void OnUnEquip(Inventory inventory, EquipmentState newEquipmentState) {}
        public virtual void OnEquip(Inventory inventory, EquipmentState? oldEquipmentState) {}
        protected virtual void PostEquipmentDef() {}
        
        public EquipmentDef EquipmentDef;
        
        private static IEnumerable<EquipmentBase> _equipments;
        public static IEnumerable<EquipmentBase> Equipments => _equipments ?? (_equipments = Instances.OfType<EquipmentBase>());

        [HarmonyPrefix, HarmonyPatch(typeof(EquipmentSlot), nameof(EquipmentSlot.PerformEquipmentAction))]
        public static bool PerformEquipmentAction(EquipmentSlot __instance, EquipmentDef equipmentDef, ref bool __result)
        {
            var equipment = Equipments.FirstOrDefault(x => x.EquipmentDef == equipmentDef);
            if (equipment == null) return true;
            
            try
            {
                __result = equipment.PerformEquipment(__instance);
            }
            catch (Exception e)
            {
                equipment.Logger.LogError(e);
            }

            return false;
        }

        public override string GetFormattedDescription(Inventory inventory = null)
        {
            return Language.GetString(EquipmentDef.descriptionToken);
        }

        /*
        [HarmonyPrefix, HarmonyPatch(typeof(EquipmentSlot), nameof(EquipmentSlot.UpdateInventory))]
        public static void OnEquipmentSwap(EquipmentSlot __instance)
        {
            var inventory = __instance.characterBody.inventory;
            if (!inventory) return;
            
            var oldEquipmentIndex = __instance.equipmentIndex;
            var newEquipmentIndex = inventory.GetEquipmentIndex();
            if (newEquipmentIndex == oldEquipmentIndex) return;
            
            Equipments.FirstOrDefault(x => x.EquipmentDef.equipmentIndex == oldEquipmentIndex)?.OnUnequip(__instance, newEquipmentIndex);
            Equipments.FirstOrDefault(x => x.EquipmentDef.equipmentIndex == newEquipmentIndex)?.OnEquip(__instance, oldEquipmentIndex);
        }
        */

        [HarmonyPrefix, HarmonyPatch(typeof(Inventory), nameof(Inventory.SetEquipmentInternal))]
        public static void OnEquipmentSwap(Inventory __instance, EquipmentState equipmentState, uint slot)
        {
            EquipmentState? oldState = null;
            if (__instance.equipmentStateSlots.Length > (long) ((ulong) slot))
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
                oldEquip.Logger.LogError(e);
            }

            try
            {
                newEquip?.OnEquip(__instance, oldState);
            }
            catch (Exception e)
            {
                newEquip?.Logger.LogError(e);
            }
        }

        public void RenderPickup()
        {
            PickupRenderer.PickupRenderer.RenderPickupIcon(new ConCommandArgs {userArgs = new List<string> {EquipmentDef.name}});
        }

        [SystemInitializer(typeof(EquipmentCatalog))]
        public static void AssignAllEquipmentDefs()
        {
            var equipments = Instances.OfType<EquipmentBase>().ToList();
            foreach (var pack in ContentPacks)
            {
                foreach (var equipmentDef in pack.equipmentDefs)
                {
                    foreach (var equipment in equipments.Where(shared => equipmentDef.name == shared.GetType().Name))
                    {
                        equipment.EquipmentDef = equipmentDef;
                        equipment.PostEquipmentDef();
                    }
                }
            }

            foreach (var x in equipments)
            {
                try
                {
                    PickupIndexes.Add(PickupCatalog.FindPickupIndex(x.EquipmentDef.equipmentIndex), x);
                }
                catch (NullReferenceException e)
                {
                    x.Logger.LogError("Equipment " + x.GetType().Name + " threw a NRE when filling pickup indexes, this could mean its not defined in your content pack:\n" + e);
                }
            }
        }
    }
}