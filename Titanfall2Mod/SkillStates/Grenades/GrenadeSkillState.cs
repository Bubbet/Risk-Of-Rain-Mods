using EntityStates;
using RoR2.Projectile;
using UnityEngine;

namespace Titanfall2Mod.SkillStates.Grenades
{
    public class GrenadeSkillState : BaseSkillState
    {
        public GameObject ProjectilePrefab = null;

        public GrenadeSkillState(){}

        public void Fire()
        {
            Debug.Log(ProjectilePrefab);
        }
    }
}