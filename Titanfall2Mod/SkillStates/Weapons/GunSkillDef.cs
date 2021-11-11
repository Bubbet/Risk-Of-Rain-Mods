using JetBrains.Annotations;
using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace Titanfall2Mod.SkillStates.Weapons
{
    public class GunSkillDef : SkillDef
    {
        private float _cooldown;
        public float fireInterval;

        public override bool IsReady(GenericSkill skillSlot)
        {
            if (_cooldown > 0.001f) return false;
            return HasRequiredStockAndDelay(skillSlot); 
        }

        public override void OnExecute(GenericSkill skillSlot)
        {
            _cooldown = fireInterval / skillSlot.characterBody.attackSpeed;
            base.OnExecute(skillSlot);
        }

        public override void OnFixedUpdate(GenericSkill skillSlot)
        {
            _cooldown -= Time.fixedDeltaTime;
            base.OnFixedUpdate(skillSlot);
        }
    }
}