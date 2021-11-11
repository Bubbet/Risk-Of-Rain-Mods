using KinematicCharacterController;
using RoR2;
using UnityEngine;

namespace Titanfall2Mod
{
    public class PilotMotor : CharacterMotor
    {
        public delegate void OnHitDelegate(Collider hitCollider, Vector3 hitNormal);
        public event OnHitDelegate ONMovementHit;
        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            base.OnMovementHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
            ONMovementHit?.Invoke(hitCollider, hitNormal);
        }
    }
}