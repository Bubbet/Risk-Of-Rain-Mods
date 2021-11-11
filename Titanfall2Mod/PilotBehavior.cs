using FullPrefabSkins;
using RoR2;
using UnityEngine;

namespace Titanfall2Mod
{
    public class PilotBehavior : MonoBehaviour
    {
        public CharacterBody body;
        public Vector3 lastTouchNormal;
        public bool touchingWall;
        private PilotMaster _pilotMaster;

        private PilotMaster pilotMaster
        {
            get
            {
                if (_pilotMaster == null)
                    _pilotMaster = body.masterObject.GetComponent<PilotMaster>(); 
                return _pilotMaster;
            }
        }

        public GameObject Weapon { get; set; }

        //private CharacterMotor _motor;
        public void Awake()
        {
            //HarmonyPatches.onCollisionEnter += OnCollision;
            body = GetComponent<CharacterBody>();
            //body.characterMotor.onMovementHit += OnCollision;
            SkinAppliedComponent.skinApplied += SkinApplied;
            ((PilotMotor) body.characterMotor).ONMovementHit += OnCollision;
        }

        private void SkinApplied(GameObject o){ if (o == gameObject) ResolveWeapon(); }

        public void OnDestroy()
        {
            ((PilotMotor) body.characterMotor).ONMovementHit -= OnCollision;
            SkinAppliedComponent.skinApplied -= SkinApplied;
            // ReSharper disable once DelegateSubtraction
            //HarmonyPatches.onCollisionEnter -= OnCollision;
            //body.characterMotor.onMovementHit -= OnCollision;
        }
        private void ResolveWeapon()
        {
            var whichGun = body.master.loadout.bodyLoadoutManager.GetSkillVariant(Prefabs.pilotBodyPrefab.bodyIndex, 0);
            var weapons = Assets.mainAssetBundle.LoadAsset<PrefabPairing>("WeaponPairing");
            var model = GetComponent<ModelLocator>().modelTransform;
            var characterModel = model.GetComponent<CharacterModel>();
            var childLocator = model.GetComponent<ChildLocator>();
            var weapon = weapons.prefabs[whichGun];
            var parent = childLocator.FindChild("GunBase");
            //Weapon = Instantiate(weapon, parent);
            //NetworkServer.Spawn(Weapon);
            var display = new CharacterModel.ParentedPrefabDisplay();
            display.Apply(characterModel, weapon, parent, Vector3.zero, Quaternion.identity, Vector3.one);
            characterModel.parentedPrefabDisplays.Add(display);

            Weapon = display.instance;
            var weaponLocator = Weapon.GetComponent<ChildLocator>();
            ref var muzzlePair = ref childLocator.transformPairs[childLocator.FindChildIndex("Muzzle")];
            var trans = weaponLocator.FindChild("Muzzle");
            body.aimOriginTransform = trans;
            muzzlePair.transform = trans;
        }

        private void OnCollision(Collider collider, Vector3 hitNormal)
        {
            hitNormal.y = 0;
            if (!(hitNormal.magnitude > 0.9)) return;
            lastTouchNormal = hitNormal;
            touchingWall = true;
        }

        public void FixedUpdate()
        {
            touchingWall = false;
        }
    }
}