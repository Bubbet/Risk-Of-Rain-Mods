using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BubbetsItems
{
	public class BubPickupDisplayCustom : MonoBehaviour
	{
		[SystemInitializer(typeof(GenericPickupController))]
		public static void ModifyGenericPickup()
		{
			pickup = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/GenericPickup.prefab").WaitForCompletion();

			var pickupDisplay = pickup.transform.Find("PickupDisplay").gameObject;
			pickupDisplay.AddComponent<BubPickupDisplayCustom>();
			
			var voidSystem = pickup.transform.Find("VoidSystem");
			var voidSystemLoops = voidSystem.Find("Loops");
			var lunarSystem = pickup.transform.Find("LunarSystem");
			var lunarSystemLoops = lunarSystem.Find("Loops");
			voidLunarSystem = new GameObject("VoidLunarSystem");
			voidLunarSystem.SetActive(false);
			DontDestroyOnLoad(voidLunarSystem);
			

			//Setup Loops
			var voidLunarSystemLoops = new GameObject("Loops");
			voidLunarSystemLoops.transform.SetParent(voidLunarSystem.transform);

			var swirls = Instantiate(lunarSystemLoops.Find("Swirls").gameObject, voidLunarSystemLoops.transform);
			var mainModule = swirls.GetComponent<ParticleSystem>().main;
			mainModule.startColor = new ParticleSystem.MinMaxGradient(ColorCatalogPatches.VoidLunarColor);
			
			Instantiate(voidSystemLoops.Find("DistantSoftGlow").gameObject, voidLunarSystemLoops.transform);
			Instantiate(voidSystemLoops.Find("Glowies").gameObject, voidLunarSystemLoops.transform);
			var pointLight = Instantiate(voidSystemLoops.Find("Point Light").gameObject, voidLunarSystemLoops.transform);
			pointLight.GetComponent<Light>().color = ColorCatalogPatches.VoidLunarColor;

			//Setup Bursts
			Instantiate(lunarSystem.Find("Burst").gameObject, voidLunarSystem.transform).name = "LunarBurst";
			Instantiate(voidSystem.Find("Burst").gameObject, voidLunarSystem.transform).name = "VoidBurst";
		}
		
		private static GameObject voidLunarSystem;
		private static GameObject pickup;
		private PickupDisplay display;
		private bool set;

		private void Awake()
		{
			display = GetComponent<PickupDisplay>();
		}

		public void Update()
		{
			var pickupDef = PickupCatalog.GetPickupDef(display.pickupIndex);
			if (pickupDef == null || set) return;
			set = true;
			var itemIndex = pickupDef.itemIndex;
			var tier = ItemCatalog.GetItemDef(itemIndex)?.tier ?? ItemTier.NoTier;
			if (tier == BubbetsItemsPlugin.VoidLunarTier.tier)
				Instantiate(voidLunarSystem, transform.parent).SetActive(true);
		}
	}
}