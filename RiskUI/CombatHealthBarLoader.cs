using System;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MaterialHud
{
	public class CombatHealthBarLoader : MonoBehaviour
	{
		public CombatHealthBarViewer viewer;
		private static GameObject HealthBar => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/CombatHealthbar.prefab").WaitForCompletion();

		public void Start()
		{
			viewer.healthBarPrefab = HealthBar;
		}
	}
}