using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using R2API;
using R2API.MiscHelpers;
using RoR2;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace Bhop
{
    public static class Assets
    {
        internal static ManualLogSource Logger;

        public static GameObject BhopFeatherPrefab;
        public static Sprite BhopFeatherIcon;
        public static ItemDef BhopFeatherDef;
        public static (float @base, float add, int tier) BhopFeatherSettings;

        internal static void Init(ManualLogSource logger, (float @base, float add, int tier) settings)
        {
            Logger = logger;
            BhopFeatherSettings = settings;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Bhop.bhopfeather"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                
                BhopFeatherPrefab = bundle.LoadAsset<GameObject>("assets/bundledassets/bhopfeather/foot.prefab");
                //"Assets/Import/bhop/bhopfeather.prefab");
                BhopFeatherIcon = bundle.LoadAsset<Sprite>("assets/bundledassets/bhopfeather/icons/foot_rarity_" + BhopFeatherSettings.tier + ".png"); // TODO load different sprites for the current tier
                //"Assets/Import/bhop/bhopfeather_icon.png");
            }

            CreateBhopFeatherItem();
            AddLanguageTokens();
        }

        private static void CreateBhopFeatherItem()
        {
            var id = ScriptableObject.CreateInstance<ItemDef>();
            BhopFeatherDef = id;
            
            var rarityNum = ItemTier.Tier2;
            switch (BhopFeatherSettings.tier)
            {
                case 1:
                    rarityNum = ItemTier.Tier1;
                    break;
                case 2:
                    rarityNum = ItemTier.Tier2;
                    break;
                case 3:
                    rarityNum = ItemTier.Tier3;
                    break;
                case 4:
                    rarityNum = ItemTier.Lunar;
                    break;
                case 5:
                    rarityNum = ItemTier.Boss;
                    break;
            }

            id.name = "BHOPFEATHER";
            id.tier = rarityNum;
            id.pickupModelPrefab = BhopFeatherPrefab;
            id.pickupIconSprite = BhopFeatherIcon;
            id.nameToken = "Bunny Foot";
            id.pickupToken = "Your little feets start quivering.";
            id.descriptionToken = $"You gain the ability to bunny hop. \nAir control: {BhopFeatherSettings.@base} (+{BhopFeatherSettings.add} per stack)";
            id.loreToken = "haha source go brrrr";
            id.tags = new[] { ItemTag.Utility };


            var def = new[]
            {
                new ItemDisplayRule
                {
                    followerPrefab = BhopFeatherPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.12805F, 0.27567F, 0.09413F),
                    localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
                    localScale = new Vector3(0.4149F, 0.4149F, 0.4149F)
                }
            };
            /*
            childName = "CalfL";
            localPos = new Vector3(-0.0887F, 0.2933F, -0.01761F);
            localAngles = new Vector3(329.3528F, 183.1584F, 2.2867F);
            localScale = new Vector3(0.41143F, 0.41143F, 0.41143F)*/


            var feather = new CustomItem(BhopFeatherDef, new ItemDisplayRule[]{});

            SetupFootDisplayRules(feather.ItemDisplayRules, def);

            if (!ItemAPI.Add(feather))
            {
                BhopFeatherDef = null;
                Logger.LogError("Unable to add bhop item");
            }
        }

        private static void SetupFootDisplayRules(ItemDisplayRuleDict foot, ItemDisplayRule[] def)
        {
            
            foot["NemmandoBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(0.00132F, 0.00189F, -0.00017F),
                    localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
                    localScale = new Vector3(0.00473F, 0.00473F, 0.00473F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["NemesisEnforcerBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "Hammer",
                    localPos = new Vector3(0F, 0.01133F, -0.0109F),
                    localAngles = new Vector3(301.6936F, 180F, 180F),
                    localScale = new Vector3(0.015F, 0.015F, 0.015F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["CommandoBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(0.10964F, 0.2722F, 0.07702F),
                    localAngles = new Vector3(297.8961F, 337.7542F, 3.62604F),
                    localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["HuntressBody"] = def;

            foot["Bandit2Body"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighR",
                    localPos = new Vector3(-0.10279F, 0.35127F, 0.05551F),
                    localAngles = new Vector3(327.1544F, 5.32863F, 5.95733F),
                    localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["ToolbotBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(0.04776F, 1.88373F, 1.01038F),
                    localAngles = new Vector3(333.9735F, 281.9487F, 357.2159F),
                    localScale = new Vector3(2.37168F, 2.37168F, 2.37168F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["ExecutionerBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(-0.00189F, 0.00099F, -0.00019F),
                    localAngles = new Vector3(286.0008F, 198.6392F, 327.8065F),
                    localScale = new Vector3(0.00436F, 0.00436F, 0.00436F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["EnforcerBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(0.11708F, 0.24051F, 0.17615F),
                    localAngles = new Vector3(324.415F, 293.5852F, 355.0646F),
                    localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["EngiBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(0.16015F, 0.27443F, 0.0233F),
                    localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
                    localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["MageBody"] = def;

            foot["MercBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(0.17358F, 0.26745F, 0.04173F),
                    localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
                    localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["RobPaladinBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(-0.18165F, 0.3724F, -0.19136F),
                    localAngles = new Vector3(294.1706F, 158.5404F, 344.7267F),
                    localScale = new Vector3(0.53494F, 0.53494F, 0.53494F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["TreebotBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "PlatformBase",
                    localPos = new Vector3(-0.68795F, -0.12907F, 0.03073F),
                    localAngles = new Vector3(64.63686F, 352.9881F, 178.495F),
                    localScale = new Vector3(0.67997F, 0.67997F, 0.67997F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["LoaderBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(0.15646F, 0.28357F, 0.1099F),
                    localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
                    localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["CrocoBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(1.2091F, 0.51674F, 0.01721F),
                    localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
                    localScale = new Vector3(3.07934F, 3.07934F, 3.07934F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["CaptainBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(0.1283F, 0.24574F, 0.0152F),
                    localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
                    localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["Sniper"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(-0.13938F, -0.19064F, 0.00388F),
                    localAngles = new Vector3(87.36492F, 337.0182F, 144.9593F),
                    localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["HereticBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(0.28362F, 0.29826F, -0.07479F),
                    localAngles = new Vector3(358.4014F, 83.00351F, 56.10048F),
                    localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
                    followerPrefab = BhopFeatherPrefab
                }
            };
            
            foot["MinerBody"] = new[] 
            {
                new ItemDisplayRule
                {
                    childName = "LegL",
                    localPos = new Vector3(-0.00001F, 0.00156F, 0.00128F),
                    localAngles = new Vector3(313.4082F, 274.817F, 356.2425F),
                    localScale = new Vector3(0.00398F, 0.00398F, 0.00398F),
                    followerPrefab = BhopFeatherPrefab
                }
            };
            
            foot["CHEF"] = new[] // TODO 
            {
                new ItemDisplayRule
                {
                    childName = "LeftLeg",
                    localPos = new Vector3(-0.00232F, -0.00503F, 0.00649F),
                    localAngles = new Vector3(340F, 180F, 0F),
                    localScale = new Vector3(0.01294F, 0.01294F, 0.01294F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["HANDOverclockedBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(-0.52056F, 1.72429F, -0.37539F),
                    localAngles = new Vector3(348.6181F, 178.1265F, 2.98409F),
                    localScale = new Vector3(2.59279F, 2.59279F, 2.59279F),
                    followerPrefab = BhopFeatherPrefab
                }
            };

            foot["BanditReloadedBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighR",
                    localPos = new Vector3(0.00024F, 0.27766F, 0.14929F),
                    localAngles = new Vector3(326.1603F, 45.63481F, 355.6136F),
                    localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
                    followerPrefab = BhopFeatherPrefab
                }
            };
            
            // Enemys
            foot["ScavBody"] = new[]
            {
                new ItemDisplayRule
                {
                    childName = "ThighL",
                    localPos = new Vector3(2.51467F, 0.74224F, 0.46471F),
                    localAngles = new Vector3(302.574F, 340.1447F, 21.65013F),
                    localScale = new Vector3(3.77607F, 3.77607F, 3.77607F),
                    followerPrefab = BhopFeatherPrefab
                }
            };
            // need to do brother(mythrix) at some point
        }

        private static void AddLanguageTokens()
        {
            LanguageAPI.Add("BHOPFEATHER_NAME", "Bunny Foot");
            LanguageAPI.Add("BHOPFEATHER_PICKUP", "Your little feets start quivering.");
            LanguageAPI.Add("BHOPFEATHER_DESC", $"You gain the ability to bunny hop. \nAir control: {BhopFeatherSettings.@base} (+{BhopFeatherSettings.add} per stack)");
            LanguageAPI.Add("BHOPFEATHER_LORE", "haha source go brrrr");
        }
        
    }
    
}