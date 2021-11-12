using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace FullPrefabSkins
{
    [BepInPlugin("bubbet.plugins.fullprefabskins", "Full Prefab Skins", "0.3.3")]
    public class FullPrefabSkinsPlugin : BaseUnityPlugin
    {
        public void Awake()
        {
            new Harmony("bubbet.plugins.fullprefabskins").PatchAll();
        }
    }
}