using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using EntityStates;
using RoR2;
using RoR2.Skills;
using Titanfall2Mod.SkillStates.Boosts;
using Titanfall2Mod.SkillStates.Weapons;
using UnityEngine;

namespace Titanfall2Mod.SkillGeneration
{
    public static partial class SkillGenerator
    {
        private static readonly string[] RootTypes = {
            "PILOTWEAPON", "PILOTORDANANCE", "PILOTKIT", "PILOTBOOST", "TITANKIT"
        };
        private static readonly string[] LoadoutOrder = {
            "Utility", "Defense", "Special", "Primary"
        };
        private static readonly string[] ImplementedTitans = { "Ion" };

        public static readonly Dictionary<string, SkillFamily> Pairing = Assets.mainAssetBundle.LoadAsset<FamilyPairing>("FamilyPairs").ToDict();
        private static readonly UnlockableDef NotImplementedDef = Assets.mainContentPack.unlockableDefs[1];
        
        public static readonly List<SkillDef> AllSkills = new List<SkillDef>();
        public static readonly List<SkillFamily> TitanSpecificKits = new List<SkillFamily>();
        public static readonly List<EquipmentDef> TitanCores = new List<EquipmentDef>();
        
        private static readonly Regex BoostLimitRegex = new Regex(@"Limit (\d)");
        private static readonly Regex BoostRatioRegex = new Regex(@"(\d+)%");
        
        private static Type[] _types;
        
        public static readonly List<(float ratio, int count)> BoostRatios = new List<(float ratio, int count)>();
        
        public static void Init()
        {
            List<List<SkillFamily.Variant>> familyLists = LoadoutOrder.Select(loadout => new List<SkillFamily.Variant>()).ToList(); //loadoutOrder.Select(loadout => pairing["TITAN_" + loadout.ToUpper()].variants.ToList()).ToList();
            
            ref var variants = ref Pairing["TITAN"].variants;
            var variant = new List<SkillFamily.Variant>();
            foreach (var line in skillTypes["TITAN"])
            {
                var skillDef = GenerateTitanFromLine(line, familyLists);
                Debug.Log(skillDef.skillNameToken);
                bool implemented = ImplementedTitans.Any(x => skillDef.skillNameToken.StartsWith("BUB_TITAN_" + x.ToUpper()));
                variant.Add(new SkillFamily.Variant {skillDef = skillDef, viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false), unlockableDef = !implemented ? NotImplementedDef : null});
            }
            variants = variant.ToArray();

            var i = 0;
            foreach (var loadout in LoadoutOrder)
            {
                Pairing["TITAN_" + loadout.ToUpper()].variants = familyLists[i].ToArray();
                i++;
            }
            foreach (var rootType in RootTypes)
            {
                var list = new List<SkillFamily.Variant>();

                var list2 = skillTypes.First(x => x.Key.StartsWith(rootType));
                foreach (var listLine in list2.Value)
                {
                    (string listKey, string listName) = MakeTranslationToken(list2.Key, listLine);
                    var listSkill = MakeBaseSkill(listKey, listLine);
                    var implemented = true;

                    if (rootType != "TITAN_KIT")
                    {
                        var typ = FindType(GetCamelCaseFromSnake(listKey.Substring(list2.Key.Length + 4)));
                        listSkill.activationState = new SerializableEntityStateType(typ);
                        listSkill.activationStateMachineName = "Weapon";
                        if (typ == null)
                        {
                            implemented = false;
                        }
                    }

                    if (rootType == "PILOTBOOST")
                    {
                        int percent = int.Parse(BoostRatioRegex.Match(listLine).Groups[1].Value);
                        int count;
                        if (!int.TryParse(BoostLimitRegex.Match(listLine).Groups[1].Value, out count))
                            count = -1;
                        BoostRatios.Add((percent/100f, count));
                    }

                    if (rootType == "TITANKIT")
                    {
                        implemented = true;
                    }

                    list.Add(new SkillFamily.Variant
                    {
                        skillDef = listSkill, viewableNode = new ViewablesCatalog.Node(listSkill.skillNameToken, false), unlockableDef = !implemented ? NotImplementedDef : null
                    });
                }
                
                Pairing[rootType].variants = list.ToArray();
            }
            
            ApplySkillInfo();
            
            Assets.mainContentPack.equipmentDefs.Add(TitanCores.ToArray());
            Assets.mainContentPack.skillDefs.Add(AllSkills.ToArray());
        }

