using System.Linq;
using BubbetsItems.Items;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Behaviours
{
	public class ShiftedQuartzVisualUpdate : MonoBehaviour
	{
		
		private void Awake()
		{
			renderer = GetComponentInChildren<Renderer>();
		}

		private void Startup()
		{
			started = true;
			body = transform.parent.GetComponent<CharacterBody>();
			
			var allButNeutral = TeamMask.allButNeutral;
			var objectTeam = body.teamComponent.teamIndex;
			if (objectTeam != TeamIndex.None)
			{
				allButNeutral.RemoveTeam(objectTeam);
			}
			search = new BullseyeSearch
			{
				teamMaskFilter = allButNeutral,
				viewer = body
			};
			renderer.material.SetFloat("_Color2BaseAlpha", ShiftedQuartz.visualTransparency.Value);
			if (ShiftedQuartz.visualOnlyForAuthority.Value && !body.hasEffectiveAuthority)
			{
				renderer.material.SetColor("_Color", Color.clear);
				renderer.material.SetColor("_Color2", Color.clear);
			}
		}

		private bool Search()
		{
			search.searchOrigin = gameObject.transform.position;
			search.RefreshCandidates();
			return search.GetResults()?.Any() ?? false;
		}

		private void FixedUpdate()
		{
			if(!transform.parent) return;
			if(!started) Startup();
			search.maxDistanceFilter = transform.localScale.z / 2f;
			inside = Search();
			var inRadius = inside ? 1f : 0f;
			renderer.material.SetFloat("_ColorMix", inRadius);
		}
		
		private GameObject nearbyDamageBonusIndicator;
		private Renderer renderer;
		private BullseyeSearch search;
		public bool inside;
		private CharacterBody body;
		private bool started;
	}
}