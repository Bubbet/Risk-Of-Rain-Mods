#nullable enable
using TMPro;
using UnityEngine;
using ZioConfigFile;

namespace MaterialHud
{
	public class TextSizeBepin : MonoBehaviour, IConfigHandler
	{
		public string Key;
		public string Description;
		public TextMeshProUGUI Target;
		private ZioConfigEntry<float> _configEntry;
		public void Awake()
		{
			Startup();
			
			_configEntry.SettingChanged += SettingChanged;
		}

		private void OnEnable()
		{
			SettingChanged(null, null, false);
		}

		private void OnDestroy()
		{
			_configEntry.SettingChanged -= SettingChanged;
		}

		private void SettingChanged(ZioConfigEntryBase zioConfigEntryBase, object o, bool arg3)
		{
			Target.fontSize = _configEntry.Value;
		}

		public void Startup()
		{
			_configEntry = ConfigHelper.Bind("Rescaling", Key, Target.fontSize, Description, riskOfOptionsExtra: 100f);
		}
	}
}