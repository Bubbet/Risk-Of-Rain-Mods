using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HG.Reflection;
using RoR2;
using RoR2.UI;

//[assembly: SearchableAttribute.OptInAttribute]
namespace DamageHistory
{
    [BepInPlugin("bubbet.damagehistory", "Damage History", "1.2.0")]
    public class DamageHistoryPlugin : BaseUnityPlugin
    {
        public static List<HUD> HUDs = new List<HUD>();
        public static ManualLogSource Log;
        public static ConfigEntry<bool> mustHoldTab;

        public void Awake()
        {
            Log = Logger;
            mustHoldTab = Config.Bind("General", "Must Hold Tab", false, "Require holding tab to see damage history.");
            new Harmony(Info.Metadata.GUID).PatchAll();
            HUD.shouldHudDisplay += CreateHud;
            CharacterBody.onBodyStartGlobal += BodyStart;
        }

        //[ConCommandAttribute(commandName = "stopsound", flags = ConVarFlags.None, helpText = "Stop all running AKEngine sounds.")]
        public void StopSound()
        {
            AkSoundEngine.StopAll();
        }

        private void BodyStart(CharacterBody obj)
        {
            if (obj.teamComponent.teamIndex != TeamIndex.Player) return;
            obj.gameObject.AddComponent<DamageHistoryBehavior>();
        }

        private void CreateHud(HUD hud, ref bool shoulddisplay)
        {
            if (HUDs.Contains(hud)) return;
            hud.gameObject.AddComponent<DamageHistoryHUD>();
            HUDs.Add(hud);
        }
    }
}