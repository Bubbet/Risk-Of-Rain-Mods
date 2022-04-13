using System;
using BubbetsItems.Helpers;
using BubbetsItems.ItemBehaviors;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

namespace BubbetsItems.Items
{
	public class JelliedSoles : ItemBase
	{
		public static JelliedSoles instance;

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("Min([a] * 0.15, 1)", "Reduction");
		}

		public JelliedSoles()
		{
			instance = this;
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(GlobalEventManager), nameof(GlobalEventManager.OnCharacterHitGroundServer))]
		public static void NullifyFallDamage(ILContext il)
		{
			var c = new ILCursor(il);
			//var h = -1;
			//var d = -1;
			c.GotoNext(x => x.MatchCallvirt<CharacterBody>("get_footPosition"));
			c.GotoNext(MoveType.Before, x => x.MatchLdloc(out _), x => x.MatchLdloc(out _), x => x.MatchCallvirt<HealthComponent>(nameof(HealthComponent.TakeDamage)));
			c.Index++;
			c.Emit(OpCodes.Dup);
			c.Index++;
			//c.Emit(OpCodes.Ldloc, h);
			//c.Emit(OpCodes.Ldloc, d);
			c.EmitDelegate<Func<HealthComponent, DamageInfo, DamageInfo>>(UpdateDamage);
		}

		
		// body.damage/body.basedamage * storedDamage
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
				var count = body.inventory.GetItemCount(instance.ItemDef);
				if (count <= 0) return amount;
				var behavior = body.GetComponent<JelliedSolesBehavior>();
				amount += body.damage / body.baseDamage * behavior.storedDamage;
				behavior.storedDamage = 0;
				return amount;
			});
			c.Emit(OpCodes.Stloc, num2);
		}
		
		private static DamageInfo UpdateDamage(HealthComponent component, DamageInfo info)
		{
			var count = component.body.inventory.GetItemCount(instance.ItemDef);
			if (count <= 0) return info;
			var behavior = component.GetComponent<JelliedSolesBehavior>();
			var frac = instance.scalingInfos[0].ScalingFunction(count);
			behavior.storedDamage += info.damage * frac;
			info.damage *= 1f - frac;
			return info;
		}

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("JELLIEDSOLES_NAME", "Jellied Soles");
			AddToken("JELLIEDSOLES_PICKUP", "Reduces " + "fall damage.".Style(StyleEnum.Utility) + " Converts that reduction into your next attack.");
			AddToken("JELLIEDSOLES_DESC", "Reduces " + "fall damage ".Style(StyleEnum.Utility) + "by " + "{0:0%}".Style(StyleEnum.Utility) + ". Converts that reduction into your next attack.");
			AddToken("JELLIEDSOLES_LORE", "");
		}
	}
}