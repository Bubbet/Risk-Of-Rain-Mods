using System.Linq;
using System.Reflection;
using RoR2;
using RoR2.UI;
using Titanfall2Mod.SkillGeneration;
using UnityEngine;

namespace Titanfall2Mod
{
    public static class TitanKitsLoadout
    {
        private static readonly GenericSkill[] Skills = Prefabs.pilotBodyPrefab.GetComponents<GenericSkill>();
        public static Loadout loadout;

        private static LoadoutPanelController _cachedPanelController;

        private static GenericSkill _cachedTitanSkillFamilySkill;

        private static GenericSkill _cachedSkillDefSkillName;

        public static FieldInfo SkillFamily =>
            typeof(GenericSkill).GetField("_skillFamily", BindingFlags.NonPublic | BindingFlags.Instance);

        private static MethodInfo Rebuild =>
            typeof(LoadoutPanelController).GetMethod("Rebuild", BindingFlags.NonPublic | BindingFlags.Instance);

        private static LoadoutPanelController PanelController
        {
            get
            {
                if (!_cachedPanelController)
                    _cachedPanelController = GameObject.Find("LoadoutPanel").GetComponent<LoadoutPanelController>();
                return _cachedPanelController;
            }
        }

        private static GenericSkill TitanSkillFamilySkill
        {
            get
            {
                if (!_cachedTitanSkillFamilySkill)
                    _cachedTitanSkillFamilySkill = Skills.First(x => x.skillName == "TITAN_SPECIFIC_KIT");
                return _cachedTitanSkillFamilySkill;
            }
        }

        public static GenericSkill SkillDefSkillName
        {
            get
            {
                if (!_cachedSkillDefSkillName)
                    _cachedSkillDefSkillName = Skills.First(x => x.skillName == "BUB_TITAN_SKILL");
                return _cachedSkillDefSkillName;
            }
        }

        private static MethodInfo meth =
            typeof(CharacterSelectController).GetMethod("GetSelectedSurvivorIndexFromBodyPreference", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void UserProfileOnLoadoutChange(bool inLobby)
        {
            if (inLobby)
            {
                var go = GameObject.Find("CharacterSelectUI");
                if (go == null) return;
                var comp = go.GetComponent<CharacterSelectController>();
                if (comp == null) return;
                if ((int) meth.Invoke(comp, null) != (int) Assets.mainContentPack.survivorDefs[0].survivorIndex) return;
            }
            
            var whichTitan =
                loadout.bodyLoadoutManager.GetSkillVariant(Prefabs.pilotBodyPrefab.bodyIndex,
                    4);
            SkillFamily.SetValue(TitanSkillFamilySkill, SkillGenerator.TitanSpecificKits[(int) whichTitan]);
            if (inLobby) Rebuild.Invoke(PanelController, null);
            
            /*
            var whichTitan =
                loadout.bodyLoadoutManager.GetSkillVariant(Prefabs.pilotBodyPrefab.bodyIndex,
                    4); // 4 is the only hard coded value its in reference to skill 5 in the component list indexed from 1
            var newName = SkillDefSkillName.skillFamily.variants[whichTitan].skillDef.skillName;
            var newFamily =
                Assets.mainContentPack.skillFamilies.First(x => x.defaultSkillDef.skillName.StartsWith(newName));
            SkillFamily.SetValue(TitanSkillFamilySkill, newFamily);

            if (inLobby) Rebuild.Invoke(PanelController, null);*/
        }

        public static void UserProfileOnLoadoutChange()
        {
            UserProfileOnLoadoutChange(true);
        }
    }
}