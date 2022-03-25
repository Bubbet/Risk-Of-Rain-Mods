using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
using InLobbyConfig;
using InLobbyConfig.Fields;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

namespace BubbetsItems.Items
{
	public class ZealotryEmbrace : ItemBase
	{
		private static ZealotryEmbrace _instance;
		public override bool RequiresSotv => true;

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("0.25", "Damage Increase");
			AddScalingFunction("2 + [a]", "Debuff Amount");
		}

		public ZealotryEmbrace()
		{
			_instance = this;
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			AddVoidPairing("DeathMark");
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
		public static void IlTakeDamage(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(MoveType.Before, x => x.OpCode == OpCodes.Ldsfld && (x.Operand as FieldReference)?.Name == nameof(RoR2Content.Items.NearbyDamageBonus),
				x => x.MatchCallOrCallvirt(out _),
				x => x.MatchStloc(out _));
			var where = c.Index;
			int num2 = -1;
			c.GotoNext(x => x.MatchLdloc(out num2),
				x => x.MatchLdcR4(1f),
				x => x.MatchLdloc(out _));
			c.Index = where;
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc_1); // Body; 0 is master
			c.Emit(OpCodes.Ldloc, num2);
			c.EmitDelegate<Func<HealthComponent, CharacterBody, float, float>>((hc, body, amount) =>
			{
				var count = body.inventory.GetItemCount(_instance.ItemDef);
				if (count <= 0) return amount;
				
				var debuffCount = BuffCatalog.debuffBuffIndices.Sum(buffType => hc.body.GetBuffCount(buffType));
				var dotController = DotController.FindDotController(hc.gameObject);
				if (dotController)
					debuffCount += dotController.dotStackList.Count;
				
				if (debuffCount < _instance.scalingInfos[1].ScalingFunction(count))
					amount *= 1f + _instance.scalingInfos[0].ScalingFunction(count);
				
				return amount;
			});
			c.Emit(OpCodes.Stloc, num2);
		}

		protected override void MakeTokens()
		{
			base.MakeTokens();

			AddToken("ZEALOTRYEMBRACE_NAME", "Zealotry Embrace");
			AddToken("ZEALOTRYEMBRACE_PICKUP", "Deal more damage to enemies with barely any debuffs inflicted." + " Corrupts all Death Marks".Style(StyleEnum.Void) + ".");
			AddToken("ZEALOTRYEMBRACE_DESC", "Deal " + "{0:0%} more damage ".Style(StyleEnum.Damage) + "on enemies with less than " + "{1} ".Style(StyleEnum.Damage) + "debuffs. " + "Corrupts all Death Marks".Style(StyleEnum.Void) + ".");
			AddToken("ZEALOTRYEMBRACE_LORE", "");
		}
	}
}