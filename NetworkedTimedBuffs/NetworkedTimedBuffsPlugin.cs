using System.Collections.Generic;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using UnityEngine.Networking;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace NetworkedTimedBuffs
{
	[BepInPlugin("bubbet.networkedtimedbuffs", "Networked Timed Buffs", "1.0.3")]
	[BepInDependency(NetworkingAPI.PluginGUID)]
	public class NetworkedTimedBuffsPlugin : BaseUnityPlugin
	{
		// only used in the server context
		public static Dictionary<List<CharacterBody.TimedBuff>, CharacterBody> timedBuffsMap = new Dictionary<List<CharacterBody.TimedBuff>, CharacterBody>();
		public static ConfigEntry<bool> onlySyncPlayers;

		public void Awake()
		{
			var harm = new Harmony(Info.Metadata.GUID);
			onlySyncPlayers = Config.Bind("General", "Only Sync Player", true, "Do not send net messages for enemies.");
			new PatchClassProcessor(harm, typeof(HarmonyPatches)).Patch();

			NetworkingAPI.RegisterMessageType<SyncTimedBuffAdd>();
			NetworkingAPI.RegisterMessageType<SyncTimedBuffRemove>();
			NetworkingAPI.RegisterMessageType<SyncTimedBuffUpdate>();

			CharacterBody.onBodyAwakeGlobal += body =>
			{
				if (!NetworkServer.active) return;
				timedBuffsMap[body.timedBuffs] = body;
			};
			CharacterBody.onBodyDestroyGlobal += body =>
			{
				if (!NetworkServer.active) return;
				timedBuffsMap.Remove(body.timedBuffs);
			};

			var listType = typeof(List<CharacterBody.TimedBuff>);
			var patchesType = typeof(HarmonyPatches);
			const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
			
			harm.Patch(listType.GetMethod(nameof(List<CharacterBody.TimedBuff>.Add)),
				new HarmonyMethod(patchesType.GetMethod(nameof(HarmonyPatches.TimedBuffAdd),
					flags)));
			harm.Patch(listType.GetMethod(nameof(List<CharacterBody.TimedBuff>.RemoveAt)), new HarmonyMethod(
				patchesType.GetMethod(nameof(HarmonyPatches.TimedBuffRemoveAt),
					flags)));
			
			//Compiler generated methods
			var bodyType = typeof(CharacterBody);
			harm.Patch(bodyType.GetMethodCached("<AddTimedBuff>g__DefaultBehavior|32_0"),
				null,null,null,null,new HarmonyMethod(patchesType.GetMethod(nameof(HarmonyPatches.DefaultBehaviour), flags)));
			harm.Patch(bodyType.GetMethodCached("<AddTimedBuff>g__RefreshStacks|32_1"),
				null,null,null,null,new HarmonyMethod(patchesType.GetMethod(nameof(HarmonyPatches.RefreshStacks), flags)));
		}
		
		public static void UpdateTimer(CharacterBody body, int index, float duration)
		{
			if (!NetworkServer.active) return;
			if (!body.isPlayerControlled && NetworkedTimedBuffsPlugin.onlySyncPlayers.Value) return;
			new SyncTimedBuffUpdate(body.networkIdentity.netId, index, duration).Send(NetworkDestination.Clients);
		}
	}

	
}