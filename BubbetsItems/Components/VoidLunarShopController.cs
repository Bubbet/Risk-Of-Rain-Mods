using BepInEx.Bootstrap;
using HarmonyLib;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace BubbetsItems
{
	public static class VoidLunarShopController
	{
		public static GameObject ShopInstance;
		private static GameObject? _shopPrefab;
		private static ExplicitPickupDropTable voidCoinTable;
		private static InteractableSpawnCard voidBarrelSpawncard;
		public static GameObject ShopPrefab => _shopPrefab ??= BubbetsItemsPlugin.AssetBundle.LoadAsset<GameObject>("LunarVoidShop");

		public static void Init()
		{
			Stage.onStageStartGlobal += SceneLoaded;
			RoR2Application.onLoad += MakeTokens;
		}

		private static void MakeTokens()
		{
			Language.english.SetStringByToken("BUB_VOIDLUNARSHOP_NAME", "Void Bud");
			Language.english.SetStringByToken("BUB_VOIDLUNARSHOP_CONTEXT", "Open Void Bud");
			Language.english.SetStringByToken("BUB_VOIDLUNARSHOP_REROLL_NAME", "Slab");
			Language.english.SetStringByToken("BUB_VOIDLUNARSHOP_REROLL_CONTEXT", "Refresh Shop");
			
			if(!Chainloader.PluginInfos.ContainsKey("com.Anreol.ReleasedFromTheVoid")) EnableVoidCoins();
		}

		public static void SceneLoaded(Stage stage)
		{
			if (stage.sceneDef.nameToken != "MAP_BAZAAR_TITLE") return;
			if (!Run.instance.IsExpansionEnabled(BubbetsItemsPlugin.BubSotvExpansion)) return;
			ShopInstance = GameObject.Instantiate(ShopPrefab, new Vector3(284.3365f, -445.1391f, -139.8904f), Quaternion.Euler(0, 330, 0));
			NetworkServer.Spawn(ShopInstance);
		}
		
		public static void EnableVoidCoins()
		{
			voidCoinTable = Addressables.LoadAssetAsync<ExplicitPickupDropTable>("RoR2/DLC1/Common/DropTables/dtVoidCoin.asset").WaitForCompletion();
			voidBarrelSpawncard = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC1/VoidCoinBarrel/iscVoidCoinBarrel.asset").WaitForCompletion();
			voidBarrelSpawncard.prefab.GetComponent<ModelLocator>().gameObject.AddComponent<ChestBehavior>().dropTable = voidCoinTable;
			voidBarrelSpawncard.directorCreditCost = 7;
		}
	}
}