using UnityEngine;
using ZioConfigFile;

namespace MaterialHud
{
	public class HideFromBepinConfig : MonoBehaviour, IConfigHandler
	{
		public GameObject target;
		public string configName;
		public string configDesc;
		public string configCategory;
		public bool defaultValue;
		private ZioConfigEntry<bool> _configEntry;

		public void Awake()
		{
			Startup();
			_configEntry.SettingChanged += SettingChanged;
			SettingChanged();
		}

		public void OnDestroy()
		{
			_configEntry.SettingChanged -= SettingChanged;
		}

		private void SettingChanged(ZioConfigEntryBase config, object oldValue, bool ignoreSave)
		{
			SettingChanged();
		}
		private void SettingChanged()
		{
			target.SetActive(_configEntry.Value);
		}

		public void Startup()
		{
			_configEntry = ConfigHelper.Bind(configCategory, configName, defaultValue, configDesc);
		}
	}
}