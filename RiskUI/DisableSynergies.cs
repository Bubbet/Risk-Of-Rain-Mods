using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Synergies;

namespace MaterialHud
{
	[HarmonyPatch]
	public static class DisableSynergies
	{
		[HarmonyILManipulator, HarmonyPatch(typeof(Hook), nameof(Hook.HUD_Awake)), HarmonyPatch(typeof(Hook), nameof(Hook.HUD_Update))]
		public static void DisableFunction(ILContext il)
		{
			var c = new ILCursor(il);
			c.Index += 3;
			c.Emit(OpCodes.Ret);
		}
	}
}