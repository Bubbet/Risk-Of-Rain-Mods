using System;
using System.Linq;
using EntityStates;
using RoR2;
using UnityEngine;

namespace Titanfall2Mod.SkillStates.Weapons
{
    public class TitanfallFalloffBulletState : GenericBulletBaseState
    {
        public float[] falloffRanges; // from largest to smallest
        public float[] falloffDamages; // from farthest to closest
        public float fireRate;

        public override void OnEnter()
        {
            maxDistance = falloffRanges[falloffRanges.Length-1] * Config.RangeMult.Value + Config.RangeAdd.Value;
            //baseDuration = 2f; //1/(fireRate * Config.FireRateMult.Value); // 1/2.5 14.1
            base.OnEnter();
        }

        private BulletAttack _mostRecentBulletAttack;
        public override void ModifyBullet(BulletAttack bulletAttack)
        {
            _mostRecentBulletAttack = bulletAttack;
            bulletAttack.hitCallback = HitCallback;
            bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
            base.ModifyBullet(bulletAttack);
        }

        private bool HitCallback(ref BulletAttack.BulletHit hitinfo)
        {
            int rangemod = 0;
            foreach (var range in falloffRanges)
            {
                if (hitinfo.distance < range * Config.RangeMult.Value) break;
                rangemod++;
            }
            
            Debug.Log(falloffRanges[Math.Min(rangemod, falloffRanges.Length - 1)] * Config.RangeMult.Value);
            Debug.Log(rangemod);
            Debug.Log(hitinfo.distance);
            _mostRecentBulletAttack.damage = damageStat * falloffDamages[Math.Min(rangemod, falloffRanges.Length - 1)] / Config.DamageNerf.Value;
            return _mostRecentBulletAttack.DefaultHitCallback(ref hitinfo);
        }
    }
}