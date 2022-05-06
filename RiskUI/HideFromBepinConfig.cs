using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace MaterialHud
{
	public class HideFromBepinConfig : MonoBehaviour
	{
		public GameObject target;
		public string configName;
		public string configDesc;
		public string configCategory;
		public bool defaultValue;
		private ConfigEntry<bool> _configEntry;

		public void Awake()
		{
			_configEntry = ConfigHelper.Bind(configCategory, configName, defaultValue, configDesc);
			_configEntry.SettingChanged += SettingChanged;
			SettingChanged();
		}

		public void OnDestroy()
		{
			_configEntry.SettingChanged -= SettingChanged;
		}

		private void SettingChanged(object sender, EventArgs e)
		{
			SettingChanged();
		}
		private void SettingChanged()
		{
			target.SetActive(_configEntry.Value);
		}
	}
}