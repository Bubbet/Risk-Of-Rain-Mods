using System;
using RoR2;
using Titanfall2Mod.SkillGeneration;
using UnityEngine;
using UnityEngine.Networking;

namespace Titanfall2Mod
{
    public class TitanMaster : NetworkBehaviour, IMeterBuilding
    {
        public CharacterMaster master;
        public PilotMaster pilotMaster;
        public TitanAI ai;

        //Networked
        public float health;
        public float meter;
        public int coreActiveCount;
        
        public bool hasCore => meter > 0.9999f;

        public void Awake()
        {
            master = GetComponent<CharacterMaster>();
            ai = GetComponent<TitanAI>();
            master.onBodyStart += MasterOnBodyStart;
            GlobalEventManager.onCharacterDeathGlobal += OnGlobalDeath;
            GlobalEventManager.onServerDamageDealt += OnGlobalDamage;
        }

        public void OnDestroy()
        {
            GlobalEventManager.onCharacterDeathGlobal -= OnGlobalDeath;
            GlobalEventManager.onServerDamageDealt -= OnGlobalDamage;
        }

        private void OnGlobalDamage(DamageReport obj)
        {
            if (obj.attacker == master.GetBodyObject() && obj.victimIsBoss)
                meter += Config.MeterGainDamageToBossesAsTitan.Value * (obj.victimIsElite ? Config.MeterGainMultiplierForElite.Value : 1f);
        }

        private void OnGlobalDeath(DamageReport obj)
        {
            if (obj.attacker == master.GetBodyObject())
                meter += Config.MeterGainKillAsTitan.Value * (obj.victimIsElite ? Config.MeterGainMultiplierForElite.Value : 1f);
        }

        private void MasterOnBodyStart(CharacterBody obj)
        {
            //obj.GetComponent<NetworkIdentity>().AssignClientAuthority(pilotMaster.master.playerCharacterMasterController.networkUser.connectionToClient); // Todo throws nre as client
        }

        public void SetOwner(PilotMaster pilotMasterIn)
        {
            pilotMaster = pilotMasterIn;
            pilotMasterIn.TitanMaster = this;

            //pilotMaster.whichTitan;
            var equipment = SkillGenerator.TitanCores[(int) pilotMaster.WhichTitan];//Assets.mainContentPack.equipmentDefs[(int) pilotMaster.whichTitan];
            master.inventory.SetEquipmentIndex(equipment.equipmentIndex); //EquipmentIndex.None);
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            var old = base.OnSerialize(writer, initialState);
            writer.Write(health);
            writer.Write(meter);
            writer.Write(coreActiveCount);
            Debug.LogWarning("TMOnSerialize");
            return old;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            base.OnDeserialize(reader, initialState);
            health = reader.ReadSingle();
            meter = reader.ReadSingle();
            coreActiveCount = reader.ReadInt32();
            Debug.LogWarning("TMOnDeserialize");
        }

        public float GetBoostRatio()
        {
            return GetMeter();
        }

        public float GetTempMeter()
        {
            return 0f;
        }

        public float GetMeter()
        {
            return meter;
        }
    }
}