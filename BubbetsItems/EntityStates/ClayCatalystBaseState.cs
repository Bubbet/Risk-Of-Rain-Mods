using System;
using BubbetsItems.Items.BarrierItems;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace BubbetsItems.EntityStates
{
	public class ClayCatalystBaseState : EntityState
	{
		private HoldoutZoneController holdoutZone;
		private float radius;
		private TeamIndex teamIndex;
		private BuffWard indicator;
		private float stopwatch;

		public override void OnEnter()
		{
			base.OnEnter();
			Transform parent = transform.parent;
			if (parent)
			{
				holdoutZone = parent.GetComponentInParent<HoldoutZoneController>();
			}
			TeamFilter teamFilter = GetComponent<TeamFilter>();
			teamIndex = teamFilter ? teamFilter.teamIndex : TeamIndex.None;

			if (NetworkServer.active)
			{
				var inst = SharedBase.GetInstance<ClayCatalyst>();
				if (inst == null) return;

				var amount = Util.GetItemCountForTeam(teamIndex, inst.ItemDef.itemIndex, false);
				radius = inst.scalingInfos[0].ScalingFunction(amount);
			} 
			indicator = GetComponent<BuffWard>();
			indicator.radius = radius;
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (!NetworkServer.active) return;
			if (Math.Abs(holdoutZone.charge - 1f) < 0.01f)
			{
				Destroy(gameObject);
			}
		}

		public override void OnSerialize(NetworkWriter writer)
		{
			base.OnSerialize(writer);
			writer.Write(radius);
		}

		public override void OnDeserialize(NetworkReader reader)
		{
			base.OnDeserialize(reader);
			radius = reader.ReadSingle();
		}
	}
}