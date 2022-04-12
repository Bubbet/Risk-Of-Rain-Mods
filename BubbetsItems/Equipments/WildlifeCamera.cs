using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using InLobbyConfig;
using InLobbyConfig.Fields;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace BubbetsItems.Equipments
{
    public class WildlifeCamera : EquipmentBase
    {
        private ConfigEntry<float> _cooldown;
        private ConfigEntry<bool> _filterOutBosses;
        private GameObject _indicator;
        
        private static BuffDef? _buffDef;
        private static BuffDef? BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefSepia");

        public override bool PerformEquipment(EquipmentSlot equipmentSlot)
        { 
            return equipmentSlot.inventory.gameObject.GetComponent<WildLifeCameraBehaviour>().Perform();
        }

        public override void OnEquip(Inventory inventory, EquipmentState? oldEquipmentState)
        {
            base.OnEquip(inventory, oldEquipmentState);
            inventory.gameObject.AddComponent<WildLifeCameraBehaviour>();
        }

        public override void OnUnEquip(Inventory inventory, EquipmentState newEquipmentState)
        {
            base.OnUnEquip(inventory, newEquipmentState);
            Object.Destroy(inventory.gameObject.GetComponent<WildLifeCameraBehaviour>());
        }

        public override bool UpdateTargets(EquipmentSlot equipmentSlot)
        {
            base.UpdateTargets(equipmentSlot);
            
            if (equipmentSlot.stock <= 0) return false;
            var behaviour = equipmentSlot.inventory.GetComponent<WildLifeCameraBehaviour>();
            if (!behaviour || behaviour.target) return false;
            
            equipmentSlot.ConfigureTargetFinderForEnemies();
            equipmentSlot.currentTarget = new EquipmentSlot.UserTargetInfo(equipmentSlot.targetFinder.GetResults().FirstOrDefault(x => x.healthComponent && (!_filterOutBosses.Value && !x.healthComponent.body.isBoss || _filterOutBosses.Value)));

            if (!equipmentSlot.currentTarget.transformToIndicateAt) return false;
            
            equipmentSlot.targetIndicator.visualizerPrefab = _indicator;
            return true;
        }

        protected override void MakeTokens()
        {
            base.MakeTokens();
            AddToken("WILDLIFE_CAMERA_NAME", "Wildlife Camera");
            AddToken("WILDLIFE_CAMERA_PICKUP", "Take a photo of an enemy, and spawn them as an ally later.");
            AddToken("WILDLIFE_CAMERA_DESC", "Take a photo of an enemy, and spawn them as an ally using it again.");
            AddToken("WILDLIFE_CAMERA_LORE", @"A device once used by an elder scrybe to convert woodland creatures into playing cards. 
After some modifications the creatures are no longer bound to a flat card, instead bending and contorting to a living being of paper and ink... 

Luckily they seem friendly enough");
            
            AddToken("SEPIA_ELITE_NAME", "Captured {0}");
        }

        protected override void MakeConfigs()
        {
            base.MakeConfigs();
            _cooldown = configFile.Bind(ConfigCategoriesEnum.General, "Wildlife Camera Cooldown", 25f, "Cooldown for wildlife camera equipment.");
            _filterOutBosses = configFile.Bind(ConfigCategoriesEnum.General, "Wildlife Camera Can Do Bosses", false, "Can the camera capture bosses.");
            _indicator = BubbetsItemsPlugin.AssetBundle.LoadAsset<GameObject>("CameraIndicator");
        }

        
        public override void MakeInLobbyConfig(Dictionary<ConfigCategoriesEnum, List<object>> scalingFunctions)
        {
            base.MakeInLobbyConfig(scalingFunctions);
            var cool = new FloatConfigField(_cooldown.Definition.Key, () => _cooldown.Value, newValue => {
                _cooldown.Value = newValue;
                EquipmentDef.cooldown = newValue;
            });

            var general = scalingFunctions[ConfigCategoriesEnum.General];
            general.Add(ConfigFieldUtilities.CreateFromBepInExConfigEntry(_filterOutBosses));
            general.Add(cool);
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
            var isSepia = __instance.body?.HasBuff(BuffDef) ?? false;
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
        public GameObject target;
        private CharacterBody Body => _body ? _body : _body = _master.GetBody();
        private Loadout _targetLoadout = new();
        private EquipmentState _targetEquipment;
        public void Awake()
        {
            _master = GetComponent<CharacterMaster>();
        }

        public bool Perform()
        {
            if (!target)
            {
                var targ = GetTarget();
                if (targ)
                {
                    var master = targ.healthComponent.body.master;
                    target = MasterCatalog.GetMasterPrefab(master.masterIndex);
                    master.loadout.Copy(_targetLoadout);
                    _targetEquipment = master.inventory.GetEquipment(0);
                    if (target)
                    {
                        AkSoundEngine.PostEvent("WildlifeCamera_TakePicture", Body.gameObject); // Sound does not play for clients, does play for body owner
                        return false;
                    }
                }
            }
            else
            {
                RaycastHit info;
                if (Util.CharacterRaycast(Body.gameObject, GetAimRay(), out info, 50f,
                    LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
                {
                    if (NetworkServer.active)
                    {
                        var summon = new MasterSummon
                        {
                            masterPrefab = target,
                            position = info.point,
                            rotation = Quaternion.identity,
                            //inventoryToCopy = Body.inventory,
                            useAmbientLevel = true,
                            teamIndexOverride = TeamIndex.Player,
                            summonerBodyObject = Body.gameObject,
                            inventorySetupCallback = this,
                            loadout = _targetLoadout
                        };
                        summon.Perform();
                    }

                    AkSoundEngine.PostEvent("WildlifeCamera_Success", Body.gameObject);
                    target = null;
                    return true;
                }
            }
            return false;
        }

        private HurtBox GetTarget()
        {
            return Body.equipmentSlot.currentTarget.hurtBox;
        }

        private Ray GetAimRay()
        {
            var bank = Body.inputBank;
            return bank ? new Ray(bank.aimOrigin, bank.aimDirection) : new Ray(Body.transform.position, Body.transform.forward);
        }

        public void SetupSummonedInventory(MasterSummon masterSummon, Inventory summonedInventory)
        {
            //summonedInventory.SetEquipmentIndex(BubbetsItemsPlugin.ContentPack.eliteDefs[0].eliteEquipmentDef.equipmentIndex); ArtificerExtended throws an nre here. 
            summonedInventory.SetEquipment(new EquipmentState(BubbetsItemsPlugin.ContentPack.eliteDefs[0].eliteEquipmentDef.equipmentIndex, Run.FixedTimeStamp.negativeInfinity, 1), 0); // TODO replace hard reference
            summonedInventory.GetComponent<CharacterMaster>().onBodyStart += BodyStart;
            /* This doesnt work, because the elite system doesnt care about the second slot
            if (_targetEquipment.equipmentIndex != EquipmentIndex.None)
            {
                summonedInventory.SetEquipment(_targetEquipment, 1);
            }*/
        }

        private void BodyStart(CharacterBody obj)
        {
            if(_targetEquipment.equipmentIndex != EquipmentIndex.None && _targetEquipment.equipmentDef &&_targetEquipment.equipmentDef!.passiveBuffDef)
                obj.AddBuff(_targetEquipment.equipmentDef.passiveBuffDef);
        }
    }
}