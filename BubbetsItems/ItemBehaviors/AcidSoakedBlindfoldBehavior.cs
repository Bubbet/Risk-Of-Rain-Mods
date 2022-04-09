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
			return (AcidSoakedBlindfold.Instance!.ItemDef is not null ? AcidSoakedBlindfold.Instance!.ItemDef : default)!;
		}

		private void FixedUpdate()
		{
			var master = body.master; 
			if (!master) return;
			var maxCount = AcidSoakedBlindfold.Instance!.ScalingInfos[0].ScalingFunction(stack);
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
			var instance = obj.spawnedInstance;
			if (!instance) return;
			var master = instance.GetComponent<CharacterMaster>();
			if (!master) return;
			master.teamIndex = body.master.teamIndex;
			//master.inventory;

			var greenChance = AcidSoakedBlindfold.Instance!.ScalingInfos[2].ScalingFunction(stack);

			var list1 = Run.instance.availableTier1DropList;
			var list2 = Run.instance.availableTier2DropList;
			
			for (var i = 0; i < AcidSoakedBlindfold.Instance.ScalingInfos[1].ScalingFunction(stack); i++)
			{
				master.inventory.GiveItem(Run.instance.treasureRng.nextNormalizedFloat < greenChance
					? list2[Run.instance.treasureRng.RangeInt(0, list2.Count)].pickupDef.itemIndex
					: list1[Run.instance.treasureRng.RangeInt(0, list1.Count)].pickupDef.itemIndex);
			}

			var deployable = instance.AddComponent<Deployable>();
			if (!deployable) return;
			deployable.ownerMaster = body.master;
			body.master.AddDeployable(deployable, Slot);
		}
	}
}