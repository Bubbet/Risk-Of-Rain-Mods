using System;
using BepInEx.Configuration;
using UnityEngine;

namespace MaterialHud
{
	public class BepinConfigParentManager : MonoBehaviour
	{
		public Transform[] choices;
		public string category;
		public string key;
		public string description;
		private ConfigEntry<int> _configEntry;

		public void Awake()
		{
			_configEntry = ConfigHelper.Bind(category, key, 0, description, null, choices.Length - 1);
			_configEntry.SettingChanged += ConfigUpdated;
		}

		private void OnEnable()
		{
			ConfigUpdated(null, null);
		}

		public void OnDestroy()
		{
			_configEntry.SettingChanged -= ConfigUpdated;
		}

		private void ConfigUpdated(object sender, EventArgs e)
		{
			var slot = Math.Min(choices.Length - 1, _configEntry.Value);
			transform.SetParent(choices[slot], false);
		}
	}
}