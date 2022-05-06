using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MaterialHud
{
	public class NotificationAreaLoader : MonoBehaviour
	{
		public NotificationUIController controller;
		
		private static GameObject genericNotificationPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/NotificationPanel2.prefab").WaitForCompletion();
		private static GameObject genericTransformationNotificationPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/GenericTransformationNotificationPanel.prefab").WaitForCompletion();
		private static GameObject contagiousVoidTransformationNotificationPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/UI/VoidTransformationNotificationPanel.prefab").WaitForCompletion();
		private static GameObject cloverVoidTransformationNotificationPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/CloverVoid/CloverVoidTransformationNotificationPanel.prefab").WaitForCompletion();
		private static GameObject regeneratingScrapRegenTransformationNotificationPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/RegeneratingScrap/RegeneratingScrapRegenTransformationNotificationPanel.prefab").WaitForCompletion();
		public void Start()
		{
			controller.genericNotificationPrefab = genericNotificationPrefab;
			controller.genericTransformationNotificationPrefab = genericTransformationNotificationPrefab;
			controller.contagiousVoidTransformationNotificationPrefab = contagiousVoidTransformationNotificationPrefab;
			controller.cloverVoidTransformationNotificationPrefab = cloverVoidTransformationNotificationPrefab;
			controller.regeneratingScrapRegenTransformationNotificationPrefab = regeneratingScrapRegenTransformationNotificationPrefab;
		}
	}
}