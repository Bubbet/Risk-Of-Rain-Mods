using System;
using System.Linq;
using BubbetsItems.Items;
using HarmonyLib;
using RoR2;
using RoR2.Items;
using UnityEngine;

namespace BubbetsItems.ItemBehaviors
{
	public class ShiftedQuartzBehavior : BaseItemBodyBehavior
	{

		[ItemDefAssociation(useOnServer = true, useOnClient = false)]
		private static ItemDef? GetItemDef()
		{
			var instance = SharedBase.GetInstance<ShiftedQuartz>();
			return instance?.ItemDef;
		}

		private void OnEnable()
		{
			var allButNeutral = TeamMask.allButNeutral;
			var objectTeam = TeamComponent.GetObjectTeam(gameObject);
			if (objectTeam != TeamIndex.None)
			{
				allButNeutral.RemoveTeam(objectTeam);
			}
			var instance = SharedBase.GetInstance<ShiftedQuartz>();
			search = new BullseyeSearch
			{
				maxDistanceFilter = instance.scalingInfos[0].ScalingFunction(stack),
				teamMaskFilter = allButNeutral,
				viewer = body
			};
			indicatorEnabled = true;
		}

		private bool Search()
		{
			search.searchOrigin = gameObject.transform.position;
			search.RefreshCandidates();
			return search.GetResults()?.Any() ?? false;
		}
		private void OnDisable()
		{
			indicatorEnabled = false;
			search = null;
		}

		private void FixedUpdate()
		{
			var instance = SharedBase.GetInstance<ShiftedQuartz>();
			search.maxDistanceFilter = instance.scalingInfos[0].ScalingFunction(stack);
			inside = Search();
		}

		private bool indicatorEnabled
		{
			get => nearbyDamageBonusIndicator;
			set
			{
				if (indicatorEnabled == value)
				{
					return;
				}
				if (value)
				{
					var original = BubbetsItemsPlugin.AssetBundle.LoadAsset<GameObject>("FarDamageBonusIndicator");
					nearbyDamageBonusIndicator = Instantiate(original, body.corePosition, Quaternion.identity);
					var radius = search.maxDistanceFilter / 20f;
					nearbyDamageBonusIndicator.transform.localScale *= radius;
					nearbyDamageBonusIndicator.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(gameObject); // TODO figure out what the fuck this is doing and replace it with my own client and server ran code
					return;
				}
				Destroy(nearbyDamageBonusIndicator);
				nearbyDamageBonusIndicator = null;
			}
		}
		
		private GameObject nearbyDamageBonusIndicator;
		private BullseyeSearch search;
		public bool inside;
	}
}