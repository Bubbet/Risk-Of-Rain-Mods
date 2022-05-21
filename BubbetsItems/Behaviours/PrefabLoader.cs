using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ZedMod
{
	[ExecuteAlways]
	public class PrefabLoader : MonoBehaviour
	{
		public string prefabAddress;
		private string loadedPrefab;
		private Boolean loading = false;
		GameObject instance;
		void Start()
		{
			LoadPrefab();
		}

		void OnValidate()
		{
			LoadPrefab();
		}

		void LoadPrefab()
		{
			if (!string.IsNullOrEmpty(prefabAddress) && !loading)
			{
				loading = true;
				Addressables.LoadAssetAsync<GameObject>(prefabAddress).Completed += PrefabLoaded;
			}
		}

		private void PrefabLoaded(AsyncOperationHandle<GameObject> obj)
		{
			switch (obj.Status)
			{
				case AsyncOperationStatus.Succeeded:
					if (loadedPrefab == prefabAddress) break;
					if (instance != null) DestroyImmediate(instance);
					var prefab = obj.Result;
					instance = Instantiate(prefab);
					SetRecursiveFlags(instance.transform);
					instance.transform.SetParent(this.gameObject.transform, false);
					loadedPrefab = prefabAddress;
					loading = false;
					break;
				case AsyncOperationStatus.Failed:
					if (instance != null) DestroyImmediate(instance);
					Debug.LogError("Prefab load failed.");
					loading = false;
					break;
				default:
					// case AsyncOperationStatus.None:
					break;
			}
		}

		static void SetRecursiveFlags(Transform transform)
		{
			transform.gameObject.hideFlags |= HideFlags.DontSave;
			foreach(Transform child in transform)
			{
				SetRecursiveFlags(child);
			}
		}
	}
}