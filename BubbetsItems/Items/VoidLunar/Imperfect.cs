using System;
using System.Collections.Generic;
using BubbetsItems.Helpers;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
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
			var convert = "Corrupts all Transcendence".Style(StyleEnum.VoidLunar) + ".";
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
		
		[HarmonyILManipulator, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void PatchIl(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchCallOrCallvirt<CharacterBody>("set_" + nameof(CharacterBody.maxShield)));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<float, CharacterBody, float>>((shield, cb) =>
			{
				if (!cb) return shield;
				var inv = cb.inventory;
				if (!inv) return shield;
				var inst = GetInstance<Imperfect>();
				var amount = inv.GetItemCount(inst.ItemDef);
				if (amount <= 0) return shield;
				shield *= 1 + inst.scalingInfos[0].ScalingFunction(amount);
				cb.maxHealth += shield - 1;
				shield = 1;
				return shield;
			});
			c.GotoNext(x => x.MatchCallOrCallvirt<CharacterBody>("set_" + nameof(CharacterBody.armor)));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<float, CharacterBody, float>>((armor, cb) =>
			{
				if (!cb) return armor;
				var inv = cb.inventory;
				if (!inv) return armor;
				var inst = GetInstance<Imperfect>();
				var amount = inv.GetItemCount(inst.ItemDef);
				if (amount <= 0) return armor;
				armor += inst.scalingInfos[1].ScalingFunction(amount);
				return armor;
			});
		}
	}
}