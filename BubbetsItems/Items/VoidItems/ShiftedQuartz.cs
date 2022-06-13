using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using BubbetsItems.ItemBehaviors;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Items
{
	public class ShiftedQuartz : ItemBase
	{
		public static ConfigEntry<bool> visualOnlyForAuthority;
		public static ConfigEntry<float> visualTransparency;

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("SHIFTEDQUARTZ_NAME", "Shifted Quartz");
			var convert = "Corrupts all Focus Crystals".Style(StyleEnum.Void) + ".";
			AddToken("SHIFTEDQUARTZ_CONVERT", convert);
			AddToken("SHIFTEDQUARTZ_PICKUP", "Deal bonus damage if there aren't nearby enemies. " + "Corrupts all Focus Crystals".Style(StyleEnum.Void) + ". " + convert);
			AddToken("SHIFTEDQUARTZ_DESC", "Increase damage dealt by " + "{1:0%} ".Style(StyleEnum.Damage) + "when there are no enemies within " + "{0}m ".Style(StyleEnum.Damage) + "of you. ");
			AddToken("SHIFTEDQUARTZ_DESC_SIMPLE", "Increase damage dealt by " + "15% ".Style(StyleEnum.Damage) + "(+15% per stack) ".Style(StyleEnum.Stack) + "when there are no enemies within " + "18m ".Style(StyleEnum.Damage) + "of you. ");
			SimpleDescriptionToken = "SHIFTEDQUARTZ_DESC_SIMPLE";
			AddToken("SHIFTEDQUARTZ_LORE", "");
		}
		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("18", "Distance", oldDefault: "20");
			AddScalingFunction("[a] * 0.15", "Damage", oldDefault: "[a] * 0.2");
			visualOnlyForAuthority = sharedInfo.ConfigFile!.Bind(ConfigCategoriesEnum.General,
				"Shifted quartz visual only for authority", false,
				"Should shifted quartz visual effect only show for the player who has the item", networked: false);
			visualTransparency = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Shifted quartz inside transparency",
				0.15f, "The transparency of the dome when enemies are inside it.", networked: false);
		}

		public override void MakeRiskOfOptions()
		{
			base.MakeRiskOfOptions();
			ModSettingsManager.AddOption(new CheckBoxOption(visualOnlyForAuthority));
			ModSettingsManager.AddOption(new SliderOption(visualTransparency, new SliderConfig {min = 0, max = 1, formatString = "{0:0.00%}"}));
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			AddVoidPairing("NearbyDamageBonus");
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
			c.Emit(OpCodes.Ldloc_1); // Body; 0 is master
			c.Emit(OpCodes.Ldloc, num2);
			c.EmitDelegate<Func<CharacterBody, float, float>>((body, amount) =>
			{
				var instance = GetInstance<ShiftedQuartz>();
				var count = body.inventory.GetItemCount(instance.ItemDef);
				if (count <= 0) return amount;
				var inside = body.GetComponent<ShiftedQuartzBehavior>().inside; // TODO this might not exist in scope and may throw errors in multiplayer
				if (!inside)
					amount *= 1f + instance.scalingInfos[1].ScalingFunction(count); // 1f + count * 0.2f
				return amount;
			});
			c.Emit(OpCodes.Stloc, num2);
		}
		
		protected override void FillItemDisplayRules()
		{
			base.FillItemDisplayRules();
			AddDisplayRules(VanillaIDRS.Engineer, 
				new ItemDisplayRule
				{
					childName = "HandL",
					localPos = new Vector3(-0.00579F, 0.03112F, -0.00715F),
					localAngles = new Vector3(291.4859F, 161.2108F, 10.64889F),
					localScale = new Vector3(0.1F, 0.1F, 0.1F)
				}
			);
			AddDisplayRules(VanillaIDRS.Commando, 
				new ItemDisplayRule
				{
					childName = "HandR",
					localPos = new Vector3(0.02447F, 0.02118F, 0.01939F),
					localAngles = new Vector3(272.5952F, 264.0469F, 339.3327F),
					localScale = new Vector3(0.08394F, 0.08394F, 0.08394F)
				}
			);
			AddDisplayRules(VanillaIDRS.Huntress, 
				new ItemDisplayRule
				{
					childName = "BowBase",
					localPos = new Vector3(0.00704F, -0.07864F, -0.02207F),
					localAngles = new Vector3(270.0198F, 0F, 0F),
					localScale = new Vector3(0.06104F, 0.06104F, 0.06104F)
				}
			);
			AddDisplayRules(VanillaIDRS.Bandit, 
				new ItemDisplayRule
				{
					childName = "MainWeapon",
					localPos = new Vector3(-0.09467F, 0.86001F, -0.01482F),
					localAngles = new Vector3(347.3123F, 278.5629F, 15.23954F),
					localScale = new Vector3(0.05985F, 0.05985F, 0.05985F)

				}
			);
			AddDisplayRules(VanillaIDRS.Mult, 
				new ItemDisplayRule
				{
					childName = "Head",
					localPos = new Vector3(0.31767F, 3.05505F, -1.07862F),
					localAngles = new Vector3(55.84481F, 356.188F, 0.98089F),
					localScale = new Vector3(0.53159F, 0.53159F, 0.53159F)
				}
			);
			AddDisplayRules(VanillaIDRS.Artificer, 
				new ItemDisplayRule
				{
					childName = "Head",
					localPos = new Vector3(0.00038F, -0.07349F, 0.00302F),
					localAngles = new Vector3(85.66677F, 175.6506F, 175.3229F),
					localScale = new Vector3(0.06245F, 0.06785F, 0.06245F)
				}
			);
			AddDisplayRules(VanillaIDRS.Mercenary, 
				new ItemDisplayRule
				{
					childName = "HandL",
					localPos = new Vector3(0F, 0F, 0F),
					localAngles = new Vector3(292.3235F, 61.29676F, 322.3165F),
					localScale = new Vector3(0.06341F, 0.06341F, 0.06341F)
				}
			);
			AddDisplayRules(VanillaIDRS.Rex,
				new ItemDisplayRule
				{
					childName = "AimOriginSyringe",
					localPos = new Vector3(-0.00003F, -0.00556F, 0.00016F),
					localAngles = new Vector3(19.21766F, 0.79543F, 0.2625F),
					localScale = new Vector3(0.09729F, 0.09729F, 0.09729F)
				}
			);
			AddDisplayRules(VanillaIDRS.Loader,
				new ItemDisplayRule
				{
					childName = "MechBase",
					localPos = new Vector3(0.19437F, 0.05113F, 0.42794F),
					localAngles = new Vector3(46.53585F, 252.6348F, 292.8854F),
					localScale = new Vector3(0.0562F, 0.0562F, 0.0562F)
				}
			);
			AddDisplayRules(VanillaIDRS.Acrid,
				new ItemDisplayRule
				{
					childName = "Finger11L",
					localPos = new Vector3(-0.22908F, 0.53714F, 0.03819F),
					localAngles = new Vector3(289.1386F, 324.2163F, 229.5377F),
					localScale = new Vector3(0.3743F, 0.3743F, 0.3743F)
				}
			);
			AddDisplayRules(VanillaIDRS.Captain, 
				new ItemDisplayRule
				{
					childName = "MuzzleGun",
					localPos = new Vector3(0.02424F, -0.00455F, 0.01275F),
					localAngles = new Vector3(338.4595F, 15.44651F, 269.0287F),
					localScale = new Vector3(0.10684F, 0.10684F, 0.10381F)
				}
			);
			AddDisplayRules(VanillaIDRS.RailGunner, 
				new ItemDisplayRule
				{
					childName = "ThighL",
					localPos = new Vector3(-0.00213F, 0.28705F, 0.03017F),
					localAngles = new Vector3(77.29975F, 335.8542F, 354.0805F),
					localScale = new Vector3(-0.11036F, -0.11036F, -0.11036F)
				}
			);
			AddDisplayRules(VanillaIDRS.VoidFiend, 
				new ItemDisplayRule
				{
					childName = "Chest",
					localPos = new Vector3(-0.01839F, -0.17924F, 0.06976F),
					localAngles = new Vector3(81.41689F, 35.81398F, 62.77233F),
					localScale = new Vector3(0.17493F, 0.17493F, 0.17493F)
				}
			);
		}
	}
}