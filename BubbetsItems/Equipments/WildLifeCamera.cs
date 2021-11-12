using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using InLobbyConfig;
using InLobbyConfig.Fields;
using RoR2;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BubbetsItems.Equipments
{
    public class WildLifeCamera : EquipmentBase
    {
        private ConfigEntry<float> _cooldown;

        public override bool PerformEquipment(EquipmentSlot equipmentSlot)
        {
            equipmentSlot.inventory.gameObject.GetComponent<WildLifeCameraBehaviour>().Perform();
            return true;
        }

        public override void OnUnEquip(Inventory inventory, EquipmentState newEquipmentState)
        {
            base.OnUnEquip(inventory, newEquipmentState);
            Object.Destroy(inventory.gameObject.GetComponent<WildLifeCameraBehaviour>());
        }

        public override void OnEquip(Inventory inventory, EquipmentState? oldEquipmentState)
        {
            base.OnEquip(inventory, oldEquipmentState);
            inventory.gameObject.AddComponent<WildLifeCameraBehaviour>();
        }
        
        protected override void MakeTokens()
        {
            base.MakeTokens();
            AddToken("WILDLIFE_CAMERA_NAME", "Wildlife Camera");
            AddToken("WILDLIFE_CAMERA_DESC", "Take a picture of a creature to spawn them later.");
            AddToken("WILDLIFE_CAMERA_PICKUP", "Take pictures of the local wildlife.");
            AddToken("WILDLIFE_CAMERA_LORE", @"A device once used by an elder scrybe to convert woodland creatures into playing cards. 
After some modifications the creatures are no longer bound to a flat card, instead bending and contorting to a living being of paper and ink... 

Luckily they seem friendly enough");
            
            AddToken("SEPIA_ELITE_NAME", "Captured {0}");
        }

        protected override void MakeConfigs(ConfigFile configFile)
        {
            base.MakeConfigs(configFile);
            _cooldown = configFile.Bind("General", "Wildlife Camera Cooldown", 25f, "Cooldown for wildlife camera equipment.");
        }

        public override void MakeInLobbyConfig(object modConfigEntryObj)
        {
            base.MakeInLobbyConfig(modConfigEntryObj);
            var modConfigEntry = (ModConfigEntry) modConfigEntryObj;

            var list = modConfigEntry.SectionFields.ContainsKey("General") ? modConfigEntry.SectionFields["General"].ToList() : new List<IConfigField>();
            
            var cool = new FloatConfigField(_cooldown.Definition.Key, () => _cooldown.Value, newValue => {
                _cooldown.Value = newValue;
                EquipmentDef.cooldown = newValue;
            });
            list.Add(cool);
            modConfigEntry.SectionFields["General"] = list;
        }

        protected override void PostEquipmentDef()
        {
            base.PostEquipmentDef();
            EquipmentDef.cooldown = _cooldown.Value;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.UpdateOverlays))]
        public static void UpdateOverlays(CharacterModel __instance)
        {
            // ReSharper disable once Unity.NoNullPropagation
            var isSepia = __instance.body?.HasBuff(BubbetsItemsPlugin.ContentPack.buffDefs[1]) ?? false;
            AddOverlay(__instance, BubbetsItemsPlugin.AssetBundle.LoadAsset<Material>("SepiaMaterial"), isSepia);
        }

        public static void AddOverlay(CharacterModel self, Material overlayMaterial, bool condition)
        {
            if (self.activeOverlayCount >= CharacterModel.maxOverlays)
            {
                return;
            }
            if (condition)
            {
                Material[] array = self.currentOverlays;
                int num = self.activeOverlayCount;
                self.activeOverlayCount = num + 1;
                array[num] = overlayMaterial;
            }
        }
    }

    public class WildLifeCameraBehaviour : MonoBehaviour, MasterSummon.IInventorySetupCallback
    {
        private CharacterMaster _master;
        private CharacterBody _body;
        private BullseyeSearch search;
        private GameObject target;
        private CharacterBody Body => _body ? _body : _body = _master.GetBody();
        public void Awake()
        {
            _master = GetComponent<CharacterMaster>();
            search = new BullseyeSearch
            {
                teamMaskFilter = TeamMask.all,
                maxDistanceFilter = 50f,
                maxAngleFilter = 90f,
                sortMode = BullseyeSearch.SortMode.DistanceAndAngle
            };
        }

        public void Perform()
        {
            if (!target)
            {
                var targ = GetTarget();
                if (targ)
                {
                    target = MasterCatalog.GetMasterPrefab(targ.healthComponent.body.master.masterIndex);
                    if (target)
                    {
                        AkSoundEngine.PostEvent("WildlifeCamera_TakePicture", Body.gameObject);
                        var slot = Body.inventory.activeEquipmentSlot;
                        var equipmentState = Body.inventory.GetEquipment(slot);
                        Body.inventory.SetEquipment(new EquipmentState(equipmentState.equipmentIndex, equipmentState.chargeFinishTime, (byte) (equipmentState.charges + 1)), slot);
                    }
                }
            }
            else
            {
                RaycastHit info;
                if (Util.CharacterRaycast(Body.gameObject, GetAimRay(), out info, 50f,
                    LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
                {
                    /*
                    var friend = Instantiate(target);
                    var master = friend.GetComponent<CharacterMaster>();
                    master.teamIndex = _master.teamIndex;
                    //master.Respawn(info.point, Quaternion.identity);
                    master.SpawnBody(info.point, Quaternion.identity);
                    Debug.Log("spawning dude");
                    //friend.transform.position = info.point;
                    //var body = friend.GetComponent<CharacterBody>();
                    //body.teamComponent.teamIndex = Body.teamComponent.teamIndex;
                    target = null;
                    */

                    var summon = new MasterSummon
                    {
                        masterPrefab = target,
                        position = info.point,
                        rotation = Quaternion.identity,
                        //inventoryToCopy = Body.inventory,
                        useAmbientLevel = true,
                        teamIndexOverride = TeamIndex.Player,
                        summonerBodyObject = Body.gameObject,
                        inventorySetupCallback = this
                    };
                    summon.Perform();
                    AkSoundEngine.PostEvent("WildlifeCamera_Success", Body.gameObject);
                    target = null;
                }
            }
        }

        public HurtBox GetTarget()
        {
            var ray = GetAimRay();
            if (Body.teamComponent)
            {
                search.teamMaskFilter.RemoveTeam(Body.teamComponent.teamIndex);
            }
            search.searchOrigin = ray.origin;
            search.searchDirection = ray.direction;
            search.RefreshCandidates();
            search.FilterOutGameObject(Body.gameObject);
            return search.GetResults().FirstOrDefault();
        }
        
        protected Ray GetAimRay()
        {
            var bank = Body.inputBank;
            return bank ? new Ray(bank.aimOrigin, bank.aimDirection) : new Ray(Body.transform.position, Body.transform.forward);
        }

        public void SetupSummonedInventory(MasterSummon masterSummon, Inventory summonedInventory)
        {
            summonedInventory.SetEquipmentIndex(BubbetsItemsPlugin.ContentPack.eliteDefs[0].eliteEquipmentDef.equipmentIndex);
        }
    }
}