using System;
using System.Collections.Generic;
using System.Drawing;
using BepInEx.Configuration;
using JetBrains.Annotations;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using Color = UnityEngine.Color;

namespace MaterialHud
{
	public static class ConfigHelper
	{
		public static ConfigFile whichFile => RiskUIPlugin.ConfigFile;
		public static Dictionary<string, ConfigEntryBase> Entries = new();

		public static ConfigEntry<T> Bind<T>(string category, string key, T defaultValue, string desc, [CanBeNull] Action<ConfigEntry<T>> firstSetupCallback = null, object riskOfOptionsExtra = null)
		{
			var entryKey = category + key;
			if (!Entries.ContainsKey(entryKey))
			{
				var _configEntry = RiskUIPlugin.ConfigFile.Bind(category, key, defaultValue, desc);
				if(RiskUIPlugin.RiskOfOptionsEnabled)
					FillRiskOfOptions(_configEntry, riskOfOptionsExtra);
				Entries.Add(entryKey, _configEntry);
				firstSetupCallback?.Invoke(_configEntry);
			}
			return (ConfigEntry<T>) Entries[entryKey];
		}

		private static void FillRiskOfOptions<T>(ConfigEntry<T> configEntry, object riskOfOptionsExtra = null)
		{
			switch (configEntry)
			{
				case ConfigEntry<string> entry:
					ModSettingsManager.AddOption(new StringInputFieldOption(entry));
					break;
				case ConfigEntry<bool> entry:
					ModSettingsManager.AddOption(new CheckBoxOption(entry));
					break;
				case ConfigEntry<int> entry:
					ModSettingsManager.AddOption(new IntSliderOption(entry, new IntSliderConfig
					{
						max = (int) riskOfOptionsExtra
					}));
					break;
				case ConfigEntry<Color> entry:
					ModSettingsManager.AddOption(new ColorOption(entry));
					break;
			}
		}
	}
}