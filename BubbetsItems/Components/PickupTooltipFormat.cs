using System;
using System.Linq;
using HarmonyLib;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using RoR2.UI.LogBook;

namespace BubbetsItems
{
    [HarmonyPatch]
    public class PickupTooltipFormat
    {
        /*
        public static void Init(Harmony harmony)
        {
            if (Chainloader.PluginInfos.ContainsKey("com.xoxfaby.BetterUI"))
            {
                //InitBetterUIPatches(harmony);
            }
        }

        private static void InitBetterUIPatches(Harmony harmony)
        {
            var methodInfo = typeof(AdvancedIcons).GetMethod("EquipmentIcon_Update", BindingFlags.Static);
            Debug.Log(methodInfo);
            var harmonyMethod = new HarmonyMethod()
            {
                declaringType = typeof(AdvancedIcons),
                methodName = "EquipmentIcon_Update"
            };
            var bae = (MethodBase) AccessTools.DeclaredMethod(typeof(AdvancedIcons), "EquipmentIcon_Update");
            harmony.Patch(bae, null, null, null, null, new HarmonyMethod(typeof(PickupTooltipFormat).GetMethod("FixBetterUIsGarbage")));
        }*/

        [HarmonyPrefix, HarmonyPatch(typeof(TooltipProvider), "get_bodyText")]
        public static bool FixToken(TooltipProvider __instance, ref string __result)
        {
            try
            {
                //if (!string.IsNullOrEmpty(__instance.overrideBodyText)) return true;

                var item = ItemBase.Items.FirstOrDefault(x =>
                {
                    if (x.ItemDef == null) // This is a really bad way of doing this
                        BubbetsItemsPlugin.Log.LogWarning($"ItemDef is null for {x} in tooltipProvider, this will throw errors.");
                    return __instance.bodyToken == x.ItemDef.descriptionToken;
                });
                var equipment = EquipmentBase.Equipments.FirstOrDefault(x =>
                {
                    if (x.EquipmentDef == null)
                        BubbetsItemsPlugin.Log.LogWarning($"EquipmentDef is null for {x} in tooltipProvider, this will throw errors.");
                    return __instance.bodyToken == x.EquipmentDef.descriptionToken;
                });
                var titleEquipment = EquipmentBase.Equipments.FirstOrDefault(x =>
                {
                    if (x.EquipmentDef == null)
                        BubbetsItemsPlugin.Log.LogWarning($"EquipmentDef is null for {x} in tooltipProvider, this will throw errors.");
                    return __instance.titleToken == x.EquipmentDef.nameToken;
                });

                var inventoryDisplay = __instance.transform.parent.GetComponent<ItemInventoryDisplay>();

                // ReSharper disable twice Unity.NoNullPropagation
                if (item != null)
                {
                    __result = item.GetFormattedDescription(inventoryDisplay?.inventory);
                    return false;
                }

                if (equipment != null)
                {
                    __result = equipment.GetFormattedDescription(inventoryDisplay?.inventory);
                    return false;
                }

                if (titleEquipment != null
                ) // This is only a half measure for betterui if the advanced tooltips for equipment is turned off this will fuck up and i dont care
                {
                    // TODO this also doesnt work very well without betterui, infact it probably throws an exception
                    __result = titleEquipment.GetFormattedDescription(inventoryDisplay?.inventory) +
                               __instance.overrideBodyText.Substring(Language
                                   .GetString(titleEquipment.EquipmentDef.descriptionToken).Length);
                    return false;
                }
            }
            catch (Exception e)
            {
                BubbetsItemsPlugin.Log.LogError(e);
            }

            return true;
        }

        [HarmonyILManipulator, HarmonyPatch(typeof(PageBuilder), nameof(PageBuilder.AddSimplePickup))]
        public static void PagebuilderPatch(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext( MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(out _),
                x => x.MatchLdfld<ItemDef>("descriptionToken"),
                x => x.MatchCall<Language>("GetString")
            );
            c.Index-=2;
            c.RemoveRange(2);
            c.EmitDelegate<Func<ItemDef, string>>(def =>
            {
                var item = ItemBase.Items.FirstOrDefault(x => x.ItemDef == def);
                return item != null ? item.GetFormattedDescription(null) : Language.GetString(def.descriptionToken);
            });
            
            c.GotoNext( MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(out _),
                x => x.MatchLdfld<EquipmentDef>("descriptionToken"),
                x => x.MatchCall<Language>("GetString")
            );
            c.Index-=2;
            c.RemoveRange(2); //TODO replace this with a dup on the equipmentdef and consume the old string to return if we dont find our item
            c.EmitDelegate<Func<EquipmentDef, string>>(def =>
            {
                var equipment = EquipmentBase.Equipments.FirstOrDefault(x => x.EquipmentDef == def);
                return equipment != null ? equipment.GetFormattedDescription() : Language.GetString(def.descriptionToken);
            });
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GenericNotification), nameof(GenericNotification.SetItem))]
        public static void NotifItemPostfix(GenericNotification __instance, ItemDef itemDef)
        {
            var item = ItemBase.Items.FirstOrDefault(x => x.ItemDef == itemDef);
            if (item != null && item.descInPickup.Value)
                __instance.descriptionText.token = item.GetFormattedDescription(null);
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(GenericNotification), nameof(GenericNotification.SetEquipment))]
        public static void NotifEquipmentPostfix(GenericNotification __instance, EquipmentDef equipmentDef)
        {
            var equipment = EquipmentBase.Equipments.FirstOrDefault(x => x.EquipmentDef == equipmentDef);
            if (equipment != null && equipment.descInPickup.Value)
                __instance.descriptionText.token = equipment.GetFormattedDescription();
        }

        /*
        //[HarmonyILManipulator, HarmonyPatch(typeof(AdvancedIcons), nameof(AdvancedIcons.EquipmentIcon_Update))]
        // Patched in awake because fuck me
        public static void FixBetterUIsGarbage(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext( MoveType.After,
                x => x.MatchLdflda<EquipmentIcon>("currentDisplayData"),
                x => x.MatchLdfld<EquipmentIcon.DisplayData>("equipmentDef"),
                x => x.MatchLdfld<EquipmentDef>("descriptionToken"),
                x => x.MatchCall<Language>("GetString") 
            );
            c.Index-=2;
            c.RemoveRange(2);
            c.EmitDelegate<Func<EquipmentDef, string>>(def =>
            {
                var equipment = EquipmentBase.Equipments.FirstOrDefault(x => x.EquipmentDef == def);
                return equipment != null ? equipment.GetFormattedDescription() : Language.GetString(def.descriptionToken);
            });
        }
        */
    }
}