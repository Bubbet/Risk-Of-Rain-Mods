using System;
using FullPrefabSkins;
using RoR2;
using Titanfall2Mod.SkillGeneration;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 618

namespace Titanfall2Mod
{
    public class PilotMaster : NetworkBehaviour, IMeterBuilding
    {
        public CharacterMaster master;
        public PlayerCharacterMasterController playerMasterController;
        public GenderSkins skins;// = Assets.mainAssetBundle.LoadAsset<GenderSkins>("PilotGenderSkins");
        [NonSerialized] public TitanMaster TitanMaster;
        
        //[SyncVar][NonSerialized] public int boostCount;

        //Networked
        [NonSerialized] public bool TitanCalled; // Cannot be property checking if titanmaster is null because titanmaster is null while titan dropping
        private float _meter;
        private float _tempmeter;
        public int boostCount;

        public bool titanReady;
        private bool _boostGiven;

        [NonSerialized] public Loadout TitanLoadout; // TODO this might need networking, but ideally it would recreate the same behavior from the vanilla loadout
        [NonSerialized] public uint WhichTitan;

        private (float ratio, int count)? _selectedBoost;

        public float BoostRatio
        {
            get
            {
                if (_selectedBoost == null)
                    _selectedBoost = SkillGenerator.BoostRatios[(int) master.loadout.bodyLoadoutManager.GetSkillVariant(Prefabs.pilotBodyPrefab.bodyIndex, 3)];
                return _selectedBoost.Value.ratio;
            }
        }
        private float BoostLimit
        {
            get
            {
                if (_selectedBoost == null)
                    _selectedBoost = SkillGenerator.BoostRatios[(int) master.loadout.bodyLoadoutManager.GetSkillVariant(Prefabs.pilotBodyPrefab.bodyIndex, 3)];
                var val = _selectedBoost.Value.count;
                return val < 0 ? 1 : val;
            }
        }
        //private bool boostGiven;

        public void Awake()
        {
            Debug.LogWarning("Pilot Master Awake");
            
            var pilotIndex = Prefabs.pilotBodyPrefab.bodyIndex;
            var titanIndex = Prefabs.titanBodyPrefab.bodyIndex;

            var loadout = master.loadout;
            var pilotbody = loadout.bodyLoadoutManager;
            TitanLoadout = new Loadout();
            var titanbody  = TitanLoadout.bodyLoadoutManager;
            
            titanReady = true;
            _meter = 1f;

            WhichTitan = pilotbody.GetSkillVariant(pilotIndex, 4);
            var titan = WhichTitan;

            var skillSlot = 0;
            
            // Copy titan skin to display correct from pilot loadout
            titanbody.SetSkinIndex(titanIndex, titan);
            titanbody.SetSkillVariant(titanIndex, skillSlot, titan);
            titanbody.SetSkillVariant(titanIndex, ++skillSlot, titan);
            titanbody.SetSkillVariant(titanIndex, ++skillSlot, titan);
            titanbody.SetSkillVariant(titanIndex, ++skillSlot, titan);
            titanbody.SetSkillVariant(titanIndex, ++skillSlot, titan);

            skillSlot++;
            /*
            var skill = Prefabs.titanBodyPrefab.gameObject.GetComponents<GenericSkill>()[skillSlot]; // This probably causes desyncs 5
            var newName = TitanKitsLoadout.SkillDefSkillName.skillFamily.variants[titan].skillDef.skillName;
            var newFamily = Assets.mainContentPack.skillFamilies.First(x => x.defaultSkillDef.skillName.StartsWith(newName));
            TitanKitsLoadout.SkillFamily.SetValue(skill, newFamily);
            titanbody.SetSkillVariant(titanIndex, skillSlot, pilotbody.GetSkillVariant(pilotIndex, skillSlot - 1)); // titan specific kit*/

            skillSlot++;
            titanbody.SetSkillVariant(titanIndex, skillSlot, pilotbody.GetSkillVariant(pilotIndex, skillSlot - 1)); // titan kit
            
            SkinDefExt.onSkinSwap += OnSkinSwap;
            GlobalEventManager.onCharacterDeathGlobal += OnGlobalDeath;
            master.onBodyStart += OnBodyStart;
        }
        
        // Called on character creation/run start
        public void Init(CharacterMaster characterMaster,
            PlayerCharacterMasterController playerCharacterMasterController)
        {
            master = characterMaster;
            playerMasterController = playerCharacterMasterController;
        }

