using System.Collections.Generic;
using Rewired;
using RoR2;
using UnityEngine;
using Logger = UnityEngine.Logger;

namespace BubbetsItems
{
	public enum VanillaIDRS
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
		EngineerWalkerTurret,
		EngineerBeamTurret,
		Heretic
	}
	
	public enum ModdedIDRS
	{
		AurelionSol,
		Natsu,
		Goku,
		Trunks,
		Vegeta,
		Aatrox,
		Bomber,
		SynergiesKing,
		Sett,
		Katarina,
		Jinx,
		Enforcer,
		NemesisEnforcer,
		Gunslinger,
		Paladin,
		Miner,
		House,
		Lagann,
		GurrenLagann,
		SYBeholder,
		Tracer,
		Samus,
		Myst,
		CHEF,
		BanditReloaded,
		MegamanXV3,
		MegamanCM,
		Phoenix,
		Ganondorf,
		Scout,
		Ditto,
		Holomancer,
		RedMist,
		Yasso,
		Yasuo,
		Dancer,
		Wisp,
		Deku,
		Soldier,
		Arbiter,
		Nemmando,
		Executioner,
		Hand
	}
	public static class IDRHelper
	{
		public static Dictionary<VanillaIDRS, string> enumToBodyObjName = new()
		{
			[VanillaIDRS.Commando] 				= "CommandoBody",
			[VanillaIDRS.Bandit] 				= "Bandit2Body",
			[VanillaIDRS.Captain] 				= "CaptainBody",
			[VanillaIDRS.Acrid] 				= "CrocoBody",
			[VanillaIDRS.Engineer] 				= "EngiBody",
			[VanillaIDRS.Huntress] 				= "HuntressBody",
			[VanillaIDRS.Loader] 				= "LoaderBody",
			[VanillaIDRS.Artificer] 			= "MageBody",
			[VanillaIDRS.Mercenary] 			= "MercBody",
			[VanillaIDRS.RailGunner] 			= "RailgunnerBody",
			[VanillaIDRS.Scavenger] 			= "ScavBody",
			[VanillaIDRS.Mult] 					= "ToolbotBody",
			[VanillaIDRS.Rex] 					= "TreebotBody",
			[VanillaIDRS.EngineerTurret] 		= "EngiTurretBody",
			[VanillaIDRS.EngineerWalkerTurret] 	= "EngiWalkerTurretBody",
			[VanillaIDRS.EngineerBeamTurret] 	= "EngiBeamTurretBody",
			[VanillaIDRS.VoidFiend] 			= "VoidSurvivorBody",
			[VanillaIDRS.Heretic]				= "HereticBody",
		};
		public static Dictionary<ModdedIDRS, string> moddedEnumToBodyObjName = new()
		{
			[ModdedIDRS.AurelionSol] 			= "AurelionSolBody",
			[ModdedIDRS.Natsu] 					= "NatsuBody",
			[ModdedIDRS.Goku] 					= "GokuBody",
			[ModdedIDRS.Trunks] 				= "TrunksBody",
			[ModdedIDRS.Vegeta] 				= "VegetaBody",
			[ModdedIDRS.Aatrox] 				= "AatroxBody",
			[ModdedIDRS.Bomber] 				= "DragonBomberBody",
			[ModdedIDRS.SynergiesKing] 			= "SynergiesKingBody",
			[ModdedIDRS.Sett] 					= "SettBody",
			[ModdedIDRS.Katarina] 				= "Katarina",
			[ModdedIDRS.Jinx] 					= "JinxBody",
			[ModdedIDRS.Enforcer] 				= "EnforcerBody",
			[ModdedIDRS.NemesisEnforcer] 		= "NemesisEnforcerBody",
			[ModdedIDRS.Gunslinger] 			= "GunslingerBody",
			[ModdedIDRS.Paladin] 				= "RobPaladinBody",
			[ModdedIDRS.Miner] 					= "MinerBody",
			[ModdedIDRS.House] 					= "JavangleHouse",
			[ModdedIDRS.Lagann] 				= "LagannBody",
			[ModdedIDRS.GurrenLagann] 			= "GurrenLagannBody",
			[ModdedIDRS.SYBeholder] 			= "SYBeholderBody",
			[ModdedIDRS.Tracer] 				= "TracerBody",
			[ModdedIDRS.Samus] 					= "DGSamusBody",
			[ModdedIDRS.Myst] 					= "JavangleMystBody",
			[ModdedIDRS.CHEF] 					= "CHEF",
			[ModdedIDRS.BanditReloaded] 		= "BANDITRELOADEDBODY",
			[ModdedIDRS.MegamanXV3] 			= "MegamanXV3Body",
			[ModdedIDRS.MegamanCM] 				= "MegamanCMBody",
			[ModdedIDRS.Phoenix] 				= "PhoenixBody",
			[ModdedIDRS.Ganondorf] 				= "GanondorfBody",
			[ModdedIDRS.Scout] 					= "ScoutBody",
			[ModdedIDRS.Ditto] 					= "DittoBody",
			[ModdedIDRS.Holomancer] 			= "HolomancerBody",
			[ModdedIDRS.RedMist] 				= "RedMistBody",
			[ModdedIDRS.Yasso] 					= "YassoBody",
			[ModdedIDRS.Yasuo] 					= "YasuoBody",
			[ModdedIDRS.Dancer] 				= "DancerBody",
			[ModdedIDRS.Wisp] 					= "WarframeWispBody",
			[ModdedIDRS.Deku] 					= "DekuBody",
			[ModdedIDRS.Soldier] 				= "TF2SollyBody",
			[ModdedIDRS.Arbiter] 				= "ArbiterBody",
			[ModdedIDRS.Nemmando]				= "NemmandoBody",
			[ModdedIDRS.Executioner]			= "ExecutionerBody",
			[ModdedIDRS.Hand]					= "HANDOverclockedBody",
		};

		public static Dictionary<VanillaIDRS, ItemDisplayRuleSet> bodyReference = new();
		public static Dictionary<ModdedIDRS, ItemDisplayRuleSet> moddedBodyReference = new();

		public static ItemDisplayRuleSet? GetRuleSet(ModdedIDRS which)
		{
			if (moddedBodyReference.ContainsKey(which)) return moddedBodyReference[which];

			var body = BodyCatalog.FindBodyPrefab(moddedEnumToBodyObjName[which]);
			if (!body) return null;
			moddedBodyReference.Add(which, body.GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>().itemDisplayRuleSet);
			
			return moddedBodyReference[which];
		}
		public static ItemDisplayRuleSet? GetRuleSet(VanillaIDRS which)
		{
			if (bodyReference.ContainsKey(which)) return bodyReference[which];

			var body = BodyCatalog.FindBodyPrefab(enumToBodyObjName[which]);
			if (!body)
			{
				Debug.Log("Missing body for vanilla survivor: " + which);
				return null;
			}
			bodyReference.Add(which, body.GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>().itemDisplayRuleSet);

			return bodyReference[which];
		}
	}
}