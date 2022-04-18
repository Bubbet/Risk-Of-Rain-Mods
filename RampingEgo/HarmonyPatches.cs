using System;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

namespace RampingEgo
{
	[HarmonyPatch]
	public static class HarmonyPatches
	{
		[HarmonyILManipulator, HarmonyPatch(typeof(LunarSunBehavior), nameof(LunarSunBehavior.FixedUpdate))]
		public static void FixLunarSun(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(MoveType.After, x => x.MatchLdcR4(60f));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<float, LunarSunBehavior, float>>((f, behavior) => f / (float)behavior.stack);
		}
	}
}