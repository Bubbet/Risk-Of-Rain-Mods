using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Skills;

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
	}
}