using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace ModItemsChest
{
    [HarmonyPatch]
    class DisableModItemsInNormalChests
    {
        [HarmonyILManipulator, HarmonyPatch(typeof(Run), "BuildDropTable")]
        public static void BuildDropTable(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdloc(2),
                x => x.MatchLdcI4(9),
                x => x.MatchCallvirt<ItemDef>("DoesNotContainTag")
            );
            c.Index += 3;
            var br = c.Next;
            c.Index++;
            c.Emit(OpCodes.Ldloc_2);
            c.EmitDelegate<Func<ItemDef, bool>>(def =>
            {
                FillVanillaItems();
                if (!VanillaItemIndexes.Contains(def.itemIndex))
                {
                    if (_moddedFilled) return true;//false;
                    Debug.Log("Found modded item: " + def.name);
                    if (ModdedItemDefs.Contains(def)) _moddedFilled = true;
                    ModdedItemDefs.Add(def);
                    return true; //false;
                }

                // TODO return false if the item is modded, but also add it to the modded list instead
                return true;
            });
            c.Emit(OpCodes.Brfalse, br.Operand);
        }

        /*[HarmonyILManipulator, HarmonyPatch(typeof(SceneDirector), "GenerateInteractableCardSelection")]
        public static void InjectCard(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(x => x.MatchLdsfld<SceneDirector>("onGenerateInteractableCardSelection"), x => x.MatchDup());
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate<InjectCardDele>(selection =>
            {
                Debug.Log("\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\nMakingModItemChest");
                selection.AddCard(0, ModItemsChestPlugin.get_moddedChestDirectorCard(selection));
            });
            // Needed to inject our card before the action is called so sacrifice can see it and disable it
        }

        delegate void InjectCardDele(DirectorCardCategorySelection selection);*/

        /*
        [HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), "HandleDamageDealt")]
        public static void HealthComponent_handleDamageDealt1(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdloc(2),
                x => x.MatchCallvirt<TeamComponent>("get_teamIndex"),
                x => x.MatchLdloc(0),
                x => x.MatchLdfld<DamageDealtMessage>("damageColorIndex"),
                x => x.MatchCallvirt<DamageNumberManager>("SpawnDamageNumber")
            );
            c.Index += 4;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Test>((hc) =>
            {
                Debug.Log("Checking for cloak");
                if ((bool)hc.body?.hasCloakBuff)
                {
                    return false;
                }
                return true;
            });
            var ind = c.Index;
            c.GotoNext(x => x.MatchLdloc(0), x => x.MatchCall<GlobalEventManager>("ClientDamageNotified"));
            var br = c.Prev;
            c.Index = ind;
            Debug.Log(c);
            c.Emit(OpCodes.Brtrue, br);
        }

        delegate bool Test(HealthComponent hc);*/
        
        public static string[] whitelistedChests = {"Chest1(Clone)", "CategoryChestDamage(Clone)", "CategoryChestHealing(Clone)", "CategoryChestUtility(Clone)"};
        
        [HarmonyPrefix, HarmonyPatch(typeof(ChestBehavior), "PickFromList")]
        public static void PickFromList(ref List<PickupIndex> dropList, ChestBehavior __instance)
        {
            if (!whitelistedChests.Contains(__instance.gameObject.name)) return;
            //if (PickupCatalog.GetPickupDef(dropList[0]).equipmentIndex != EquipmentIndex.None) return;
            dropList = dropList.Where(x => VanillaPickupIndexes.Contains(x)).ToList();
        }

        public static void FillVanillaItems()
        {
            if (_vanillaFilled) return;
            foreach (var field in typeof(RoR2Content.Items).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var index = ((ItemDef) field.GetValue(null)).itemIndex;
                VanillaItemIndexes.Add(index);
                VanillaPickupIndexes.Add(PickupCatalog.FindPickupIndex(index));
            }
            _vanillaFilled = true;
        }

        private static bool _vanillaFilled;
        public static bool _moddedFilled;
        public static readonly List<ItemIndex> VanillaItemIndexes = new List<ItemIndex>();
        public static readonly List<ItemDef> ModdedItemDefs = new List<ItemDef>();
        public static readonly List<PickupIndex> VanillaPickupIndexes = new List<PickupIndex>();
    }
}