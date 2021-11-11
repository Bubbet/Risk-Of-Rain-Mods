using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using EntityStates.Bandit2.Weapon;
using HarmonyLib;
using KinematicCharacterController;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using Titanfall2Mod.SkillGeneration;
using UnityEngine;
// ReSharper disable InconsistentNaming

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

#pragma warning disable CS0122
[assembly: IgnoresAccessChecksTo("RoR2")]
namespace Titanfall2Mod
{
    [HarmonyPatch]
    public class HarmonyPatches
    {
        /*[HarmonyPostfix, HarmonyPatch(typeof(Language), nameof(Language.Init))]
        public static void DoTokens()
        {
            Tokens.Init();
        }*/
        
        /*
        public static Action<GameObject, RaycastHit> onCollisionEnter;
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(KinematicCharacterMotor), "CharacterCollisionsSweep")]
        public static void CollisionExposing(ref int __result, ref RaycastHit closestHit,
            KinematicCharacterMotor __instance)
        {
            if (__result == 0) return;
            onCollisionEnter?.Invoke(__instance.gameObject, closestHit);
        }
        */

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UserProfile), "OnLogin")]
        public static void OnLogin(Loadout ___loadout, UserProfile __instance)
        {
            __instance.onLoadoutChanged += TitanKitsLoadout.UserProfileOnLoadoutChange;
            TitanKitsLoadout.loadout = ___loadout;
            TitanKitsLoadout.UserProfileOnLoadoutChange(false);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterMaster), "GetBodyObject")]
        public static bool CheckForTitanBody(CharacterMaster __instance, ref GameObject __result)
        {
            // ReSharper disable twice Unity.NoNullPropagation
            try
            {
                var pm = __instance?.GetComponent<PilotMaster>()?.TitanMaster?.master?.GetBody()?.gameObject;
                if (pm == null) return true;

                if (pm.GetComponent<TitanBehavior>()?.pilotSeat?.hasPassenger ?? false)
                {
                    __result = pm;
                    return false;
                }
            }
            catch (NullReferenceException)
            {
            }

            return true;
        }

        // Hook connected in awake
        [HarmonyILManipulator, HarmonyPatch(typeof(LoadoutPanelController.Row), "FromSkillSlot")]
        public static void FromSkillSlot(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchLdstr("LOADOUT_SKILL_MISC")
            );
            c.Emit(OpCodes.Ldarg_3);
            c.EmitDelegate<Func<string, GenericSkill, string>>((s, skill) =>
            {
                if (skill.skillName == "TITAN_SPECIFIC_KIT") return (skill.skillFamily as ScriptableObject).name.ToUpper();
                return skill.skillName ?? s;
            });
        }

        [HarmonyILManipulator, HarmonyPatch(typeof(LoadoutPanelController.Row), "UpdateHighlightedChoice")]
        public static void FixIndexOutOfRangeInUpdateHighlightedChoice(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<LoadoutPanelController.Row>("findCurrentChoice"),
                x => x.MatchLdloc(out var _),
                x => x.OpCode == OpCodes.Callvirt,
                x => x.MatchStloc(out var _)
            );
            c.Index--;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, LoadoutPanelController.Row, int>>((i, row) =>
            {
                if (i >= row.buttons.Count)
                {
                    return row.buttons.Count - 1;
                }
                return i;
            });
        }

        [HarmonyILManipulator, HarmonyPatch(typeof(CharacterSelectController), "RebuildLocal")]
        public static void FixOutOfRangeRebuildLocal(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdloc(out var _),
                x => x.MatchCallvirt<GenericSkill>("get_skillFamily"),
                x => x.MatchLdfld<SkillFamily>("variants"),
                x => x.MatchLdloc(out var _),
                x => x.MatchLdelema<SkillFamily.Variant>(),
                x => x.MatchLdfld<SkillFamily.Variant>("skillDef"),
                x => x.MatchStloc(out var _)
            );
            c.Index += 3;
            c.Emit(OpCodes.Dup);
            c.Index++;
            c.EmitDelegate<Func<SkillFamily.Variant[], int, int>>((variants, i) => i >= variants.Length ? variants.Length - 1 : i);
        }

        // Hook connected in awake
        [HarmonyILManipulator, HarmonyPatch(typeof(Loadout.BodyLoadoutManager.BodyLoadout), "ToXml")]
        public static void UpdateSkillName(ILContext il)
        {
            var c = new ILCursor(il);
            int index = default;
            
            c.GotoNext(
                x => x.MatchLdloc(out int bodyInfo),
                x => x.MatchLdfld<Loadout.BodyLoadoutManager.BodyInfo>("skillFamilyIndices"),
                x => x.MatchLdloc(out index), 
                x => x.MatchLdelemI4(),
                x => x.MatchDup()
            );

            c.GotoNext(
                x => x.MatchDup(),
                x => x.MatchCall("RoR2.Skills.SkillCatalog", "GetSkillFamily"),
                x => x.MatchStloc(out int _)
            );

            c.Index++;
            c.Remove();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, index);
            c.EmitDelegate<Func<int, Loadout.BodyLoadoutManager.BodyLoadout, int, SkillFamily>>((skillFamilyIndex, loadout, i) =>
            {
                if (loadout.bodyIndex == Prefabs.pilotBodyPrefab.bodyIndex)
                {
                    if (i == 5)
                    {
                        var whichTitan = loadout.skillPreferences[4];
                        var newFamily = SkillGenerator.TitanSpecificKits[(int) whichTitan];
                        return newFamily;
                    }
                }
                return SkillCatalog.GetSkillFamily(skillFamilyIndex);
            });

            c.GotoNext(
                x => x.MatchCall("RoR2.Skills.SkillCatalog", "GetSkillFamilyName"),
                x => x.MatchStloc(out int oldSkillFamilyName)
            );

            c.Remove();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, index);
            c.EmitDelegate<Func<int, Loadout.BodyLoadoutManager.BodyLoadout, int, string>>((oldSkillFamilyIndex, loadout, ind) =>
            {
                if (loadout.bodyIndex == Prefabs.pilotBodyPrefab.bodyIndex)
                {
                    if (ind == 5)
                    {
                        
                        var whichTitan = loadout.skillPreferences[4];
                        var newFamily = SkillGenerator.TitanSpecificKits[(int) whichTitan];
                        var name = ((ScriptableObject) newFamily).name;
                        return name;
                    }
                }
                return SkillCatalog.GetSkillFamilyName(oldSkillFamilyIndex);
            });
            c.GotoNext( // variantName Match
                x => x.MatchLdloc(out int oldSkillFamily),
                x => x.MatchLdarg(0),
                x => true,
                //x => x.MatchLdfld("BodyLoadout", "skillPreferences"),
                x => x.MatchLdloc(out int i),
                x => x.MatchLdelemU4(),
                x => x.MatchCallvirt<SkillFamily>("GetVariantName")
            );
            c.Index+=2;
            c.Remove();
            c.Index++;
            c.Remove();
            c.Remove();
            c.EmitDelegate<Func<SkillFamily, Loadout.BodyLoadoutManager.BodyLoadout, int, string>>((oldFamily, loadout, i) =>
            {
                var ind = (int) loadout.skillPreferences[i];
                if (ind >= oldFamily.variants.Length) ind = oldFamily.variants.Length - 1; 
                var oldName = oldFamily.GetVariantName(ind);
                return oldName;
            });
        }
        [HarmonyILManipulator, HarmonyPatch(typeof(Loadout.BodyLoadoutManager.BodyLoadout), "FromXml")]
        public static void UpdateSkillFamily(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdloc(out var _),
                x => x.MatchLdloca(out var _),
                x => x.OpCode == OpCodes.Call,
                x => x.MatchStloc(out var _)
            );
            
            c.Emit(OpCodes.Ldarg_0);
            c.Index += 2;
            c.Remove();
            c.EmitDelegate<dele>((Loadout.BodyLoadoutManager.BodyLoadout loadout, string requestedSkillFamilyName,
                ref GenericSkill[] prefabSkillSlots) =>
            {
                for (int index = 0; index < prefabSkillSlots.Length; ++index)
                {
                    if (SkillCatalog.GetSkillFamilyName(prefabSkillSlots[index].skillFamily.catalogIndex).Equals(requestedSkillFamilyName, StringComparison.Ordinal))
                        return index;
                }

                if (requestedSkillFamilyName.StartsWith("BUB_TITAN_KIT_"))
                    return 5;

                return -1;
            });

            c.GotoNext(
                x => x.MatchLdloc(out int locals1),
                x => true, // matchfld of some fucking weird ass type that isnt even defined
                x => x.MatchLdloc(6),
                x => x.MatchLdelemRef(),
                x => x.MatchCallvirt<GenericSkill>("get_skillFamily"),
                x => x.MatchLdloc(5),
                x => x.MatchCallvirt<SkillFamily>("GetVariantIndex"),
                x => x.MatchStloc(7)
            );
            c.Emit(OpCodes.Ldarg_0);
            c.Index += 3;
            c.Remove();
            c.Remove();
            c.EmitDelegate<Func<Loadout.BodyLoadoutManager.BodyLoadout, GenericSkill[], int, SkillFamily>>(((loadout, skills, skillSlotIndex) =>
            {
                if (loadout.bodyIndex == Prefabs.pilotBodyPrefab.bodyIndex)
                {
                    if (skillSlotIndex == 5)
                    {
                        var whichTitan = loadout.skillPreferences[4];
                        var newFamily = SkillGenerator.TitanSpecificKits[(int) whichTitan];
                        skills[5]._skillFamily = newFamily; 
                        return newFamily;
                    }
                }

                return skills[skillSlotIndex].skillFamily;
            }));
        }

        delegate int dele (Loadout.BodyLoadoutManager.BodyLoadout bodyLoadout, string requestedSkillFamilyName, ref GenericSkill[] prefabSkillSlots);

        [HarmonyILManipulator, HarmonyPatch(typeof(Run), "SetupUserCharacterMaster")]
        public static void PatchMasterChoice(ILContext il)
        {
            var c = new ILCursor(il);
            var st = "";
            c.GotoNext(
                x => x.MatchLdstr(out st),
                x => x.MatchCall<Resources>("Load")
            );
            c.RemoveRange(2);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<NetworkUser, GameObject>>(user =>
            {
                var bodyIndex = BodyCatalog.GetBodyPrefab(user.bodyIndexPreference).GetComponent<CharacterBody>().bodyIndex;
                try
                {
                    var mast = MasterCatalog.allMasters.First(master =>
                        master.bodyPrefab.GetComponent<CharacterBody>().bodyIndex == bodyIndex && !master.name.Contains("Monster")).gameObject;
                    return mast;
                }
                catch (InvalidOperationException) { }
                return Resources.Load<GameObject>(st);
            });
        }
        
        
        /*
        [HarmonyPostfix, HarmonyPatch(typeof(AkWwiseInitializationSettings), nameof(AkWwiseInitializationSettings.InitializeSoundEngine))]
        public static void DoSound() => Assets.LoadSoundBank();
        */
    }
}
