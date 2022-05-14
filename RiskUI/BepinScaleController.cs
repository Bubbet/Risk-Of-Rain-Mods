using System;
using BepInEx.Configuration;
using UnityEngine;

namespace MaterialHud
{
	public class BepinScaleController : MonoBehaviour
	{
		public string Category;
		public string Key;
		public string Description;
		private ConfigEntry<float> _configValue;

		private void Awake()
		{
			_configValue = ConfigHelper.Bind(Category, Key, 100f, Description, riskOfOptionsExtra: 300f);
			_configValue.SettingChanged += SettingChanged;
		}

		private void OnEnable()
		{
			SettingChanged(null, null);
		}

		private void OnDestroy()
		{
			_configValue.SettingChanged -= SettingChanged;
		}

		private void SettingChanged(object sender, EventArgs e)
		{
			var val = _configValue.Value / 100f;
			var transformLocalScale = transform.localScale;
			transformLocalScale.y = val;
			transformLocalScale.x = val;
			transform.localScale = transformLocalScale;
		}
	}
}