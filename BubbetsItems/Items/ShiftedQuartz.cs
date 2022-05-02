using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using BubbetsItems.ItemBehaviors;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

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
			AddToken("SHIFTEDQUARTZ_PICKUP", "Deal bonus damage if there aren't nearby enemies. " + "Corrupts all Focus Crystals".Style(StyleEnum.Void) + ".");
			AddToken("SHIFTEDQUARTZ_DESC", "Increase damage dealt by " + "{1:0%} ".Style(StyleEnum.Damage) + "when there are no enemies within " + "{0}m ".Style(StyleEnum.Damage) + "of you. " + "Corrupts all Focus Crystals".Style(StyleEnum.Void) + ".");
			AddToken("SHIFTEDQUARTZ_LORE", "");
		}
		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("18", "Distance", oldDefault: "20");
			AddScalingFunction("[a] * 0.15", "Damage", oldDefault: "[a] * 0.2");
			visualOnlyForAuthority = sharedInfo.ConfigFile!.Bind(ConfigCategoriesEnum.General,
				"Shifted quartz visual only for authority", false,
				"Should shifted quartz visual effect only show for the player who has the item");
			visualTransparency = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Shifted quartz inside transparency",
				0.15f, "The transparency of the dome when enemies are inside it.");
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
	}
}