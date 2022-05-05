using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MaterialHud
{
	public class FieldLoader : MonoBehaviour
	{
		public string addressablePath;
		public string targetFieldName;
		public MonoBehaviour target;

		private static readonly MethodInfo LoadAsset = typeof(Addressables).GetMethod(nameof(Addressables.LoadAssetAsync),new[]{typeof(string)});//BindingFlags.Static | BindingFlags.Public);

		[ContextMenu("Fill In Editor")]
		public void Start()
		{
			var typ = target.GetType();
			var field = typ.GetField(targetFieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null) return;
			var meth = LoadAsset.MakeGenericMethod(field.FieldType);
			var awaiter = meth.Invoke(null, new object[] {addressablePath});
			var wait = awaiter.GetType().GetMethod("WaitForCompletion", BindingFlags.Instance | BindingFlags.Public);
			var asset = wait.Invoke(awaiter, null);
			field.SetValue(target, asset);
		}
	}
	
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