using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using RoR2;

namespace KookehsDropItemPatch
{
    [BepInPlugin("bubbet.plugins.kookehsdropitempatch", "KookehsDropItemPatch", "1.0.0.0")]
    [BepInDependency("KookehsDropItemMod")]
    public class KookehsDropItemPatchPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> _dropLunarNearPlayers;
        public static ConfigEntry<bool> _needScrapperToDrop;
        public static ConfigEntry<float> _needScrapperToRadius;
        public static readonly ItemDef[] _blacklistedItems =
        {
            RoR2Content.Items.TonicAffliction
        };
        
        internal static ManualLogSource Log;
        
        public void Awake()
        {
            _needScrapperToDrop = Config.Bind("General", "Need Scrapper", true,
                "Require the proximity of a scrapper to drop items. Helps prevent abusing dropping for 3d printers.");
            _dropLunarNearPlayers = Config.Bind("General", "Drop Lunar Near Players", true,
                "Require other players nearby to drop lunar items.");
            _needScrapperToRadius = Config.Bind("General", "Search Radius", 50f,
                "Radius to check in for scrappers and other players.");
            // TODO add blacklist populated via config
            Log = Logger;

            var harmony = new Harmony("bubbet.plugins.kookehsdropitempatch");
            harmony.PatchAll();
        }
    }
}