using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using BubbetsItems.ItemBehaviors;
using HarmonyLib;
using InLobbyConfig;
using InLobbyConfig.Fields;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NCalc.Domain;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Items
{
	public class ShiftedQuartz : ItemBase
	{
		public static ShiftedQuartz instance;
		public override bool RequiresSotv => true;

		public ShiftedQuartz()
		{
			instance = this;
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("20", "Distance");
			AddScalingFunction("[a] * 0.2", "Damage");
		}

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("SHIFTEDQUARTZ_NAME", "Shifted Quartz");
			AddToken("SHIFTEDQUARTZ_DESC", "{1:0%} extra damage".Style(StyleEnum.Damage) + " when enemies are not within " + "{0}m".Style(StyleEnum.Damage) + " of you." + " Corrupts all Focus Crystal".Style(StyleEnum.Void) + ".");
			AddToken("SHIFTEDQUARTZ_PICKUP", "Extra damage when enemies are far away." + " Corrupts all Focus Crystal".Style(StyleEnum.Void) + ".");
			AddToken("SHIFTEDQUARTZ_LORE", "");
		}

		public override void MakeInLobbyConfig(Dictionary<ConfigCategoriesEnum, List<object>> scalingFunctions)
		{
			base.MakeInLobbyConfig(scalingFunctions);
			//list.Add(ConfigFieldUtilities.CreateFromBepInExConfigEntry(scalingInfos[0].)); TODO
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
		public static void IlTakeDamage(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(MoveType.After, x => x.OpCode == OpCodes.Ldsfld && (x.Operand as FieldReference)?.Name == nameof(RoR2Content.Items.NearbyDamageBonus),
				x => x.MatchCallOrCallvirt(out _),
				x => x.MatchStloc(out _));
			var where = c.Index;
			int num2 = -1;
			c.GotoNext(x => x.MatchLdloc(out num2),
				x => x.MatchLdcR4(1f),
				x => x.MatchLdloc(out _));
			c.Index = where;
			c.Emit(OpCodes.Ldloc_1); // Body; 0 is master
			c.Emit(OpCodes.Ldloc, num2);
			c.EmitDelegate<Func<CharacterBody, float, float>>((body, amount) =>
			{
				var count = body.inventory.GetItemCount(instance.ItemDef);
				if (count <= 0) return amount;
				var inside = body.GetComponent<ShiftedQuartzBehavior>().inside; // TODO this might not exist in scope and may throw errors in multiplayer
				if (!inside)
					amount *= 1f + instance.scalingInfos[1].ScalingFunction(count); // 1f + count * 0.2f
				return amount;
			});
			c.Emit(OpCodes.Stloc, num2);
		}
		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			pairs.Add(new ItemDef.Pair
			{
				itemDef1 = RoR2Content.Items.NearbyDamageBonus,
				itemDef2 = ItemDef
			});
		}
	}
}