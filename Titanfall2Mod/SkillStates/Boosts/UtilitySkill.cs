using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 618

namespace Titanfall2Mod.SkillStates
{
    public class UtilitySkill : BaseSkillState
    {
        private PilotMaster _pilotMaster;
        private GameObject _titanMarkerPrefab;

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("Enter Utility State");
            _titanMarkerPrefab = Assets.mainAssetBundle.LoadAsset<GameObject>("TitanfallMarker");
            _pilotMaster = characterBody.master.GetComponent<PilotMaster>();
            DoBehavior();
        }

        public virtual void BoostAbility()
        {
            //_pilotMaster.BoostCount--;
        }

        public void CallTitan()
        {
            //if (!NetworkServer.active) return;
            //if (!isAuthority) return;
            
            var aimRay = GetAimRay();
            if (!Physics.Raycast(aimRay.origin, aimRay.direction, out var hit)) return;
            _pilotMaster.OnTitanCall();
            
            /*
            var titanfall = Object.Instantiate(_titanMarkerPrefab, null);
            titanfall.transform.position = hit.point;
            titanfall.GetComponent<TitanFallBehavior>().SetOwner(_pilotMaster);
            NetworkServer.Spawn(titanfall);*/
            
            var test = new MasterSummon()
            {
                ignoreTeamMemberLimit = true,
                inventoryItemCopyFilter = (itemIndex) => true,
                inventoryToCopy = _pilotMaster.master.inventory,
                loadout = _pilotMaster.TitanLoadout,
                masterPrefab = Assets.mainContentPack.masterPrefabs[1],
                position = hit.point,
                summonerBodyObject = _pilotMaster.master.GetBodyObject(),
            };
            test.preSpawnSetupCallback += characterMaster => characterMaster.GetComponent<TitanMaster>().SetOwner(_pilotMaster);
            var master = test.Perform();

            GameObject gameObject = Object.Instantiate(Resources.Load<GameObject>("prefabs/networkedobjects/survivorpod"), hit.point, Quaternion.identity); //Assets.mainContentPack.networkedObjectPrefabs[2], hit.point, Quaternion.identity);
            gameObject.GetComponent<VehicleSeat>().AssignPassenger(master.GetBodyObject());
            NetworkServer.Spawn(gameObject);
        }

        /*
        [Command]
        private void CmdSpawnMarker(Vector3 where)
        {
            var titanfall = Object.Instantiate(_titanMarkerPrefab, null);
            titanfall.transform.position = where;
            //titanfall.GetComponent<TitanFallBehavior>().SetOwner(_pilotMaster);
            NetworkServer.Spawn(titanfall);
        }

        protected static void InvokeCmdCmdSpawnMarker(NetworkBehaviour obj, NetworkReader reader)
        {
            if (!NetworkServer.active)
            {
                Debug.LogError("Command CmdSpawnMarker called on client.");
                return;
            }
            ((UtilitySkill) obj).CmdSpawnMarker(reader.ReadVector3());
        }

        static UtilitySkill()
        {
            
        }
        */

        public void DoBehavior()
        {
            if (_pilotMaster.titanReady && !_pilotMaster.TitanCalled)
            {
                Debug.Log("before call titan");
                CallTitan();
            }
            else if (_pilotMaster.boostCount > 0)
            {
                Debug.Log("before boost call");
                BoostAbility();
            }

            outer.SetNextStateToMain();
        }
    }
}