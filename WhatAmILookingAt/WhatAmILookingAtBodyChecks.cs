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
			if (ItemCatalog.itemDefs.TryFirst(x => x.descriptionToken == body || x.pickupToken == body, out var itemDef))
				identifier = WhatAmILookingAtPlugin.GetIdentifier(itemDef);
		}

		public static void EquipmentDef(string body, ref string identifier)
		{
			if (EquipmentCatalog.equipmentDefs.TryFirst(x => x.descriptionToken == body || x.pickupToken == body, out var eq))
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
			if (SkillCatalog.allSkillDefs.TryFirst(x => x.skillDescriptionToken != "" && bodyText.StartsWith(Language.GetString(x.skillDescriptionToken)), out var skill))
				identifier = WhatAmILookingAtPlugin.GetIdentifier(skill);
		}

		public static void UnlockableDef(string bodyText, ref string identifier)
		{
			if (UnlockableCatalog.indexToDefTable.TryFirst(x => x.getUnlockedString() == bodyText || x.getHowToUnlockString() == bodyText, out var unlockable))
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
		public delegate void InWorldChecksDelegate(GameObject go, ref string identifier);
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
			var str = "";
			if (InWorldChecks == null || !go) return str;
			foreach (var dele in InWorldChecks.GetInvocationList())
			{
				(dele as InWorldChecksDelegate)?.Invoke(go, ref str);
				if (str != "") return str;
			}
			return str;
		}
		
		private static void SceneCheck(GameObject go, ref string identifier)
		{
			// test if the object is from the scene
			if (!go.IsInScene()) return;
			
			var scene = SceneManager.GetActiveScene();
			var def = SceneCatalog.GetSceneDef(SceneCatalog.FindSceneIndex(scene.name));
			if (def == null) return;
			foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack => pack.sceneDefs.Contains(def)))
			{
				identifier = pack.identifier;
				return;
			}
		}

		public static void InteractableCheck(GameObject go, ref string identifier)
		{
			var netIdentity = go.GetComponent<NetworkIdentity>();
			if (netIdentity == null) return;
			var netId = netIdentity.assetId;
			foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack => pack.networkedObjectPrefabs.Any(x =>
			{
				var xi = x.GetComponent<NetworkIdentity>();
				var xId = xi.assetId;
				return xId.Equals(netId);
			})))
			{
				identifier = pack.identifier;
				return;
			}
		}

		public static void PickupCheck(GameObject go, ref string identifier)
		{
			var display = go.GetComponent<GenericPickupController>();
			var shop = go.GetComponent<ShopTerminalBehavior>();
			if (!display && !shop || shop.pickupIndexIsHidden) return;
			var def = PickupCatalog.GetPickupDef(display ? display.pickupIndex : shop.pickupIndex);
			if (def == null) return;

			if (def.itemIndex != ItemIndex.None)
			{
				var itemDef = ItemCatalog.GetItemDef(def.itemIndex);
				foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack => pack.itemDefs.Contains(itemDef)))
				{
					identifier = pack.identifier;
					return;
				}
			}

			if (def.equipmentIndex != EquipmentIndex.None)
			{
				var equipmentDef = EquipmentCatalog.GetEquipmentDef(def.equipmentIndex);
				foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack => pack.equipmentDefs.Contains(equipmentDef)))
				{
					identifier = pack.identifier;
					return;
				}
			}

			if (def.artifactIndex != ArtifactIndex.None)
			{
				var artifactDef = ArtifactCatalog.GetArtifactDef(def.artifactIndex);
				foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack => pack.artifactDefs.Contains(artifactDef)))
				{
					identifier = pack.identifier;
					return;
				}
			}

			if (def.miscPickupIndex != MiscPickupIndex.None)
			{
				var miscPickupDef = MiscPickupCatalog.miscPickupDefs.First(x => x.miscPickupIndex == def.miscPickupIndex);
				foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack => pack.miscPickupDefs.Contains(miscPickupDef)))
				{
					identifier = pack.identifier;
					return;
				}
			}
		}

		private static void BodyCheck(GameObject go, ref string identifier)
		{
			var body = go.GetComponent<CharacterBody>();
			if (!body) return;
			var prefab = BodyCatalog.GetBodyPrefab(body.bodyIndex);
			if (!prefab) return;
			foreach (var pack in ContentManager.allLoadedContentPacks.Where(pack => pack.bodyPrefabs.Contains(prefab)))
			{
				identifier = pack.identifier;
				break;
			}
		}
	}
}