using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MaterialHud
{
	public class AddressablePrefabLoader : MonoBehaviour
	{
		public string Address;
		public bool defaultActiveState = true;

		private void OnValidate()
		{
			Awake();
		}

		private void Awake()
		{
			var obj = Addressables.LoadAssetAsync<GameObject>(Address).WaitForCompletion();
			Instantiate(obj, transform).SetActive(defaultActiveState);
		}
	}
}