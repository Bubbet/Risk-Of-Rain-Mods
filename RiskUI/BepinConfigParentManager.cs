using System;
using UnityEngine;
using ZioConfigFile;

namespace MaterialHud
{
	public class BepinConfigParentManager : MonoBehaviour, IConfigHandler
	{
		public Transform[] choices;
		public string category;
		public string key;
		public string description;
		private ZioConfigEntry<int> _configEntry;

		public void Awake()
		{
			Startup();
			_configEntry.SettingChanged += ConfigUpdated;
		}

		private void OnEnable()
		{
			ConfigUpdated(null, null, false);
		}

		public void OnDestroy()
		{
			_configEntry.SettingChanged -= ConfigUpdated;
		}

		private void ConfigUpdated(ZioConfigEntryBase zioConfigEntryBase, object o, bool arg3)
		{
			var slot = Math.Min(choices.Length - 1, _configEntry.Value);
			transform.SetParent(choices[slot], false);
		}

		public void Startup()
		{
			_configEntry = ConfigHelper.Bind(category, key, 0, description, null, choices.Length - 1);
		}
	}
}