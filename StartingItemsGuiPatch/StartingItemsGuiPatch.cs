using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Phedg1Studios.StartingItemsGUI;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Resources = UnityEngine.Resources;

namespace StartingItemsGuiPatch
{
    [BepInPlugin("bubbet.plugins.startingitemsguipatch", "StartingItemsGuiPatch", "1.4.0")]
    [BepInDependency("com.Phedg1Studios.StartingItemsGUI")]
    public class StartingItemsGuiPatch : BaseUnityPlugin
    {
        public static ConfigEntry<bool> AllModes;
        public static ConfigEntry<bool> ItemsOverTime;
        public static ConfigEntry<bool> ItemsOverTimeMixTier;
        public static ConfigEntry<bool> CommandNumberKeyChoice;
        public static ConfigEntry<bool> SteamCloudSaves;
        public static ConfigEntry<bool> MidJoinPatch;
        private GameObject _commandCubePrefab;
        public static ManualLogSource log;

        public static StartingItemsGuiPatch instance;

        public bool shouldSendItems;
        //public static Dictionary<uint, Dictionary<int, int>> LocalItems = new Dictionary<uint, Dictionary<int, int>>();

        public static Dictionary<uint, Dictionary<int, int>> Items => GameManager.items;

        public Dictionary<uint, Dictionary<int, int>> GetItems()
        {
            return Items;
        }

        public Dictionary<uint, List<bool>> GetStatus()
        {
            return GameManager.status;
        }

        public Dictionary<uint, CharacterMaster> GetMasters()
        {
            return GameManager.characterMasters;
        }

        public List<Coroutine> GetCorotuines()
        {
            return GameManager.spawnItemCoroutines;
        }
        
        public void Awake()
        {
            AllModes = Config.Bind("General", "All Earning Modes", true, "Enables all earning mods to work together, meaning you dont have to choose which one you want.");
            ItemsOverTime = Config.Bind("General", "Items Over Time", true, "Gain your starting items as you open chests instead of all at once at the start. (Disabled in command artifact)");
            ItemsOverTimeMixTier = Config.Bind("General", "Items Over Time Tier Matching", true, "Restricts the items over time to be of the same tier.");
            CommandNumberKeyChoice = Config.Bind("General", "Command Number Key Choices", true, "Enables pressing 1-9 for choosing the first x command items.");
            SteamCloudSaves = Config.Bind("General", "Steam Cloud Saves", true, "Enables syncing profiles to steam cloud.");
            MidJoinPatch = Config.Bind("General", "Mid Join Patch", true, "Enables the patch for mid join when someone hasnt previously been in the game. Disable if you run into issues, but do notify me. (experimental)");

            instance = this;
            log = Logger;
            Data.modEnabled = true;
            NetworkingAPI.RegisterMessageType<RequestItemsMessage>();

            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) =>
            {
                Logger.LogInfo("Scene Loaded:" + scene.name);
                if (scene.name == "lobby")
                {
                    Data.localUsers.Clear();
                    Data.SetForcedMode(-1);
                    GameManager.ClearItems();
                    UIDrawer.CreateCanvas();
                }
            };
            
            var harmony = new Harmony("bubbet.plugins.startingitemsguipatch");
            harmony.PatchAll();

            if (ItemsOverTime.Value)
            {
                _commandCubePrefab = Resources.Load<GameObject>("Prefabs/NetworkedObjects/CommandCube");
                PickupDropletController.onDropletHitGroundServer += OnDropletHitGroundServer;
            }

