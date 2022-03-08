using System;
using System.Reflection;
using EntityStates.Railgunner.Weapon;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace FixTeleportMomentum
{
	[HarmonyPatch]
	public static class HarmonyPatches
	{
		private static FieldInfo velo = typeof(CharacterMotor).GetField("velocity");

		[HarmonyPostfix, HarmonyPatch(typeof(MapZone), nameof(MapZone.TeleportBody))]
		public static void FuckingMomentum(CharacterBody characterBody)
		{
			velo.SetValue(characterBody.characterMotor, Vector3.zero);
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(BaseFireSnipe), nameof(BaseFireSnipe.OnExit))]
		public static void FixSniper(ILContext il)
		{
			var c = new ILCursor(il);
			ILLabel label = null;
			c.GotoNext(x => x.MatchLdarg(out _),
				x => x.MatchLdfld<BaseFireSnipe>(nameof(BaseFireSnipe.wasMiss)),
				x => x.MatchBrtrue(out label));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<BaseFireSnipe, bool>>(snipe => !snipe.isAuthority);
			c.Emit(OpCodes.Brtrue, label);
		}
	}
}