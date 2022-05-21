using System;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BubbetsItems
{
	public static class CustomItemTierDefs
	{
		//public static GameObject VoidLunarDroplet;
		
		public static void Init(BubsItemsContentPackProvider bubsItemsContentPackProvider)
		{
			var old = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier3Def.asset").WaitForCompletion();
			//var pickup = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/GenericPickup.prefab").WaitForCompletion();
			//var droplet = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/PickupDroplet.prefab").WaitForCompletion();
			
			/*
			var parent = new GameObject("VoidLunarDropletParent");
			parent.SetActive(false);
			GameObject.DontDestroyOnLoad(parent);
			VoidLunarDroplet = GameObject.Instantiate(old.dropletDisplayPrefab, parent.transform);
			var transform = VoidLunarDroplet.transform.Find("VFX");

			var core = transform.Find("Core").GetComponent<ParticleSystem>();
			var light = transform.Find("Point light").GetComponent<Light>();
			var glow = transform.Find("PulseGlow").GetComponent<ParticleSystem>();

			var mainModule = core.main;
			mainModule.startColor = new ParticleSystem.MinMaxGradient(ColorCatalogPatches.VoidLunarColor);
			
			var mainModule2 = glow.main;
			mainModule2.startColor = new ParticleSystem.MinMaxGradient(ColorCatalogPatches.VoidLunarColor);

			light.color = ColorCatalogPatches.VoidLunarColor;
			*/


			foreach (var itemTierDef in bubsItemsContentPackProvider.itemTierDefs)
			{
				itemTierDef.highlightPrefab = old.highlightPrefab;
				itemTierDef.dropletDisplayPrefab = old.dropletDisplayPrefab;
			}

			BubbetsItemsPlugin.VoidLunarTier = bubsItemsContentPackProvider.itemTierDefs[0];
		}
	}
}