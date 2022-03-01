using System;
using FullPrefabSkins;
using RoR2;
using Titanfall2Mod.SkillGeneration;
using UnityEngine;

namespace Titanfall2Mod
{
    public class TitanBehavior : MonoBehaviour, ILifeBehavior//, ICameraStateProvider
    {
        public CharacterBody body;

        private PilotBehavior _pilotBehavior;
        public PilotBehavior pilotBehavior
        {
            get
            {
                if (!_pilotBehavior) _pilotBehavior = titanMaster.pilotMaster.master.GetBody().GetComponent<PilotBehavior>();
                return _pilotBehavior;
            }
        }
        public VehicleSeat pilotSeat;
        public VehicleSeat rodeoSeat;

        private TitanMaster _titanMaster;
        public TitanMaster titanMaster
        {
            get
            {
                if (!_titanMaster)
                {
                    _titanMaster = body.master.GetComponent<TitanMaster>();
                    if (_titanMaster.health > 0)
                        GetComponent<HealthComponent>().health = _titanMaster.health;
                }
                return _titanMaster;
            }
        }
        private float _seatCooldown;
        private CameraRigController _rigController;
        private bool _enableAi;

        public void Awake()
        {
            if (pilotSeat == null) pilotSeat = GetComponent<VehicleSeat>();

            if (rodeoSeat == null) rodeoSeat = GetComponentInChildren<VehicleSeat>();
            pilotSeat.onPassengerEnter += PilotEntered;
            pilotSeat.onPassengerExit += PilotExited;
            pilotSeat.enterVehicleAllowedCheck.AddCallback(CheckPilotEnterAllowed);
            pilotSeat.enterVehicleAllowedCheck.AddCallback(CheckPilotExitAllowed);

            body = GetComponent<CharacterBody>();
            //body.baseNameToken = Prefabs.pilotBodyPrefab.GetComponents<GenericSkill>()[5].skillFamily.variants[titanMaster.pilotMaster.master.loadout.bodyLoadoutManager.GetSkillVariant(Prefabs.pilotBodyPrefab.bodyIndex, 4)].skillDef.skillNameToken;
            // Cannot get master here because it is set afterwards
            _enableAi = true;
            
            SkinDefExt.onSkinApplyAfter += OnSkinApply;
        }

        public void OnDestroy()
        {
            _titanMaster.health = GetComponent<HealthComponent>().health;
            
            pilotSeat.onPassengerEnter -= PilotEntered;
            pilotSeat.onPassengerExit -= PilotExited;
            SkinDefExt.onSkinApplyAfter -= OnSkinApply;
        }

        private void OnSkinApply(CharacterBody arg1, GameObject o, GameObject arg3)
        {
            Debug.Log("skin apply in behavior");
            if (arg1 != body) return;
            Debug.Log("skin apply in behavior passing");
            var seat = body.GetComponent<VehicleSeat>();
            seat.seatPosition = o.transform.FindInChildren(seat.seatPosition.name);
            seat.exitPosition = o.transform.FindInChildren(seat.exitPosition.name);
            
            body.baseNameToken = SkillGenerator.Pairing["TITAN"].variants[titanMaster.pilotMaster.WhichTitan].skillDef.skillNameToken;
        }

        private void FixedUpdate()
        {
            if (_enableAi)
            {
                _enableAi = false;
                EnableAI();
            }
            _seatCooldown -= Time.fixedDeltaTime;
            if (_seatCooldown > 0) return;
            if (body.inputBank.interact.justPressed) pilotSeat.EjectPassenger();
        }
        /*
        private void FixedUpdate()
        {
            if (!pilotSeat.hasPassenger) return;
            var titanInputs = body.inputBank;
            var bodyInputs = pilotBehavior.body.inputBank;
            titanInputs.skill1.PushState(bodyInputs.skill1.down);
            titanInputs.skill2.PushState(bodyInputs.skill2.down);
            titanInputs.skill3.PushState(bodyInputs.skill3.down);
            titanInputs.skill4.PushState(bodyInputs.skill4.down);
            titanInputs.jump.PushState(bodyInputs.jump.down);
            titanInputs.activateEquipment.PushState(bodyInputs.activateEquipment.down);

            var flag = body.isSprinting;
            if (_sprintInputPressReceived)
            {
                _sprintInputPressReceived = false;
                flag = !flag;
            }

            titanInputs.sprint.PushState(flag);
        }
        private void Update()
        {
            if (!pilotSeat.hasPassenger) return;
            var titanInputs = body.inputBank;
            var bodyInputs = pilotBehavior.body.inputBank;
            titanInputs.moveVector = bodyInputs.moveVector;
            titanInputs.aimDirection = bodyInputs.aimDirection;

            _sprintInputPressReceived |= pilotBehavior.body.master.playerCharacterMasterController.networkUser
                .inputPlayer.GetButtonDown(18);
        }*/

        private void CheckPilotEnterAllowed(CharacterBody b, ref Interactability? i)
        {
            i = Interactability.Available; // todo part of titanmasternre
            return;
            if (_seatCooldown < 0 && b.gameObject == pilotBehavior.gameObject)
            {
                i = Interactability.Available;
                return;
            }

            i = Interactability.Disabled;
        }

        private void CheckPilotExitAllowed(CharacterBody b, ref Interactability? i)
        {
            i = Interactability.Available;
        }

        private void PilotEntered(GameObject obj)
        {
            _seatCooldown = 0.25f;
            pilotBehavior.body.healthComponent.godMode = true;

            Debug.Log("Titan Entered " + obj.name);
            pilotBehavior.GetComponent<Collider>().enabled = false;

            pilotBehavior.body.inputBank.interact.PushState(false);
            UpdateCameras(obj);
            titanMaster.ai.enabled = false;
        }

        private void PilotExited(GameObject obj)
        {
            _seatCooldown = 0.25f;
            Debug.Log("Titan Exited " + obj.name);
            pilotBehavior.body.healthComponent.godMode = false;
            pilotBehavior.GetComponent<Collider>().enabled = true;
            UpdateCameras(obj);
            EnableAI();
        }

        private void EnableAI()
        {
            body.inputBank.moveVector = Vector3.zero; // Probably not needed because the ai should start pushing its values to the input bank
            titanMaster.ai.enabled = true;
        }

        private void UpdateCameras(GameObject characterBodyObject)
        {
            foreach (var cameraRigController in CameraRigController.readOnlyInstancesList)
                if (characterBodyObject && cameraRigController.target == characterBodyObject)
                {
                    _rigController = cameraRigController;
                }
                else if (characterBodyObject && cameraRigController.target == body.gameObject)
                {
                    
                }

            try
            {
                _rigController.enabled = false;
                _rigController.enabled = true;
            }
            catch (NullReferenceException) { /* catch error on game exit */ }
        }

        public void OnDeathStart()
        {
            titanMaster.pilotMaster.OnTitanDeath();
            //TODO implement death behavior ie kill pilot if not emergency eject kit
        }
    }
}