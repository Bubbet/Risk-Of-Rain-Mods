using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace Titanfall2Mod
{
    public class TitanPodBehaviour : NetworkBehaviour
    {
        private EntityStateMachine stateMachine;
        private VehicleSeat vehicleSeat;

        private void Awake()
        {
            stateMachine = GetComponent<EntityStateMachine>();
            vehicleSeat = GetComponent<VehicleSeat>();
            vehicleSeat.onPassengerEnter += OnPassengerEnter;
            vehicleSeat.onPassengerExit += OnPassengerExit;
            vehicleSeat.enterVehicleAllowedCheck.AddCallback(CheckEnterAllowed);
            vehicleSeat.exitVehicleAllowedCheck.AddCallback(CheckExitAllowed);
        }

        private void CheckEnterAllowed(CharacterBody arg, ref Interactability? resultoverride)
        {
            resultoverride = Interactability.Disabled;
        }

        private void CheckExitAllowed(CharacterBody arg, ref Interactability? resultoverride)
        {
            resultoverride = Interactability.Available;
        }

        private void OnPassengerEnter(GameObject obj)
        {
            
        }
        
        private void OnPassengerExit(GameObject obj)
        {
            Destroy(gameObject);
        }
    }
}