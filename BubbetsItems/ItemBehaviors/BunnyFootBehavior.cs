using System;
using BubbetsItems.Items;
using RoR2;
using RoR2.Items;
using UnityEngine;

namespace BubbetsItems.ItemBehaviors
{
	public class BunnyFootBehavior : BaseItemBodyBehavior
	{
		public Vector3 hitGroundVelocity;
		public float hitGroundTime;

		[ItemDefAssociation(useOnServer = true, useOnClient = true)]
		private static ItemDef? GetItemDef()
		{
			var instance = SharedBase.GetInstance<BunnyFoot>();
			return instance?.ItemDef;
		}

		private void OnEnable()
		{
			if (!body.characterMotor) return;
			body.characterMotor.onHitGroundAuthority += HitGround;
		}

		private void OnDisable()
		{
			if (!body.characterMotor) return;
			body.characterMotor.onHitGroundAuthority -= HitGround;
		}

		private void HitGround(ref CharacterMotor.HitGroundInfo hitgroundinfo)
		{
			hitGroundVelocity = hitgroundinfo.velocity;
			hitGroundTime = Time.time;
		}
	}
}