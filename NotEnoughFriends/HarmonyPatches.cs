using HarmonyLib;
using RoR2;
using UnityEngine;

namespace NotEnoughFriends
{
	[HarmonyPatch]
	public static class HarmonyPatches
	{
		[HarmonyPrefix, HarmonyPatch(typeof(PlatformSystems), nameof(PlatformSystems.InitNetworkManagerSystem))]
		public static void InitNetworkManagerSystem(GameObject networkManagerPrefabObject)
		{
			networkManagerPrefabObject.GetComponent<NetworkManagerConfiguration>().MaxConnections = NotEnoughFriendsPlugin.LobbySize.Value;
		}
	}
}