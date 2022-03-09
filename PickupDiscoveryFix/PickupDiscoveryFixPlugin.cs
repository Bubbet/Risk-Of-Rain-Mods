using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using RoR2;
using UnityEngine;
using Zio;

[assembly: HG.Reflection.SearchableAttribute.OptIn]
namespace PickupDiscoveryFix
{
    [BepInPlugin("com.bubbet.pickupdiscoveryfix", "Pickup Discovery Fix", "0.2.0")]
    public class PickupDiscoveryFixPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        public void Awake()
        {
            Log = Logger;
            new Harmony(Info.Metadata.GUID).PatchAll();
        }

        //[SystemInitializer(typeof(UserProfile))]
        public static void Test()
        {
            Debug.Log("killing remote profiles");
            var meth = typeof(SteamworksRemoteStorageFileSystem).GetMethod("DeleteFileImpl", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (UPath path in RoR2Application.cloudStorage.EnumeratePaths("/UserProfiles"))
            {
                meth?.Invoke((SteamworksRemoteStorageFileSystem) RoR2Application.cloudStorage,
                    new object[] {path});
            }
        }
    }
}