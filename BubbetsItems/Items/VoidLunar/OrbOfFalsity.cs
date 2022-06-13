using System;
using System.Collections.Generic;
using System.Linq;
using BubbetsItems.Helpers;
using EntityStates;
using EntityStates.Interactables.MSObelisk;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace BubbetsItems.Items.VoidLunar
{
	public class OrbOfFalsity : ItemBase
	{
		public static int? defaultCampCost;

		protected override void MakeTokens()
		{
			base.MakeTokens();
			var name = GetType().Name.ToUpper();
			SimpleDescriptionToken = name + "_DESC_SIMPLE";
			AddToken(name + "_NAME", "Orbs of Falsity");
			var convert = "Converts all Beads of Fealty's".Style(StyleEnum.Void) + ".";
			AddToken(name + "_CONVERT", convert);
			AddToken(name + "_DESC", "Seems to do nothing... but... " + "Void Seeds spawn {0:0%} more. ".Style(StyleEnum.Health));
			AddToken(name + "_DESC_SIMPLE", "Seems to do nothing... but... " +"Void Seeds spawn 50% ".Style(StyleEnum.Health) +"(+15% per stack)".Style(StyleEnum.Stack) +" more often. ");
			AddToken(name + "_PICKUP", "Seems to do nothing... but... " + convert);
			AddToken(name + "_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[a] * 0.15 + 0.35", "Seed Chance");
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			AddVoidPairing(nameof(RoR2Content.Items.LunarTrinket));
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			//SceneDirector.onGenerateInteractableCardSelection += GenerateInteractables;
			DirectorCardCategorySelection.calcCardWeight += GetWeight;
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			//SceneDirector.onGenerateInteractableCardSelection -= GenerateInteractables;
			DirectorCardCategorySelection.calcCardWeight -= GetWeight;
		}

		private void GetWeight(DirectorCard card, ref float weight)
		{
			if (card.spawnCard.name != "iscVoidCamp") return;
			var inst = GetInstance<OrbOfFalsity>();
			var amount = Util.GetItemCountForTeam(TeamIndex.Player, inst.ItemDef.itemIndex, false, false);
			defaultCampCost ??= card.spawnCard.directorCreditCost;
			card.spawnCard.directorCreditCost = defaultCampCost.Value;
			if (amount <= 0) return;
			var a = inst.scalingInfos[0].ScalingFunction(amount);
			weight += a;
			card.spawnCard.directorCreditCost = Mathf.FloorToInt(defaultCampCost.Value / (1f + a));
		}
 
		private void GenerateInteractables(SceneDirector director, DirectorCardCategorySelection categorySelection)
		{
			var inst = GetInstance<OrbOfFalsity>();
			var amount = Util.GetItemCountForTeam(TeamIndex.Player, inst.ItemDef.itemIndex, false, false);
			if (amount <= 0) return;
			var camp = categorySelection.categories.SelectMany(x => x.cards).First(x => x.spawnCard.name == "iscVoidCamp"); // TODO fix this for simulacrum, currently its fine because you cant obtain void lunar
			var a = Mathf.FloorToInt(1 + inst.scalingInfos[0].ScalingFunction(amount));
			camp.spawnCard.directorCreditCost /= a;
			camp.selectionWeight *= a;
		}

		[HarmonyPrefix, HarmonyPatch(typeof(EndingGame), nameof(EndingGame.DoFinalAction))]
		public static bool SwapExit(EndingGame __instance)
		{
			var inst = GetInstance<OrbOfFalsity>();

			var amount = Util.GetItemCountForTeam(TeamIndex.Player, inst.ItemDef.itemIndex, false, false);
			if (amount <= 0) return true;

			__instance.outer.SetNextState(new TransitionToVoidStage());
			
			return false;
		}

		//[HarmonyILManipulator, HarmonyPatch(typeof(PortalSpawner), nameof(PortalSpawner.Start))]
		public static void IncreaseChance(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(MoveType.After, x => x.MatchLdfld<PortalSpawner>(nameof(PortalSpawner.spawnChance)));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<float, PortalSpawner, float>>((f, spawner) =>
			{
				if (spawner.spawnMessageToken != "PORTAL_VOID_OPEN") return f;
				var inst = GetInstance<OrbOfFalsity>();
				var amount = Util.GetItemCountForTeam(TeamIndex.Player, inst.ItemDef.itemIndex, false, false);
				if (amount <= 0) return f;
				return f + inst.scalingInfos[0].ScalingFunction(amount);
			});
		}
	}

	public class TransitionToVoidStage : EntityState
	{
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (!NetworkServer.active || !(fixedAge >= duration)) return;
			
			Stage.instance.BeginAdvanceStage(SceneCatalog.GetSceneDefFromSceneName("voidstage"));
			outer.SetNextState(new Idle());
		}

		// Token: 0x04001081 RID: 4225
		public static float duration;
	}
}