        private static void ApplySkillInfo()
        {
            foreach (var skillDef in AllSkills)
            {
                if (skillDef.activationState.stateType != null)
                {
                    if (typeof(ISkillStatDef).IsAssignableFrom(skillDef.activationState.stateType))
                        skillDef.activationState.stateType.InvokeMember("ApplyStats", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new[] {skillDef});
                }
            }
        }
        private static SkillDef GenerateTitanFromLine(string line, IReadOnlyList<List<SkillFamily.Variant>> familyLists)
        {
            (string key, string name) = MakeTranslationToken("TITAN", line);

            var skillDef = MakeBaseSkill(key, line);

            var keywordTokens = new List<string>();

            var i = 0;

            var list = skillTypes.First(x => x.Key.StartsWith("TITANLOADOUT") && x.Key.EndsWith(name)); // Gets TITANLOADOUT_ION
            foreach (var listLine in list.Value)
            {
                (string listKey, string listName) = MakeTranslationToken(list.Key, listLine);
                keywordTokens.Add(listKey + "_NAME");
                keywordTokens.Add(listKey + "_DESC");
                if (listName.EndsWith("CORE"))
                {
                    GenerateEquipment(listKey, listName);
                    continue;
                }
                var listSkill = MakeBaseSkill(listKey, listLine);

                listSkill.activationState = new SerializableEntityStateType(FindType(GetCamelCaseFromSnake(listKey.Substring("TITANLOADOUT".Length + 4))));
                listSkill.activationStateMachineName = "Weapon";
                familyLists[i].Add(new SkillFamily.Variant
                {
                    skillDef = listSkill, viewableNode = new ViewablesCatalog.Node(listSkill.skillNameToken, false)
                });
                i++;
            }

            skillDef.keywordTokens = keywordTokens.ToArray();
            
            List<SkillFamily.Variant> variants = new List<SkillFamily.Variant>();
            var list2 = skillTypes.First(x => x.Key.StartsWith("TITANSPECIFICKIT") && x.Key.EndsWith(name)); // Gets TITANSPECIFICKIT_ION
            foreach (var listLine in list2.Value)
            {
                (string listKey, string listName) = MakeTranslationToken(list2.Key, listLine);
                var listSkill = MakeBaseSkill(listKey, listName);
                
                variants.Add(new SkillFamily.Variant
                {
                    skillDef = listSkill, viewableNode = new ViewablesCatalog.Node(listSkill.skillNameToken, false)
                });
            }

            var family = ScriptableObject.CreateInstance<SkillFamily>();
            ((ScriptableObject) family).name = "BUB_TITAN_KIT_" + name; 
            family.variants = variants.ToArray();
            TitanSpecificKits.Add(family);

            return skillDef;
        }
        private static SkillDef MakeBaseSkill(string listKey, string listName)
        {
            SkillDef listSkill;
            if (listKey.Contains("WEAPON"))
            {
                Debug.Log("Weapon" + listKey);
                listSkill = ScriptableObject.CreateInstance<GunSkillDef>();
            }
            else
            {
                listSkill = ScriptableObject.CreateInstance<SkillDef>();
            }

            //listSkill.icon =
            ((ScriptableObject) listSkill).name = listKey + "_NAME";
            listSkill.skillName = listName;
            listSkill.skillNameToken = listKey + "_NAME";
            listSkill.skillDescriptionToken = listKey + "_DESC";

            /* This stuff displays to the right of the loadout window in the skills tab, not responsible for tooltips
            listSkill.keywordTokens = new[]
            {
                listKey + "_NAME", listKey + "_DESC"
            };*/
            AllSkills.Add(listSkill);
            
            return listSkill;
        }
        private static void GenerateEquipment(string listKey, string listName)
        {
            var equipment = ScriptableObject.CreateInstance<EquipmentDef>();
            equipment.name = listKey;  
            
            //equipment.pickupIconSprite =
            equipment.nameToken = listKey + "_NAME";
            equipment.descriptionToken = listKey + "_DESC";
            equipment.pickupToken = listKey + "_DESC";
            equipment.loreToken = listKey + "_DESC";

            equipment.appearsInMultiPlayer = false;
            equipment.appearsInSinglePlayer = false;
            equipment.canDrop = false;

            TitanCores.Add(equipment);
        }
        private static (string key, string name) MakeTranslationToken(string prefix, string line)
        {
            var sep = new[] {" - "};
            var splits = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            var key = $"{prefix}_{splits[0]}".ToUpper();
            key = key.Replace(" ", "_");
            Tokens.PreStartReg(key + "_NAME", splits[0]);
            Tokens.PreStartReg(key + "_DESC", splits[1]);
            
            return (Tokens.Prefix + key, splits[0].ToUpper());
        }
        private static string GetCamelCaseFromSnake(string snake)
        {
            snake = snake.Replace(" ", "_");
            snake = snake.Replace("-", "");
            var words = snake.ToLower().Split('_');
            string output = "";
            foreach (var word in words)
            {
                if (word == "") continue;
                var startletter = word[0].ToString().ToUpper();
                output += startletter + word.Substring(1);
            }
            return output;
        }
        private static Type FindType(string camelCaseName)
        {
            if (_types == null) _types = typeof(Titanfall2ModPlugin).Assembly.GetTypes();
            foreach (var type in _types)
            {
                if (type.Name.StartsWith(camelCaseName)) return type;
            }
            Titanfall2ModPlugin.logger.LogWarning("EntityState class not found for " + camelCaseName);
            return null;
        }
    }
}