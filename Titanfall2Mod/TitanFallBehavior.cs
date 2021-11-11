using System.Diagnostics.CodeAnalysis;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
#pragma warning disable 618

namespace Titanfall2Mod
{
    public class TitanFallBehavior : NetworkBehaviour
    {
        private float _fixedLifespan;
        //[SyncVar]
        private PilotMaster _pilotMaster;

        public void SetOwner(PilotMaster pilotBehavior)
        {
            _pilotMaster = pilotBehavior;
        }

        [SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
        public void FixedUpdate()
        {
            //if (!isLocalPlayer) return;
            if (_fixedLifespan > 1f)
            {
                SpawnTitan();
                /*
                var titan = Instantiate(Assets.mainContentPack.masterPrefabs[0],
                    null); // TODO replace this hard reference with a soft one

                var tm = titan.GetComponent<TitanMaster>();
                tm.SetOwner(_pilotMaster);

                var master = titan.GetComponent<CharacterMaster>();
                master.bodyPrefab = Prefabs.titanBodyPrefab.gameObject;

                _pilotMaster.titanLoadout.Copy(master.loadout);
                
                NetworkServer.Spawn(titan);
                if (NetworkServer.active) // networkuser.connectionToClient
                    master.GetComponent<NetworkIdentity>().AssignClientAuthority(_pilotMaster.master
                        .playerCharacterMasterController.networkUser.connectionToClient);


                var transform1 = transform;
                master.SpawnBody(transform1.position, transform1.rotation);//.GetComponent<TitanBehavior>().UpdateMaster();

                Destroy(gameObject);
                */
                //gameObject.SetActive(false);
            }

            _fixedLifespan += Time.fixedDeltaTime;
        }

        //[Command]
        void SpawnTitan()
        {
            Debug.Log("DoingCommand");
            /*
            var test = new MasterSummon()
            {
                ignoreTeamMemberLimit = true,
                inventoryItemCopyFilter = (itemIndex) => true,
                inventoryToCopy = _pilotMaster.master.inventory,
                loadout = _pilotMaster.TitanLoadout,
                masterPrefab = Assets.mainContentPack.masterPrefabs[1],
                position = transform.position,
                summonerBodyObject = _pilotMaster.master.GetBodyObject(),
            };
            test.preSpawnSetupCallback += characterMaster => characterMaster.GetComponent<TitanMaster>().SetOwner(_pilotMaster);
            test.Perform();*/
            Destroy(gameObject);
        }
    }
}