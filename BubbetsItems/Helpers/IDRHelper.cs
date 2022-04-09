using System.Collections.Generic;
using RoR2;

// ReSharper disable InconsistentNaming 

namespace BubbetsItems.Helpers
{
	public enum VanillaCharacterIDRS
	{
		Commando,
		Huntress,
		Bandit,
		Mult,
		Engineer,
		Artificer,
		Mercenary,
		Rex,
		Loader,
		Acrid,
		Captain,
		RailGunner,
		VoidFiend,
		Scavenger,
		EngineerTurret,
	}
	public static class IDRHelper
	{
		public static Dictionary<VanillaCharacterIDRS, string> enumToBodyObjName = new()
		{
			[VanillaCharacterIDRS.Commando] = "CommandoBody",
			[VanillaCharacterIDRS.Bandit] = "Bandit2Body",
			[VanillaCharacterIDRS.Captain] = "CaptainBody",
			[VanillaCharacterIDRS.Acrid] = "CrocoBody",
			[VanillaCharacterIDRS.Engineer] = "EngiBody",
			[VanillaCharacterIDRS.Huntress] = "HuntressBody",
			[VanillaCharacterIDRS.Loader] = "LoaderBody",
			[VanillaCharacterIDRS.Artificer] = "MageBody",
			[VanillaCharacterIDRS.Mercenary] = "MercBody",
			[VanillaCharacterIDRS.RailGunner] = "RailgunnerBody",
			[VanillaCharacterIDRS.Scavenger] = "ScavBody",
			[VanillaCharacterIDRS.Mult] = "ToolbotBody",
			[VanillaCharacterIDRS.Rex] = "TreebotBody",
			[VanillaCharacterIDRS.EngineerTurret] = "Turret1Body",
			[VanillaCharacterIDRS.VoidFiend] = "VoidSurvivorBody"
		};

		public static Dictionary<VanillaCharacterIDRS, ItemDisplayRuleSet> bodyReference = new();
		
		public static ItemDisplayRuleSet? GetRuleSet(VanillaCharacterIDRS which)
		{
			if (bodyReference.ContainsKey(which)) return bodyReference[which];

			var body = BodyCatalog.FindBodyPrefab(enumToBodyObjName[which]);
			if (!body) return null;
			bodyReference.Add(which, body!.GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>().itemDisplayRuleSet);

			return bodyReference[which];
		}
	}
}