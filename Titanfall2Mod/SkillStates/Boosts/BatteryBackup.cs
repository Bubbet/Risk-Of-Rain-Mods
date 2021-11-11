using RoR2.Skills;
using Titanfall2Mod.SkillGeneration;
using UnityEngine;

namespace Titanfall2Mod.SkillStates.Boosts
{
    public class BatteryBackup : UtilitySkill, ISkillStatDef
    {
        public override void BoostAbility()
        {
            // TODO give pilot a battery
            Debug.Log("Battery ability called");
            base.BoostAbility(); // decrease the amount of charges
        }
        
        public static void ApplyStats(SkillDef skillDef)
        {
            skillDef.mustKeyPress = true;
            skillDef.baseMaxStock = 1;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.baseRechargeInterval = 1;
        }
    }
}