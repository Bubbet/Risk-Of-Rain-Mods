using EntityStates;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using Titanfall2Mod.SkillGeneration;
using UnityEngine;

namespace Titanfall2Mod.SkillStates.Grenades
{
	public class FragGrenade : AimThrowableBase, ISkillStatDef
	{
		public override void OnEnter()
		{
			AkSoundEngine.PostEvent("Play_frag_pinout_drop", gameObject);
			projectilePrefab = Assets.mainAssetBundle.LoadAsset<GameObject>("fragGrenadeProjectile");
			damageCoefficient = 12; // TODO use tf2 values

			base.OnEnter();
		}

		public override void FixedUpdate()
		{
			if (age > 5f)
			{
				// TODO explode
				Debug.Log("lifetime exceeded");
				outer.SetNextStateToMain();
			}
			base.FixedUpdate();
		}
		
		public override void FireProjectile()
		{
			//projectilePrefab.GetComponent<ProjectileImpactExplosion>().lifetime = 5f - age;
			AkSoundEngine.PostEvent("Play_frag_throw", gameObject);
			base.FireProjectile();
		}

		public override void ModifyProjectile(ref FireProjectileInfo fireProjectileInfo)
		{
			base.ModifyProjectile(ref fireProjectileInfo);
			fireProjectileInfo.force = 1000;
			fireProjectileInfo.useFuseOverride = true;
			fireProjectileInfo.fuseOverride = 5f - age;
			Debug.Log("modifying projectile: " + fireProjectileInfo.fuseOverride);
		}

		public static void ApplyStats(SkillDef skillDef)
		{
			skillDef.mustKeyPress = true;
			skillDef.beginSkillCooldownOnSkillEnd = true;
			skillDef.activationStateMachineName = "Weapon";
		}
	}
}