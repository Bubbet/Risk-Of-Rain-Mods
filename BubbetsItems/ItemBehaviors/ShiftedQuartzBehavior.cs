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
				maxDistanceFilter = ShiftedQuartz.radius.Value,
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
					nearbyDamageBonusIndicator.transform.localScale *= ShiftedQuartz.radius.Value / 20f;
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