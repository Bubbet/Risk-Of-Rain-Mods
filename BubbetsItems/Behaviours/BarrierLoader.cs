using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BubbetsItems.Behaviours
{
	public class BarrierLoader : MonoBehaviour
	{
		private void Awake()
		{
			var obj = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/BarrierEffect.prefab").WaitForCompletion();
			var objI = Instantiate(obj, transform);
			objI.SetActive(false);
			Destroy(objI.GetComponent<TemporaryVisualEffect>());
			Destroy(objI.GetComponent<DestroyOnTimer>());
			objI.SetActive(true);
		}
	}
}