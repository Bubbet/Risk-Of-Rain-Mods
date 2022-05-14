using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MaterialHud
{
	public class FieldLoader : MonoBehaviour
	{
		public string addressablePath;
		public string targetFieldName;
		public MonoBehaviour target;

		private static readonly MethodInfo LoadAsset = typeof(Addressables).GetMethod(nameof(Addressables.LoadAssetAsync),new[]{typeof(string)});//BindingFlags.Static | BindingFlags.Public);

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
}