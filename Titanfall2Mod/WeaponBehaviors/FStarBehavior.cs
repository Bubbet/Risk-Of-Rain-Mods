using System;
using RoR2;
using UnityEngine;

namespace Titanfall2Mod.WeaponBehaviors
{
    public class FStarBehavior : MonoBehaviour
    {
        private bool _stuck;
        private float _stopWatch;

        public void OnStick()
        {
            _stuck = true;
        }

        public void FixedUpdate()
        {
            if (_stuck)
            {
                _stopWatch += Time.fixedDeltaTime;
                if (_stopWatch >= 0.2f)
                {
                    _stopWatch -= 0.2f;
                    var attack = new BlastAttack()
                    {
                        position = transform.position,
                        damageType = DamageType.IgniteOnHit,
                        baseDamage = 50f,
                        radius = 15f
                    };
                    Debug.Log("DoingDamageTick");
                }
            }
        }
    }
}