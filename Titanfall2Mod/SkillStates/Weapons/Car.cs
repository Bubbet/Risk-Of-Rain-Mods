using EntityStates;
using RoR2.Skills;
using Titanfall2Mod.SkillGeneration;
using Titanfall2Mod.SkillStates.Weapons;
using UnityEngine;
using ES = EntityStates;

namespace Titanfall2Mod.SkillStates
{
    public class Car : TitanfallFalloffBulletState, ISkillStatDef
    {
        public override void OnEnter()
        {
            hitEffectPrefab = ES.Commando.CommandoWeapon.FirePistol2.hitEffectPrefab;
            tracerEffectPrefab = ES.Commando.CommandoWeapon.FirePistol2.tracerEffectPrefab;
            muzzleFlashPrefab = ES.Commando.CommandoWeapon.FirePistol2.muzzleEffectPrefab;
            
            falloffRanges = new float[] { 1000, 1500, 3000 };
            falloffDamages = new float[] { 25, 13, 10 };

            spreadBloomValue = 0.12f;
            muzzleName = "Muzzle";
            fireRate = 14.1f;

            AkSoundEngine.PostEvent("ShootCar", gameObject);
            /*
            uint ids = 0;
            uint[] playingIds = { };
            AkSoundEngine.GetPlayingIDsFromGameObject(gameObject, ref ids, playingIds);
            foreach (var playingId in playingIds)
            {
                Debug.Log(playingId);
            }
            Debug.Log(ids);
            if (ids < 1)
                AkSoundEngine.PostEvent("StartCarShootPointBlank", gameObject);
            if(skillLocator.primary.stock <= 1) AkSoundEngine.PostEvent("StopCarShootPointBlank", gameObject);
            */
            //baseDuration = 60f / 846f; //0.142857f; // 420 rpm = 1/(420/60) == 60/420 
            useSmartCollision = true;
            base.OnEnter();
        }

        public override void OnExit()
        {
            //if(!inputBank.skill1.down)
                //AkSoundEngine.PostEvent("StopCarShootPointBlank", gameObject);
            base.OnExit();
        }

        public static void ApplyStats(SkillDef skillDef)
        {
            skillDef.rechargeStock = 30;
            skillDef.baseMaxStock = 30;
            skillDef.resetCooldownTimerOnUse = true;
            skillDef.baseRechargeInterval = 2.53f;
            ((GunSkillDef) skillDef).fireInterval = Config.FireRateMult.Value / 14.1f;
        }
    }
}