using System;
using BubbetsItems.Items;
using RoR2;
using RoR2.Items;
using UnityEngine;

namespace BubbetsItems.ItemBehaviors
{
	public class AcidSoakedBlindfoldBehavior : BaseItemBodyBehavior
	{
		private float _deployableTime;
		private const float TimeBetweenRespawns = 30f;
		private const float TimeBetweenRetries = 1f;
		private const DeployableSlot Slot = (DeployableSlot) 340502;
		
		[ItemDefAssociation(useOnServer = true, useOnClient = false)]
		private static ItemDef GetItemDef()
		{
			var instance = SharedBase.GetInstance<AcidSoakedBlindfold>();
			return instance.ItemDef;
		}

		private void FixedUpdate()
		{
			var master = body.master; 
			if (!master) return;
			var instance = SharedBase.GetInstance<AcidSoakedBlindfold>();
			var maxCount = instance.scalingInfos[0].ScalingFunction(stack);
			var count = master.GetDeployableCount(Slot);
			if (count >= maxCount) return;
			_deployableTime -= Time.fixedDeltaTime;
			if (_deployableTime > 0) return;

			var request = new DirectorSpawnRequest(
				LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/CharacterSpawnCards/cscVermin"),
				new DirectorPlacementRule
				{
					placementMode = DirectorPlacementRule.PlacementMode.Approximate,
					minDistance = 3f,
					maxDistance = 40f,
					spawnOnTarget = transform
				}, RoR2Application.rng
			) {summonerBodyObject = gameObject, onSpawnedServer = VerminSpawnedServer};
			DirectorCore.instance.TrySpawnObject(request);
			_deployableTime = master.GetDeployableCount(Slot) >= maxCount ? TimeBetweenRetries : TimeBetweenRespawns;
		}

		private void VerminSpawnedServer(SpawnCard.SpawnResult obj)
		{
			var instances = obj.spawnedInstance;
			if (!instances) return;
			var master = instances.GetComponent<CharacterMaster>();
			if (!master) return;
			master.teamIndex = body.master.teamIndex;
			//master.inventory;

			var instance = SharedBase.GetInstance<AcidSoakedBlindfold>();
			var greenChance = instance.scalingInfos[2].ScalingFunction(stack);

			var runInstance = Run.instance;
			var list1 = runInstance.availableTier1DropList;
			var list2 = runInstance.availableTier2DropList;
			var tRng = runInstance.treasureRng;
			
			for (var i = 0; i < instance.scalingInfos[1].ScalingFunction(stack); i++)
			{
				master.inventory.GiveItem(tRng.nextNormalizedFloat < greenChance
					? list2[tRng.RangeInt(0, list2.Count)].pickupDef.itemIndex
					: list1[tRng.RangeInt(0, list1.Count)].pickupDef.itemIndex);
			}

			var deployable = instances.AddComponent<Deployable>();
			if (!deployable) return;
			deployable.ownerMaster = body.master;
			body.master.AddDeployable(deployable, Slot);
		}
	}
}