            Run.onRunStartGlobal += RunOnonRunStartGlobal;
            NetworkUser.OnPostNetworkUserStart += networkUser =>
            {
                if (NetworkClient.active && shouldSendItems)
                {
                    if (networkUser.isLocalPlayer)
                    {
                        Debug.Log("sendingItems for " + networkUser.netId.Value);
                        GameManager.SendItems(networkUser);
                    }

                    return;
                }
                if (!NetworkServer.active) return;
                if (GameManager.status.ContainsKey(networkUser.netId.Value)) return;
                //Debug.LogError("Setting up in progress user: " + networkUser.userName);
                //Debug.Log(GameManager.status.Count);
                GameManager.status.Add(networkUser.netId.Value, new List<bool>
                {
                    false,
                    false,
                    false
                });
                //Debug.Log(GameManager.status.Count);
                GameManager.items.Add(networkUser.netId.Value, new Dictionary<int, int>());
                GameManager.modes.Add(networkUser.netId.Value, -1);
                Phedg1Studios.StartingItemsGUI.StartingItemsGUI.startingItemsGUI.characterMasterCoroutines.Add(
                    Phedg1Studios.StartingItemsGUI.StartingItemsGUI.startingItemsGUI.StartCoroutine(
                        Phedg1Studios.StartingItemsGUI.StartingItemsGUI.startingItemsGUI.GetMasterController(
                            networkUser)));

                new RequestItemsMessage() {_connectionID = networkUser.netId.Value}.Send(NetworkDestination.Clients);
            };

            /*
            if (MidJoinPatch.Value)
            {
                GameNetworkManager.onClientConnectGlobal += connection =>
                {
                    var what = gameObject.GetComponent<Phedg1Studios.StartingItemsGUI.StartingItemsGUI>();
                    Logger.LogInfo(what);
                    var meth = typeof(Phedg1Studios.StartingItemsGUI.StartingItemsGUI).GetMethod("OnRunStartGlobal", BindingFlags.Instance | BindingFlags.NonPublic);
                    Logger.LogWarning(meth);
                    if (NetworkServer.active) return;
                    meth?.Invoke(what, new object[] {null});
                };
            }*/
        }

        private void RunOnonRunStartGlobal(Run obj)
        {
            if (!ItemsOverTime.Value) return;
            Data.modEnabled = true;
            Logger.LogError("Run Starting");
            GameManager.status.Clear();
            GameManager.items.Clear();
            GameManager.modes.Clear();
        }

        private void OnDropletHitGroundServer(ref GenericPickupController.CreatePickupInfo createPickupInfo, ref bool shouldSpawn)
        {
            PickupIndex pickupIndex = createPickupInfo.pickupIndex;
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef == null || pickupDef.itemIndex == ItemIndex.None && pickupDef.equipmentIndex == EquipmentIndex.None)
            {
                return;
            }
            
            if (!PickupStartItemHelper.AnyoneHas(pickupDef) || pickupDef.equipmentIndex != RoR2Content.Equipment.QuestVolatileBattery.equipmentIndex && PickupStartItemHelper.NearScrapper(createPickupInfo) != ItemIndex.None || PickupStartItemHelper.NearPrinter(createPickupInfo) || RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Command)) return;
            GameObject go = Instantiate(_commandCubePrefab, createPickupInfo.position, createPickupInfo.rotation);
            go.GetComponent<PickupIndexNetworker>().NetworkpickupIndex = pickupIndex;

            go.AddComponent<CommandNameChangeBehaviour>();

            go.GetComponent<PickupPickerController>().SetOptionsServer(new[]{
                new PickupPickerController.Option
                {
                    available = true,
                    pickupIndex = pickupDef.pickupIndex
                }
            });

            NetworkServer.Spawn(go);
            shouldSpawn = false;
        }
    }

    public class RequestItemsMessage : INetMessage, ISerializableObject
    {
        public uint _connectionID;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(_connectionID);
        }

        public void Deserialize(NetworkReader reader)
        {
            _connectionID = reader.ReadUInt32();
        }

        public void OnReceived()
        {
            StartingItemsGuiPatch.instance.shouldSendItems = true;
            /*
            var user = NetworkUser.readOnlyInstancesList.FirstOrDefault(x => x.netId.Value == _connectionID && x.localUser != null);
            Debug.Log("message recieved");
            if (user is null) return;
            Debug.Log(user.netId.Value);
            if (user == default) return;
            Debug.Log(user.localUser.userProfile.fileName);
            Data.modEnabled = true;
            GameManager.SendItems(user);
            */
        }
    }
}