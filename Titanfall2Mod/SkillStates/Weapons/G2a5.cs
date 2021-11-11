using System;
using RoR2;
using RoR2.Skills;
using Titanfall2Mod.SkillGeneration;
using UnityEngine;
using ES = EntityStates;
using Random = UnityEngine.Random;

namespace Titanfall2Mod.SkillStates.Weapons
{
    public class G2a5 : TitanfallFalloffBulletState, ISkillStatDef
    {
        //public static readonly string[] distStates = {"ShotPointBlank", "ShotClose", "ShotMed", "ShotDist"};
        public static readonly string[] distEvents =
        {
            "G2A5ShotPointBlank",
            "G2A4ShotCloseRandom",
            "G2A4ShotMedRandom",
            "G2A4ShotDistRandom"
        };

        public override void OnEnter()
        {
            //_pilotBehavior = GetComponent<PilotBehavior>();
            hitEffectPrefab = ES.Commando.CommandoWeapon.FirePistol2.hitEffectPrefab;
            tracerEffectPrefab = ES.Commando.CommandoWeapon.FirePistol2.tracerEffectPrefab;
            muzzleFlashPrefab = ES.Commando.CommandoWeapon.FirePistol2.muzzleEffectPrefab;

            falloffRanges = new float[] { 1500, 2000, 2000 };
            falloffDamages = new float[] {40, 35, 35};

            spreadBloomValue = 0.12f;
            muzzleName = "Muzzle";
            fireRate = 5.5f;
            //baseDuration = 60f / 330f; //0.142857f; // 420 rpm = 1/(420/60) == 60/420
            useSmartCollision = true; // TODO maybe not for the g2
            
            //AkSoundEngine.SetState("G2A4ShotSwitchGroup", distStates[dist]);
            //AkSoundEngine.PostEvent("G2A4ShotSwitch", gameObject);

            var dist = Mathf.Clamp(Mathf.RoundToInt(Vector3.Distance(gameObject.transform.position, LocalUserManager.GetFirstLocalUser().cameraRigController.transform.position) / 75f), 0, distEvents.Length-1);
            AkSoundEngine.PostEvent(distEvents[dist], gameObject);
            
            base.OnEnter();
        }
        
        public static void ApplyStats(SkillDef skillDef)
        {
            skillDef.mustKeyPress = true;
            skillDef.rechargeStock = 14;
            skillDef.baseMaxStock = 14;
            skillDef.resetCooldownTimerOnUse = true;
            skillDef.baseRechargeInterval = 2.64f;
            ((GunSkillDef) skillDef).fireInterval = Config.FireRateMult.Value / 5.5f;
        }
    }
}