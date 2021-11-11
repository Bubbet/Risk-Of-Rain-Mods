using System.Collections.Generic;
using System.Linq;
using EntityStates;
using EntityStates.Barrel;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace ModItemsChest
{
    public class ModChestBehavior : NetworkBehaviour
    {
        public Transform dropTransform;
        public float tier1Chance = ModItemsChestPlugin.tier1Chance.Value; //0.8f;
        public float tier2Chance = ModItemsChestPlugin.tier2Chance.Value; //0.2f;
        public float tier3Chance = ModItemsChestPlugin.tier3Chance.Value; //0.01f;
        public float dropUpVelocityStrength = 20f;
        public float dropForwardVelocityStrength = 2f;
        private PickupIndex _dropPickup;
        public SerializableEntityStateType openState = new SerializableEntityStateType(typeof (Opening));
        private EntityStateMachine _stateMachine;
        private EntityState _openStateInited;
        private float _lifeTime;
        private bool _opened;

        private void Awake()
        {
            var purchase = GetComponent<PurchaseInteraction>();
            purchase.onPurchase.AddListener(Open);
            
            _stateMachine = GetComponent<EntityStateMachine>();
            _openStateInited = EntityStateCatalog.InstantiateState(openState);
            
            WeightedSelection<List<PickupIndex>> selector = new WeightedSelection<List<PickupIndex>>();

            Add(ItemTier.Tier1, tier1Chance);
            Add(ItemTier.Tier2, tier2Chance);
            Add(ItemTier.Tier3, tier3Chance);
            
            void Add(ItemTier tier, float chance)
            {
                if (chance <= 0.0) return;

                var tierLi = DisableModItemsInNormalChests.ModdedItemDefs.Where(x => x.tier == tier).Select(x => PickupCatalog.FindPickupIndex(x.itemIndex)).ToList();
                
                selector.AddChoice(tierLi, chance);
            }
            
            PickFromList(selector.Evaluate(Run.instance.treasureRng.nextNormalizedFloat));
            
            if (dropTransform != null) return; 
            dropTransform = transform;
        }

        [Server]
        private void PickFromList(List<PickupIndex> dropList)
        {
            if (!NetworkServer.active) return;
            _dropPickup = PickupIndex.none;
            if (dropList == null || dropList.Count <= 0) return;
            _dropPickup = Run.instance.treasureRng.NextElementUniform(dropList);
        }
        
        private void Open(Interactor interactor)
        {
            if (!(bool) _stateMachine) return;
            _stateMachine.SetNextState(_openStateInited);
        }

        private void FixedUpdate()
        {
            if (_stateMachine.state == _openStateInited)
            {
                _lifeTime += Time.fixedDeltaTime;
                if (!_opened && _lifeTime > 0.8f)
                {
                    PickupDropletController.CreatePickupDroplet(_dropPickup, dropTransform.position + Vector3.up * 1.5f,
                        Vector3.up * dropUpVelocityStrength + dropTransform.forward * dropForwardVelocityStrength);
                    _opened = true;
                }
            }
        }
    }
}