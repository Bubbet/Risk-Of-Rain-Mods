using System;
using System.Collections;
using System.Reflection;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MaterialHud
{
	[ExecuteAlways]
	public class FieldLoader : MonoBehaviour
	{
		public string addressablePath;
		public string targetFieldName;
		public Component target;

		private static readonly MethodInfo LoadAssetAsyncInfo = typeof(Addressables).GetMethod(nameof(Addressables.LoadAssetAsync), new[] { typeof(string) });

		private static readonly BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic;

		void LoadAsset(bool dontSave = false)
		{
			var typ = target.GetType();
			var field = typ.GetField(targetFieldName, flags);
			PropertyInfo property = null;
			if (field == null)
			{
				property = typ.GetProperty(targetFieldName, flags);
				if (property == null) return;
			}
			var meth = LoadAssetAsyncInfo.MakeGenericMethod(field?.FieldType ?? property.PropertyType);
			var awaiter = meth.Invoke(null, new object[] { addressablePath });
			var wait = awaiter.GetType().GetMethod("WaitForCompletion", BindingFlags.Instance | BindingFlags.Public);
			var asset = wait.Invoke(awaiter, null);
			var assetObject = (UnityEngine.Object)asset;
			if (assetObject != null)
			{
				if (dontSave)
				{
					assetObject.hideFlags |= HideFlags.DontSave;
				}
				field?.SetValue(target, asset);
				property?.SetValue(target, asset);
			}
		}
		IEnumerator WaitAndLoadAsset()
		{
			yield return new WaitUntil(() => Addressables.InternalIdTransformFunc != null);
			LoadAsset(true);
		}

		void Start()
		{
			LoadAsset();
		}

		void OnValidate()
		{
			if(gameObject.activeInHierarchy) StartCoroutine(WaitAndLoadAsset());
		}
	}
	
	[ExecuteAlways]
	public class ParticleSystemMaterialLoader : MonoBehaviour
	{
		public string addressablePath;
		public ParticleSystem target;
		public Color Tint;

		void LoadAsset(bool dontSave = false)
		{
			var renderer = target.GetComponent<ParticleSystemRenderer>();
			renderer.material = Addressables.LoadAssetAsync<Material>(addressablePath).WaitForCompletion();
			if (Tint != default)
				renderer.material.SetColor("_TintColor", Tint);
		}
		IEnumerator WaitAndLoadAsset()
		{
			yield return new WaitUntil(() => Addressables.InternalIdTransformFunc != null);
			LoadAsset(true);
		}

		void Start()
		{
			LoadAsset();
		}

		void OnValidate()
		{
			if(gameObject.activeInHierarchy) StartCoroutine(WaitAndLoadAsset());
		}
	}

	[ExecuteAlways]
	public class PrefabChildLoader : MonoBehaviour
	{
		public string prefabAddress;
		public int childIndex;
		
		private Boolean loading = false;
		private GameObject instance;
		private int loadedIndex = -1;

		public UnityEvent finished = new();

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
			if (loadedIndex != childIndex)
			{
				loadedIndex = -1;
				if (instance != null) DestroyImmediate(instance);
			}
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
					if (loadedIndex == childIndex) break;
					if (instance != null) DestroyImmediate(instance);
					var prefab = obj.Result;
					var parent = Instantiate(prefab);
					if (parent.transform.childCount > 0)
						instance = parent.transform.GetChild(Math.Min(childIndex, parent.transform.childCount - 1)).gameObject;
					else
						instance = parent;

					var transformChild = instance.transform;
					SetRecursiveFlags(transformChild);
					transformChild.eulerAngles = Vector3.zero;
					transformChild.position = Vector3.zero;
					transformChild.localScale = Vector3.one;
					transformChild.SetParent(gameObject.transform, false);
					if (parent.transform.childCount > 0)
						DestroyImmediate(parent);
					loadedIndex = childIndex;
					loading = false;
					finished.Invoke();
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
	
	[ExecuteAlways]
	public class ShaderLoader : MonoBehaviour
	{
		public string addressablePath;
		public Renderer target;
		

		[ContextMenu("Fill In Editor")]
		[ExecuteAlways]
		public void Start()
		{
			var shader = Addressables.LoadAssetAsync<Shader>(addressablePath).WaitForCompletion();
			target.material.shader = shader;
			target.sharedMaterial.shader = shader;
			for (var i = 0; i < target.sharedMaterial.shader.GetPropertyCount(); i++)
			{
				Debug.Log(target.sharedMaterial.shader.GetPropertyFlags(i));
			}
		}
	}
}