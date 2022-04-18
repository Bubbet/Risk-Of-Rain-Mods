using System;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

namespace OnlyYourDamageNumbers
{
	[HarmonyPatch]
	public static class HarmonyPatches
	{
		[HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.HandleDamageDealt))]
		public static void FixDamageDealt(ILContext il)
		{
			var c = new ILCursor(il) {Index = il.Instrs.Count - 1};
			c.GotoPrev(x => x.MatchLdloc(out _));
			var where = c.Next;
			c.GotoPrev(x => x.MatchCall(typeof(DamageNumberManager),"get_instance"));
			
			c.Emit(OpCodes.Ldloc_0);
			c.EmitDelegate<Func<DamageDealtMessage, bool>>(message => message.attacker == LocalUserManager.readOnlyLocalUsersList[0].cachedBodyObject); // TODO replace this with something that works while spectating
			c.Emit(OpCodes.Brfalse, where);
		}
		
		[HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.HandleHeal))]
		public static void FixHeal(ILContext il)
		{
			var c = new ILCursor(il) {Index = il.Instrs.Count - 1};
			var where = c.Next;
			c.GotoPrev(x => x.MatchCall(typeof(DamageNumberManager),"get_instance")); // c.GotoPrev(x => x.MatchCall(typeof(DamageNumberManager).GetProperty("instance", BindingFlags.Public | BindingFlags.Static)?.GetGetMethod()));
			c.Emit(OpCodes.Ldloc_0);
			c.EmitDelegate<Func<HealthComponent.HealMessage, bool>>(message => message.target == LocalUserManager.readOnlyLocalUsersList[0].cachedBodyObject);
			c.Emit(OpCodes.Brfalse, where);
		}
	}
}