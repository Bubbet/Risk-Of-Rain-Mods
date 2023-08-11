using UnityEngine;
using ZioConfigFile;

namespace MaterialHud
{
	public class BepinScaleController : MonoBehaviour
	{
		public string Category;
		public string Key;
		public string Description;
		private ZioConfigEntry<float> _configValue;

		private void Awake()
		{
			_configValue = ConfigHelper.Bind(Category, Key, 100f, Description, riskOfOptionsExtra: 300f);
			_configValue.SettingChanged += SettingChanged;
		}

		private void OnEnable()
		{
			SettingChanged(null, null, false);
		}

		private void OnDestroy()
		{
			_configValue.SettingChanged -= SettingChanged;
		}

		private void SettingChanged(ZioConfigEntryBase zioConfigEntryBase, object o, bool arg3)
		{
			var val = _configValue.Value / 100f;
			var transformLocalScale = transform.localScale;
			transformLocalScale.y = val;
			transformLocalScale.x = val;
			transform.localScale = transformLocalScale;
		}
	}
}