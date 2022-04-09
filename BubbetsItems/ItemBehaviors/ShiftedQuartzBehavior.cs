using System.Linq;
using BubbetsItems.Items;
using RoR2;
using RoR2.Items;
using UnityEngine;

namespace BubbetsItems.ItemBehaviors
{
	public class ShiftedQuartzBehavior : BaseItemBodyBehavior
	{

		[ItemDefAssociation(useOnServer = true, useOnClient = true)]
		private static ItemDef GetItemDef()
		{
			return (ShiftedQuartz.Instance!.ItemDef is not null ? ShiftedQuartz.Instance!.ItemDef : default)!;
		}

		private void OnEnable()
		{
			var allButNeutral = TeamMask.allButNeutral;
			var objectTeam = TeamComponent.GetObjectTeam(gameObject);
			if (objectTeam != TeamIndex.None)
			{
				allButNeutral.RemoveTeam(objectTeam);
			}
			_search = new BullseyeSearch
			{
				maxDistanceFilter = ShiftedQuartz.Instance!.ScalingInfos[0].ScalingFunction(stack),
				teamMaskFilter = allButNeutral,
				viewer = body
			};
			IndicatorEnabled = true;
		}

		private bool Search()
		{
			if (_search == null) return false;
			
			_search.searchOrigin = gameObject.transform.position;
			_search.RefreshCandidates();
			return _search.GetResults()?.Any() ?? false;
		}
		private void OnDisable()
		{
			IndicatorEnabled = false;
			_search = null;
		}

		private void FixedUpdate()
		{
			if (_search == null) return;
			_search.maxDistanceFilter = ShiftedQuartz.Instance!.ScalingInfos[0].ScalingFunction(stack);
			inside = Search();
			var inRadius = inside ? 1f : 0f;
			if (!_renderer) return;
			_renderer!.material.SetFloat(ColorMix, inRadius);
		}

		private bool IndicatorEnabled
		{
			get => _nearbyDamageBonusIndicator;
			set
			{
				if (IndicatorEnabled == value)
				{
					return;
				}
				if (value)
				{
					if (_search == null) return;
					var original = BubbetsItemsPlugin.AssetBundle!.LoadAsset<GameObject>("FarDamageBonusIndicator");
					_nearbyDamageBonusIndicator = Instantiate(original, body.corePosition, Quaternion.identity);

					var radius = _search.maxDistanceFilter / 20f;
					_nearbyDamageBonusIndicator.transform.localScale *= radius;

					_nearbyDamageBonusIndicator.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(gameObject);
					_renderer = _nearbyDamageBonusIndicator.GetComponentInChildren<Renderer>();
					return;
				}
				Destroy(_nearbyDamageBonusIndicator);
				_nearbyDamageBonusIndicator = null;
			}
		}
		
		private GameObject? _nearbyDamageBonusIndicator;
		private Renderer? _renderer;
		private BullseyeSearch? _search;
		public bool inside;
		private static readonly int ColorMix = Shader.PropertyToID("_ColorMix");
	}
}