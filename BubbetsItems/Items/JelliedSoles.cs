using System;
using BepInEx.Configuration;
using BubbetsItems.Helpers;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Items
{
	public class JelliedSoles : ItemBase
	{
		private static JelliedSoles _instance;

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("Min([a] * 0.15, 1)", "Reduction");
		}

		public JelliedSoles()
		{
			_instance = this;
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
		
		private static DamageInfo UpdateDamage(HealthComponent component, DamageInfo info)
		{
			var count = component.body.inventory.GetItemCount(_instance.ItemDef);
			if (count <= 0) return info;
			info.damage *= Mathf.Max(0f, 1f - _instance.scalingInfos[0].ScalingFunction(count));
			return info;
		}

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("JELLIEDSOLES_NAME", "Jellied Soles");
			AddToken("JELLIEDSOLES_PICKUP", "Reduces " + "fall damage.".Style(StyleEnum.Utility));
			AddToken("JELLIEDSOLES_DESC", "Reduces " + "fall damage ".Style(StyleEnum.Utility) + "by " + "{0:0%}".Style(StyleEnum.Utility) + ".");
			AddToken("JELLIEDSOLES_LORE", "");
		}
	}
}