        private void OnBodyStart(CharacterBody obj)
        {
            for (var i = 0; i < boostCount; i++)
            {
                AddSpecialStock(obj);
            }
            if(titanReady && !TitanCalled) AddSpecialStock(obj);
        }

        public void OnDestroy()
        {
            SkinDefExt.onSkinSwap -= OnSkinSwap;
            GlobalEventManager.onCharacterDeathGlobal -= OnGlobalDeath;
        }

        private void OnGlobalDeath(DamageReport obj)
        {
            if (titanReady) return;
            if (obj.attacker == master.GetBodyObject())
            {
                _tempmeter = Mathf.Clamp01(_tempmeter + Config.MeterGainKillAsPilot.Value * (obj.victimIsElite ? Config.MeterGainMultiplierForElite.Value : 1f));
                DoMeterChecks();
            }
        }

        private void OnSkinSwap(ref SkinDef skinDef, CharacterBody body, ModelLocator arg3)
        {
            if (body != master.GetBody()) return;
            //Debug.Log("skindef in swap is");
            //Debug.Log(skinDef);
            //Debug.Log(skins);
            if (skinDef.nameToken.StartsWith("BUB_PRIME"))
            {
                var titanIndex = Prefabs.titanBodyPrefab.bodyIndex;
                var titan = TitanLoadout.bodyLoadoutManager.GetSkinIndex(titanIndex);
                if (titan != 6)
                {
                    //Debug.Log("Prime titan should be loaded: " + titan);
                    TitanLoadout.bodyLoadoutManager.SetSkinIndex(titanIndex, titan + 7);
                }
            }
            
            var typ = (int) master.loadout.bodyLoadoutManager.GetSkillVariant(Prefabs.pilotBodyPrefab.bodyIndex, 2);
            skinDef = skinDef.nameToken == "BUB_MALE_PILOT" || skinDef.nameToken == "BUB_PRIME_MALE_PILOT" ? skins.male[typ] : skins.female[typ];
        }

        private float _secondTimer;

        public void FixedUpdate()
        {
            _secondTimer += Time.fixedDeltaTime;
            if (_secondTimer > 1f && !TitanCalled)
            {
                _secondTimer = 0f;
                _meter = Mathf.Clamp01(_meter + Config.MeterGainPerSecondAsPilot.Value);
                _tempmeter = Mathf.Clamp01(_tempmeter - Config.MeterLossPerSecondAsPilot.Value);
                DoMeterChecks();
            }
        }

        private void DoMeterChecks()
        {
            if (_meter + _tempmeter > BoostRatio && boostCount < BoostLimit && !_boostGiven)
            {
                _boostGiven = true;
                boostCount++;//TODO maybe replace this with getting stock count of utility -1 if titan is ready
                AddSpecialStock();
            }

            if (_meter + _tempmeter > 0.999f && !titanReady)
            {
                titanReady = true;
                _meter = 1f;
                _tempmeter = 0f;
                AddSpecialStock();
            }
        }

        private void AddSpecialStock()
        {
            AddSpecialStock(master.GetBody());
        }
        private void AddSpecialStock(CharacterBody characterBody)
        {
            Debug.Log("Adding Stock");
            characterBody.skillLocator.special.AddOneStock();
        }

        public void OnTitanCall()
        {
            TitanCalled = true;
            _boostGiven = false;
            titanReady = false;
        }

        public void OnTitanDeath()
        {
            _meter = 0f;
            _tempmeter = 0f;
            TitanCalled = false;
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            var old = base.OnSerialize(writer, initialState);
            Debug.LogWarning("PMOnSerialize");
            //writer.Write(TitanCalled); // maybe not needed?
            writer.Write(_meter);
            writer.Write(_tempmeter);
            writer.Write(boostCount);
            return old;
        }
        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            base.OnDeserialize(reader, initialState);
            Debug.LogWarning("PMOnDeserialize");
            //TitanCalled = reader.ReadBoolean();
            _meter = reader.ReadSingle();
            _tempmeter = reader.ReadSingle();
            boostCount = reader.ReadInt32();
        }

        public float GetBoostRatio()
        {
            if (_selectedBoost != null) return _selectedBoost.Value.ratio;
            return GetMeter() + GetTempMeter();
        }

        public float GetTempMeter()
        {
            return _tempmeter;
        }

        public float GetMeter()
        {
            return _meter;
        }
    }
}