using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

namespace BubbetsItems.Items
{
	public class ZealotryEmbrace : ItemBase
	{
		private static ConfigEntry<bool> onlyMyDots;
		private static ConfigEntry<bool> onlyOneDot;
		protected override void MakeTokens()
		{
			base.MakeTokens();

			AddToken("ZEALOTRYEMBRACE_NAME", "Zealotry Embrace");
			AddToken("ZEALOTRYEMBRACE_PICKUP", "Deal more damage to enemies with barely any debuffs inflicted." + " Corrupts all Death Marks".Style(StyleEnum.Void) + ".");
			AddToken("ZEALOTRYEMBRACE_DESC", "Deal " + "{0:0%} more damage ".Style(StyleEnum.Damage) + "on enemies with less than " + "{1} ".Style(StyleEnum.Damage) + "debuffs. " + "Corrupts all Death Marks".Style(StyleEnum.Void) + ".");
			AddToken("ZEALOTRYEMBRACE_LORE", "");
		}
		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("0.25", "Damage Increase");
			AddScalingFunction("2 + [a]", "Debuff Amount");
			onlyMyDots = sharedInfo.ConfigFile!.Bind(ConfigCategoriesEnum.General, "Zealotry Embrace: Only track my debuffs", true,
				"Should only your dots track to the total");
			onlyOneDot = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Zealotry Embrace: Only one dot stack", false,
				"Should each dot stack count towards the total, else treat all stacks as one buff.");
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
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldloc, num2);
			c.EmitDelegate<Func<HealthComponent, CharacterBody,  DamageInfo, float, float>>((hc, body, damageInfo, amount) =>
			{
				var zealotryEmbrace = GetInstance<ZealotryEmbrace>();
				var count = body.inventory.GetItemCount(zealotryEmbrace.ItemDef);
				if (count <= 0) return amount;
				
				var debuffCount = BuffCatalog.debuffBuffIndices.Sum(buffType => hc.body.GetBuffCount(buffType));
				var dotController = DotController.FindDotController(hc.gameObject);
				if (dotController)
					if (onlyOneDot.Value)
					{
						var list = from dotStack in onlyMyDots.Value ? dotController.dotStackList.Where(x => x.attackerObject == body.gameObject) : dotController.dotStackList select dotStack.dotIndex;
						debuffCount += list.Distinct().Count();
					}
					else
					{
						if (onlyMyDots.Value)
							debuffCount += dotController.dotStackList.Count(x => x.attackerObject == body.gameObject);
						else
							debuffCount += dotController.dotStackList.Count;
					}

				if (debuffCount < zealotryEmbrace.scalingInfos[1].ScalingFunction(count))
				{
					damageInfo.damageColorIndex = DamageColorIndex.Void;
					amount *= 1f + zealotryEmbrace.scalingInfos[0].ScalingFunction(count);
				}

				return amount;
			});
			c.Emit(OpCodes.Stloc, num2);
		}
	}
}