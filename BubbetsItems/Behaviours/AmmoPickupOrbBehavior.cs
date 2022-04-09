using RoR2;
using RoR2.Orbs;
using UnityEngine;

namespace BubbetsItems.Behaviours
{
    public class AmmoPickupOrb : GenericDamageOrb
    {
        public override void OnArrival() => target.healthComponent.GetComponent<SkillLocator>().ApplyAmmoPack();

        public override GameObject GetOrbEffect() =>
            BubbetsItemsPlugin.AssetBundle!.LoadAsset<GameObject>("AmmoPickupOrb");
    }

    public class AmmoPickupOrbBehavior : MonoBehaviour
    {
        private TrailRenderer? _trail;
        private float _localTime;
        private Vector3 _startPos;
        private Vector3 _initialVelocity;
        public Transform? TargetTransform { get; set; }
        public float TravelTime { get; set; } = 1f;

        private void Awake()
        {
            _trail = GetComponent<TrailRenderer>();
        }

        private void Start()
        {
            if (!_trail) return;
            _localTime = 0f;
            _startPos = transform.position;
            _initialVelocity = (Vector3.up * 4f + Random.insideUnitSphere * 1f);
            _trail!.startWidth = 0.05f;
        }

        private void Update()
        {
            _localTime += Time.deltaTime;
            if (!TargetTransform)
            {
                var effectData = GetComponent<EffectComponent>().effectData;
                TargetTransform = effectData?.ResolveHurtBoxReference()?.transform; // nre is being thrown here
                if (effectData != null) TravelTime = effectData.genericFloat;
                if (!TargetTransform)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            var num = Mathf.Clamp01(_localTime / TravelTime);
            transform.position = CalculatePosition(_startPos, _initialVelocity, TargetTransform!.position, num);
            if (num >= 1f) Destroy(gameObject);
        }

        private static Vector3 CalculatePosition(Vector3 startPos, Vector3 initialVelocity, Vector3 targetPos, float t)
        {
            var a = startPos + initialVelocity * t;
            var t2 = t * t * t;
            return Vector3.LerpUnclamped(a, targetPos, t2);
        }
    }
}