using BepInEx;
using BepInEx.Configuration;
using EnforcerPlugin;
using Starstorm2.Cores;

namespace NemesisEnforcerStarStormConfig
{
    [BepInDependency("com.TeamMoonstorm.Starstorm2", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.EnforcerGang.Enforcer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("bubbet.plugins.NemForcerStarStormConfig", "NemForcerStarStormConfig", "1.0.0")]
    public class NemesisiEnforcerStarStormConfigPlugin : BaseUnityPlugin
    {
        private ConfigEntry<bool> _disableStarStormNemforcerBoss;

        public void Awake()
        {
            _disableStarStormNemforcerBoss = Config.Bind("General", "Disable Nemesis Starstorm Boss", true,
                "Disables the compatibility between enforcer mod and starstorm.");
            if (_disableStarStormNemforcerBoss.Value)
            {
                EnforcerPlugin.EnforcerPlugin.starstormInstalled = false;
                Logger.LogInfo("Succeeded in removing nemforcer boss: " + VoidCore.nemesisSpawns.Remove(VoidCore.nemesisSpawns.Find(x => x.masterPrefab == NemforcerPlugin.minibossMaster)));
            }
        }
    }
}