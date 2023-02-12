using System;
using System.Collections.Generic;
using BubbetsItems.Helpers;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;

namespace BubbetsItems.Items.VoidLunar
{
	public class Imperfect : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			var name = GetType().Name.ToUpper();
			SimpleDescriptionToken = name + "_DESC_SIMPLE";
			AddToken(name + "_NAME", "Imperfection");
			var convert = "Converts all Transcendence".Style(StyleEnum.VoidLunar) + ".";
			AddToken(name + "_CONVERT", convert);
			AddToken(name + "_DESC", "Converts all but 1 shield into maximum health. Gain "+"{0:0%} shield".Style(StyleEnum.Utility) +" and " + "{1} armor. ".Style(StyleEnum.Health));
			AddToken(name + "_DESC_SIMPLE", "Converts all current " + "shield".Style(StyleEnum.Utility) + " into " + "maximum health".Style(StyleEnum.Health) + ". Reduce " + "armor".Style(StyleEnum.Health) + " by " + "-25".Style(StyleEnum.Health) + " (-25 per stack)".Style(StyleEnum.Stack) + " but gain an additional " + "25% shield".Style(StyleEnum.Utility) + " (+25% per stack)".Style(StyleEnum.Stack) + ". ");
			AddToken(name + "_PICKUP", "Convert all your shield into health. "+"Increase maximum shield…".Style(StyleEnum.Utility) +" BUT your armor is frail. ".Style(StyleEnum.Health) + convert);
			AddToken(name + "_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("0.25 * [a]", "Shield Gain");
			AddScalingFunction("-25 * [a]", "Armor Add");
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			AddVoidPairing(nameof(RoR2Content.Items.ShieldOnly));
		}

		//[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))] //ODO i need to write il for this because at the bottom of recalc stats there is some code that heals/removes health based on the max hp and that might be what is causing this weird ass behavior
		public static void DoEffect(CharacterBody __instance)
		{
			if (!__instance) return;
			var inv = __instance.inventory;
			if (!inv) return;
			var inst = GetInstance<Imperfect>();
			var amount = inv.GetItemCount(inst.ItemDef);
			if (amount <= 0) return;
			__instance.maxShield *= 1 + inst.scalingInfos[0].ScalingFunction(amount);
			__instance.maxHealth += __instance.maxShield - 1;
			__instance.maxShield = 1;
			__instance.armor += inst.scalingInfos[1].ScalingFunction(amount);
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			RecalculateStatsAPI.GetStatCoefficients += RecalcStats;
			RoR2Application.onLoad += () => On.RoR2.CharacterBody.RecalculateStats += UsedToBePatchIL; // if anyone hooks it THIS LATE i will kill you
		}
		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			RecalculateStatsAPI.GetStatCoefficients -= RecalcStats;
			RoR2Application.onLoad -= () => On.RoR2.CharacterBody.RecalculateStats += UsedToBePatchIL;
			On.RoR2.CharacterBody.RecalculateStats -= UsedToBePatchIL;
		}

		private void RecalcStats(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
		{
			var inv = sender.inventory;
			if (!inv) return;
			var inst = GetInstance<Imperfect>()!;
			var amount = inv.GetItemCount(inst.ItemDef);
			if (amount <= 0) return;
			args.armorAdd += inst.scalingInfos[1].ScalingFunction(amount);
			args.shieldMultAdd += inst.scalingInfos[0].ScalingFunction(amount);
		}

		/*
		[HarmonyILManipulator, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void PatchIl(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<CharacterBody>("set_" + nameof(CharacterBody.maxShield)));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Action<CharacterBody>>(cb =>
			{
				var inv = cb.inventory;
				if (!inv) return;
				var inst = GetInstance<Imperfect>()!;
				var amount = inv.GetItemCount(inst.ItemDef);
				if (amount <= 0) return;
				cb.maxHealth += cb.maxShield - 1;
				cb.maxShield = 1;
			});
		}
		*/

		public void UsedToBePatchIL(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
			orig(self);
			if (self?.inventory == null) return;
			if (self.inventory.GetItemCount(ItemDef) > 0 && self.maxShield > 1)
			{
				self.maxHealth += self.maxShield - 1;
				self.maxShield = 1;
            }
        }
	}
}