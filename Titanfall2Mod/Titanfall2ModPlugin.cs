using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using EntityStates.Engi.EngiWeapon;
using HarmonyLib;
using RoR2;
using RoR2.UI;
using Titanfall2Mod;
using UnityEngine;
using Debug = UnityEngine.Debug;

[assembly: HG.Reflection.SearchableAttribute.OptIn]
[BepInPlugin("bubbet.titanfall2mod", "Titanfall 2 Mod", "1.0.0.0")]
[BepInDependency(ExtraSkillSlots.ExtraSkillSlotsPlugin.GUID)]
//[BepInDependency(R2API.R2API.PluginGUID), R2API.Utils.R2APISubmoduleDependency("SoundAPI")]
public class Titanfall2ModPlugin : BaseUnityPlugin
{
    public static Titanfall2ModPlugin instance;
    public static ManualLogSource logger;

    private void Awake()
    {
        var where = Info.Location;
        //SleepCurrentThreadUntilDebuggerIsAttached();
        instance = this;
        logger = Logger;
        Titanfall2Mod.Config.Init(Config);
        Assets.Init();

        var harmony = new Harmony("bubbet.titanfall2mod");
        harmony.PatchAll();
        //harmony.Patch(typeof(LoadoutPanelController).GetNestedType("Row", BindingFlags.NonPublic).GetMethod("FromSkillSlot"), null, null, null, null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod("FromSkillSlot")));
        //harmony.Patch(typeof(Loadout.BodyLoadoutManager).GetNestedType("BodyLoadout", BindingFlags.NonPublic).GetMethod("ToXml"), null, null, null, null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod("UpdateSkillName")));

        
        HUD.shouldHudDisplay += CreateHud;

        Run.onPlayerFirstCreatedServer += Run_onPlayerFirstCreatedServer;
        //UserProfile.onLoadoutChangedGlobal += TitanKitsLoadout.UserProfileOnonLoadoutChangedGlobal;
    }

    public static List<HUD> registeredHuds = new List<HUD>();
    private void CreateHud(HUD hud, ref bool shoulddisplay)
    {
        if (registeredHuds.Contains(hud)) return; // TODO remove this probably
        if (hud.targetBodyObject == null) return;
        if (hud.targetBodyObject.GetComponent<CharacterBody>().bodyIndex !=
            Prefabs.pilotBodyPrefab.bodyIndex) return;
        var tf2Hud = Instantiate(Assets.mainAssetBundle.LoadAsset<GameObject>("PilotMeter"), hud.healthBar.barContainer, false);
        /*
        var localpos = tf2Hud.transform.position;
        var localscale = tf2Hud.transform.lossyScale;
        tf2Hud.transform.parent = hud.healthBar.barContainer;
        tf2Hud.transform.localPosition = localpos;
        tf2Hud.transform.localScale = localscale;
        */
        
        tf2Hud.transform.rotation = hud.healthBar.barContainer.rotation;
        tf2Hud.GetComponent<MeterBehavior>().hud = hud;
        registeredHuds.Add(hud);
    }

    private void Run_onPlayerFirstCreatedServer(Run run,
        PlayerCharacterMasterController playerCharacterMasterController)
    {
        if (playerCharacterMasterController.master.bodyPrefab.name != "PilotBody") return;
        Logger.LogInfo("Pilot Body has been located");
        //playerCharacterMasterController.gameObject.AddComponent<PilotMaster>()
            //.Init(playerCharacterMasterController.master, playerCharacterMasterController);
    }
    
    public static void SleepCurrentThreadUntilDebuggerIsAttached() {
        var projectName = Assembly.GetCallingAssembly().GetName().Name;

        Debug.LogWarning("Waiting for debugger to be attached...");
        while (true) {
            try {
                foreach (var window in Process.GetProcesses()) {
                    var windowTitle = window.MainWindowTitle;
                    if (windowTitle == "Risk Of Rain Mods [Debugger]") {
                        Debug.LogWarning("Debugger attached");
                        return;
                    }
                }
            }
            catch (Exception) {

            }

            Thread.Sleep(1000);
        }
    }
}
