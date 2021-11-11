using BepInEx;
using HarmonyLib;

namespace FullPrefabSkins
{
    [BepInPlugin("bubbet.plugins.fullprefabskins", "Full Prefab Skins", "0.3.2")]
    public class FullPrefabSkinsPlugin : BaseUnityPlugin
    {
        public void Awake()
        {
            new Harmony("bubbet.plugins.fullprefabskins").PatchAll();
        }
    }
}