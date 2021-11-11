using System;
using System.Collections.Generic;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using UnityEngine;

namespace Titanfall2Mod.SkillGeneration
{
    [CreateAssetMenu(menuName = "FamilyPairing")]
    public class FamilyPairing : ScriptableObject
    {
        public FamilyLinkage[] pairs;
        public bool setup;
        public SkillFamily[] families;
        public string[] keys;

        public Dictionary<string, SkillFamily> ToDict()
        {
            var dict = new Dictionary<string, SkillFamily>();
            /*foreach (var familyLinkage in pairs)
            {
                dict[familyLinkage.Key] = familyLinkage.Family;
            }*/
            for (var i = 0; i < families.Length; i++)
            {
                dict[keys[i]] = families[i];
            }
            return dict;
        }
        
        [Serializable]
        public struct FamilyLinkage
        {
            public string Key;
            public SkillFamily Family;
        }
    }
}