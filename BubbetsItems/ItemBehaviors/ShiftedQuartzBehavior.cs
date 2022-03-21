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
		private static ItemDef GetItemDef()
		{
			return ShiftedQuartz.instance.ItemDef;
		}

		private void OnEnable()
		{
			var allButNeutral = TeamMask.allButNeutral;
			var objectTeam = TeamComponent.GetObjectTeam(gameObject);
			if (objectTeam != TeamIndex.None)
			{
				allButNeutral.RemoveTeam(objectTeam);
			}
			search = new BullseyeSearch
			{
				maxDistanceFilter = ShiftedQuartz.instance.scalingInfos[0].ScalingFunction(stack),
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
			search.maxDistanceFilter = ShiftedQuartz.instance.scalingInfos[0].ScalingFunction(stack);
			inside = Search();
			var inRadius = inside ? 1f : 0f;
			renderer.material.SetFloat("_ColorMix", inRadius);
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
					var radius = 26f * (search.maxDistanceFilter / 20f);
					nearbyDamageBonusIndicator.transform.localScale = new Vector3(radius, radius, radius);
					nearbyDamageBonusIndicator.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(gameObject);
					renderer = nearbyDamageBonusIndicator.GetComponentInChildren<Renderer>();
					return;
				}
				Destroy(nearbyDamageBonusIndicator);
				nearbyDamageBonusIndicator = null;
			}
		}
		
		private GameObject nearbyDamageBonusIndicator;
		private Renderer renderer;
		private BullseyeSearch search;
		public bool inside;
	}
}