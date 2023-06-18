using System;
using System.Collections.Generic;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace NetworkedTimedBuffs
{
	[HarmonyPatch]
	public static class HarmonyPatches
	{
		public static void DefaultBehaviour(ILContext il)
		{
			var c = new ILCursor(il);
			var indexloc = -1;
			c.GotoNext(
				x => x.MatchLdarg(0),
				x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.timedBuffs)),
				x => x.MatchLdloc(out indexloc)
			);
			c.GotoNext(MoveType.After,
				x => x.OpCode == OpCodes.Ldfld && (x.Operand as FieldReference)?.Name == "duration",
				x => x.MatchCallOrCallvirt<Mathf>(nameof(Mathf.Max))
			);
			c.Emit(OpCodes.Dup);
			c.Emit(OpCodes.Ldloc, indexloc);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Action<float, int, CharacterBody>>(UpdateTimer);
		}
		public static void RefreshStacks(ILContext il)
		{
			var c = new ILCursor(il);
			var indexloc = -1;
			c.GotoNext(
				x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.timedBuffs)),
				x => x.MatchLdloc(out indexloc),
				x => x.MatchCallOrCallvirt(out _)
			);
			c.GotoNext(x => x.MatchStfld<CharacterBody.TimedBuff>(nameof(CharacterBody.TimedBuff.timer)));
			c.Emit(OpCodes.Dup);
			c.Emit(OpCodes.Ldloc, indexloc);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Action<float, int, CharacterBody>>(UpdateTimer);
		}

		private static void UpdateTimer(float duration, int index, CharacterBody body)
		{
			NetworkedTimedBuffsPlugin.UpdateTimer(body, index, duration);
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.FixedUpdate))]
		public static void UpdateTimedBuffsForEveryone(ILContext il)
		{
			var c = new ILCursor(il);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Action<CharacterBody>>(body =>
			{
				body.UpdateBuffs(Time.fixedDeltaTime);
			});
			c.GotoNext(x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.UpdateBuffs)));
			c.GotoPrev(x => x.MatchLdarg(0));
			c.RemoveRange(3);
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.UpdateBuffs))]
		public static void AllowEveryoneToUpdate(ILContext il)
		{
			var c = new ILCursor(il);
			c.RemoveRange(5);
			
			c.GotoNext(
				x => (x.OpCode == OpCodes.Call || x.OpCode == OpCodes.Callvirt) && (x.Operand as MethodReference)?.Name == "RemoveAt"
			);
			c.Remove();
			c.EmitDelegate<Action<List<CharacterBody.TimedBuff>, int>>((list, index) =>
			{
				if (!NetworkServer.active) return;
				list.RemoveAt(index);
			});

			
			c.GotoNext(x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.RemoveBuff)));
			c.Remove();
			c.EmitDelegate<Action<CharacterBody, BuffIndex>>((body, index) =>
			{
				if (!NetworkServer.active) return;
				body.RemoveBuff(index);
			});
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.AddTimedBuff), typeof(BuffDef), typeof(float))]
		public static void AddTimedBuffRefresh2(ILContext il)
		{
			var c = new ILCursor(il);

			var indexloc = -1;
			
			c.GotoNext( MoveType.After,
				x => x.MatchLdarg(0),
				x => x.MatchLdfld(out _),
				x => x.MatchLdloc(out indexloc),
				x => x.MatchCallOrCallvirt(out _),
				x => x.MatchLdloc(out _),
				x => x.MatchLdfld(out _),
				x => x.MatchStfld<CharacterBody.TimedBuff>(nameof(CharacterBody.TimedBuff.timer))
			);
			c.Index--;
			c.Emit(OpCodes.Dup);
			c.Emit(OpCodes.Ldloc, indexloc);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Action<float, int, CharacterBody>>(UpdateTimer);
		}
		
		[HarmonyILManipulator, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.AddTimedBuff), typeof(BuffDef), typeof(float), typeof(int))]
		public static void AddTimedBuffRefresh(ILContext il)
		{
			var c = new ILCursor(il);
			
			var indexloc = -1;
			var durationarg = -1;

			c.GotoNext( MoveType.After,
				x => x.MatchLdarg(0),
				x => x.MatchLdfld(out _),
				x => x.MatchLdloc(out indexloc),
				x => x.MatchCallOrCallvirt(out _),
				x => x.MatchLdarg(out durationarg),
				x => x.MatchStfld<CharacterBody.TimedBuff>(nameof(CharacterBody.TimedBuff.timer))
			);

			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc, indexloc);
			c.Emit(OpCodes.Ldarg, durationarg);
			
			c.EmitDelegate<Action<CharacterBody, int, float>>(NetworkedTimedBuffsPlugin.UpdateTimer);
		}

		public static void TimedBuffAdd(List<CharacterBody.TimedBuff> __instance, CharacterBody.TimedBuff item)
		{
			if (__instance.GetType() != typeof(List<CharacterBody.TimedBuff>)) return;
			if (!NetworkServer.active) return;
			NetworkedTimedBuffsPlugin.timedBuffsMap.TryGetValue(__instance, out var body);
			if (body == null) return;
			if (!body.isPlayerControlled && NetworkedTimedBuffsPlugin.onlySyncPlayers.Value) return;
			new SyncTimedBuffAdd(body.networkIdentity.netId, item.buffIndex, item.timer).Send(NetworkDestination.Clients);
		}

		public static void TimedBuffRemoveAt(List<CharacterBody.TimedBuff> __instance, int index)
		{
			if (__instance.GetType() != typeof(List<CharacterBody.TimedBuff>)) return;
			if (!NetworkServer.active) return;
            NetworkedTimedBuffsPlugin.timedBuffsMap.TryGetValue(__instance, out var body);
            if (body == null) return;
            if (!body.isPlayerControlled && NetworkedTimedBuffsPlugin.onlySyncPlayers.Value) return;
			new SyncTimedBuffRemove(body.networkIdentity.netId, index).Send(NetworkDestination.Clients);
		}
	}
}