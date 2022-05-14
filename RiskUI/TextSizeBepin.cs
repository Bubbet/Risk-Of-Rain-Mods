#nullable enable
using System;
using System.Reflection;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;

namespace MaterialHud
{
	public class TextSizeBepin : MonoBehaviour
	{
		public string Key;
		public string Description;
		public TextMeshProUGUI Target;
		private ConfigEntry<float> _configEntry;
		public void Awake()
		{
			_configEntry = ConfigHelper.Bind("Rescaling", Key, Target.fontSize, Description, riskOfOptionsExtra: 100f);
			
			_configEntry.SettingChanged += SettingChanged;
		}

		private void OnEnable()
		{
			SettingChanged(null, null);
		}

		private void OnDestroy()
		{
			_configEntry.SettingChanged -= SettingChanged;
		}

		private void SettingChanged(object sender, EventArgs e)
		{
			Target.fontSize = _configEntry.Value;
		}
	}
}