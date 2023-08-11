using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using Zio;
using ZioConfigFile;
using ZioRiskOfOptions;
using Color = UnityEngine.Color;

namespace MaterialHud
{
	public static class ConfigHelper
	{
		public static ZioConfigFile.ZioConfigFile whichFile => RiskUIPlugin.ConfigFile;
		public static List<ZioConfigFile.ZioConfigFile> configFiles => RiskUIPlugin.ConfigFiles;
		public static Dictionary<string, ZioConfigEntryBase> Entries = new();
		private static ZioConfigEntry<int> _selectedProfile;
		private static IntSliderConfig option;

		public static int SelectedProfile
		{
			get
			{
				if (_selectedProfile is not null) return _selectedProfile.Value;
				_selectedProfile = whichFile.Bind("Profiles", "Selected Profile", 0, "Currently selected profile.");
				_selectedProfile.SettingChanged += (_, profile, _) => SelectedProfile = (int)profile;
				if (RiskUIPlugin.RiskOfOptionsEnabled)
					MakeSelectedProfileOptions();

				return _selectedProfile.Value;
			}
			set
			{
				_selectedProfile.Value = value;
				foreach (var entryPair in whichFile)
				{
					if (entryPair.Key == _selectedProfile.Definition) continue;
					ApplyConfigProfile(entryPair.Value);
				}
			}
		}

		private static void MakeSelectedProfileOptions()
		{
			option = new IntSliderConfig
			{
				max = configFiles.Count - 1
			};
			ModSettingsManager.AddOption(new ZioIntSliderOption(_selectedProfile, option));
			ModSettingsManager.AddOption(new GenericButtonOption("Add Profile", "Profiles", () =>
			{
				configFiles.Add(new ZioConfigFile.ZioConfigFile(whichFile.FileSystem,
					$"{whichFile.FilePath.GetDirectory()}/RiskUI/{new Guid()}", true,
					whichFile.OwnerMetadata));
				option.max = configFiles.Count - 1;
			}));
			ModSettingsManager.AddOption(new GenericButtonOption("Delete Profile", "Profiles", () =>
			{
				whichFile.FileSystem.DeleteFile(configFiles[SelectedProfile].FilePath);
				configFiles.RemoveAt(SelectedProfile);
				option.max = configFiles.Count - 1;
			}));
		}

		public static ZioConfigEntry<T> Bind<T>(string category, string key, T defaultValue, string desc, [CanBeNull] Action<ZioConfigEntry<T>> firstSetupCallback = null, object riskOfOptionsExtra = null)
		{
			var entryKey = category + key;
			if (!Entries.ContainsKey(entryKey))
			{
				var _configEntry = RiskUIPlugin.ConfigFile.Bind(category, key, defaultValue, desc);
				_configEntry.SettingChanged += UpdateSelectedProfile;
				foreach (var configFile in RiskUIPlugin.ConfigFiles) configFile.Bind(category, key, defaultValue, desc);
				ApplyConfigProfile(_configEntry);
				if(RiskUIPlugin.RiskOfOptionsEnabled)
					FillRiskOfOptions(_configEntry, riskOfOptionsExtra);
				Entries.Add(entryKey, _configEntry);
				firstSetupCallback?.Invoke(_configEntry);
			}
			return (ZioConfigEntry<T>) Entries[entryKey];
		}

		private static void UpdateSelectedProfile(ZioConfigEntryBase arg1, object arg2, bool arg3)
		{
			configFiles[SelectedProfile][arg1.Definition].BoxedValue = arg1.BoxedValue;
		}

		private static void ApplyConfigProfile(ZioConfigEntryBase configEntry)
		{
			if (RiskUIPlugin.NeverBeforeInitialized)
				UpdateSelectedProfile(configEntry, null, false);
			else
				configEntry.BoxedValue = configFiles[SelectedProfile][configEntry.Definition].BoxedValue;
		}

		private static void FillRiskOfOptions<T>(ZioConfigEntry<T> configEntry, object riskOfOptionsExtra = null)
		{
			switch (configEntry)
			{
				case ZioConfigEntry<string> entry:
					ModSettingsManager.AddOption(new ZioStringInputFieldOption(entry));
					break;
				case ZioConfigEntry<bool> entry:
					ModSettingsManager.AddOption(new ZioCheckBoxOption(entry));
					break;
				case ZioConfigEntry<int> entry:
					ModSettingsManager.AddOption(new ZioIntSliderOption(entry, new IntSliderConfig
					{
						max = (int) riskOfOptionsExtra
					}));
					break;
				case ZioConfigEntry<float> entry:
					ModSettingsManager.AddOption(new ZioSliderOption(entry, new SliderConfig
					{
						max = (float) riskOfOptionsExtra
					}));
					break;
				case ZioConfigEntry<Color> entry:
					ModSettingsManager.AddOption(new ZioColorOption(entry));
					break;
			}
		}
	}
}