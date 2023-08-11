using System.Linq;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace WhatAmILookingAt
{
    public static class WhatAmILookingAtBodyChecks
    {
        public static void ItemDef(string body, ref string identifier)
        {
            if (ItemCatalog.itemDefs.TryFirst(x => x.descriptionToken == body || x.pickupToken == body,
                    out var itemDef))
                identifier = WhatAmILookingAtPlugin.GetIdentifier(itemDef);
        }

        public static void EquipmentDef(string body, ref string identifier)
        {
            if (EquipmentCatalog.equipmentDefs.TryFirst(x => x.descriptionToken == body || x.pickupToken == body,
                    out var eq))
                identifier = WhatAmILookingAtPlugin.GetIdentifier(eq);
        }

        public static void ArtifactDef(string body, ref string identifier)
        {
            if (ArtifactCatalog.artifactDefs.TryFirst(x => x.descriptionToken == body, out var arti))
                identifier = WhatAmILookingAtPlugin.GetIdentifier(arti);
        }

        public static void SkillDef(string body, ref string identifier)
        {
            if (SkillCatalog.allSkillDefs.TryFirst(x => x.skillDescriptionToken == body, out var skill))
                identifier = WhatAmILookingAtPlugin.GetIdentifier(skill);
        }

        public static void ExpansionDef(string body, ref string identifier)
        {
            if (ExpansionCatalog.expansionDefs.TryFirst(x => x.descriptionToken == body, out var expansionDef))
                identifier = WhatAmILookingAtPlugin.GetIdentifier(expansionDef);
        }

        public static void Register(ref WhatAmILookingAtPlugin.StringTest BodyChecks)
        {
            BodyChecks += ItemDef;
            BodyChecks += EquipmentDef;
            BodyChecks += ArtifactDef;
            BodyChecks += SkillDef;
            BodyChecks += ExpansionDef;
        }
    }

    public static class WhatAmILookingAtBodyTextChecks
    {
        public static void SkillDef(string bodyText, ref string identifier)
        {
            if (SkillCatalog.allSkillDefs.TryFirst(
                    x => x.skillDescriptionToken != "" &&
                         bodyText.StartsWith(Language.GetString(x.skillDescriptionToken)), out var skill))
                identifier = WhatAmILookingAtPlugin.GetIdentifier(skill);
        }

        public static void UnlockableDef(string bodyText, ref string identifier)
        {
            if (UnlockableCatalog.indexToDefTable.TryFirst(
                    x => x.getUnlockedString() == bodyText || x.getHowToUnlockString() == bodyText, out var unlockable))
                identifier = WhatAmILookingAtPlugin.GetIdentifier(unlockable);
        }

        public static void Register(ref WhatAmILookingAtPlugin.StringTest BodyTextChecks)
        {
            BodyTextChecks += SkillDef;
            BodyTextChecks += UnlockableDef;
        }
    }

    public static class WailaInWorldChecks
    {
        public delegate bool InWorldChecksDelegate(GameObject go, out string identifier);

        public static event InWorldChecksDelegate InWorldChecks;

        public static void Register()
        {
            InWorldChecks += BodyCheck;
            InWorldChecks += InteractableCheck;
            InWorldChecks += PickupCheck;
            InWorldChecks += SceneCheck;
        }

        public static string GetIdentifier(GameObject go)
        {
            if (InWorldChecks == null || !go) return "";
            foreach (var dele in InWorldChecks.GetInvocationList())
            {
                if (dele is InWorldChecksDelegate deleg && deleg.Invoke(go, out var str))
                    return str;
            }

            return "";
        }

        public static bool SceneCheck(GameObject go, out string identifier)
        {
            // test if the object is from the scene
            if (!go.IsInScene())
            {
                identifier = null;
                return false;
            }

            var scene = SceneManager.GetActiveScene();
            var def = SceneCatalog.GetSceneDef(SceneCatalog.FindSceneIndex(scene.name));
            if (def == null)
            {
                identifier = null;
                return false;
            }

            foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack => pack.sceneDefs.Contains(def)))
            {
                identifier = pack.identifier;
                return true;
            }

            identifier = null;
            return false;
        }

        public static bool InteractableCheck(GameObject go, out string identifier)
        {
            var current = go.transform;
            while (current.parent)
            {
                current = current.parent;
            }

            if (current.name == "PortalDialerEvent")
            {
                identifier = "RoR2.BaseContent"; // We're all about professional solutions here
                return true;
            }

            var netIdentity = current.GetComponent<NetworkIdentity>();
            if (netIdentity == null)
            {
                identifier = null;
                return false;
            }

            var netId = netIdentity.assetId;
            foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack => pack.networkedObjectPrefabs.Any(x =>
                     {
                         var xi = x.GetComponent<NetworkIdentity>();
                         var xId = xi.assetId;
                         return xId.Equals(netId);
                     })))
            {
                identifier = pack.identifier;
                return true;
            }

            identifier = null;
            return false;
        }

        public static bool PickupCheck(GameObject go, out string identifier)
        {
            var display = go.GetComponent<GenericPickupController>();
            var shop = go.GetComponent<ShopTerminalBehavior>();
            if (!display && !shop || shop && shop.pickupIndexIsHidden)
            {
                identifier = null;
                return false;
            }

            var def = PickupCatalog.GetPickupDef(display ? display.pickupIndex : shop.pickupIndex);
            if (def == null)
            {
                identifier = null;
                return false;
            }

            if (def.itemIndex != ItemIndex.None)
            {
                var itemDef = ItemCatalog.GetItemDef(def.itemIndex);
                foreach (var pack in
                         ContentManager.allLoadedContentPacks.Where(pack => pack.itemDefs.Contains(itemDef)))
                {
                    identifier = pack.identifier;
                    return true;
                }
            }

            if (def.equipmentIndex != EquipmentIndex.None)
            {
                var equipmentDef = EquipmentCatalog.GetEquipmentDef(def.equipmentIndex);
                foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack =>
                             pack.equipmentDefs.Contains(equipmentDef)))
                {
                    identifier = pack.identifier;
                    return true;
                }
            }

            if (def.artifactIndex != ArtifactIndex.None)
            {
                var artifactDef = ArtifactCatalog.GetArtifactDef(def.artifactIndex);
                foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack =>
                             pack.artifactDefs.Contains(artifactDef)))
                {
                    identifier = pack.identifier;
                    return true;
                }
            }

            if (def.miscPickupIndex != MiscPickupIndex.None)
            {
                var miscPickupDef =
                    MiscPickupCatalog.miscPickupDefs.First(x => x.miscPickupIndex == def.miscPickupIndex);
                foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack =>
                             pack.miscPickupDefs.Contains(miscPickupDef)))
                {
                    identifier = pack.identifier;
                    return true;
                }
            }

            identifier = null;
            return false;
        }

        public static bool BodyCheck(GameObject go, out string identifier)
        {
            var body = go.GetComponent<CharacterBody>();
            if (!body)
            {
                identifier = null;
                return false;
            }

            var prefab = BodyCatalog.GetBodyPrefab(body.bodyIndex);
            if (!prefab)
            {
                identifier = null;
                return false;
            }

            foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack => pack.bodyPrefabs.Contains(prefab)))
            {
                identifier = pack.identifier;
                return true;
            }

            identifier = null;
            return false;
        }

        public static bool EliteCheck(GameObject gObject, out string name, out string identifier)
        {
            var body = gObject.GetComponent<CharacterBody>();
            var def = body.activeBuffsList.Select(BuffCatalog.GetBuffDef).FirstOrDefault(x => x.isElite);
            if (def && def.eliteDef)
            {
                name = def.eliteDef.modifierToken;
                foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack => pack.eliteDefs.Contains(def.eliteDef)))
                {
                    identifier = pack.identifier;
                    return true;
                }
            }

            name = null;
            identifier = null;
            return false;
        }
    }
}