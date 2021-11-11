using System;
using UnityEngine;

namespace Titanfall2Mod.SkillStates.Grenades
{
    public class Firestar : GrenadeSkillState
    {
        public Firestar()
        {
            ProjectilePrefab = Assets.mainAssetBundle.LoadAsset<GameObject>("FirestarPrefab");
        }
        
        public override void OnEnter()
        {
            base.OnEnter();
            Fire();
            throw new Exception("Test");
            outer.SetNextStateToMain();
        }
    }
}