using System;
using System.IO;
using System.Linq;
using BepInEx.Configuration;

namespace ZioConfigFile
{
	public abstract class ZioConfigEntryBase
	{
		public bool DontSaveOnChange;

		protected ZioConfigEntryBase(ConfigDefinition configDefinition, Type settingType, object defaultValue, ConfigDescription configDescription)
		{
			Definition = configDefinition ?? throw new ArgumentNullException(nameof(configDefinition));
			SettingType = settingType ?? throw new ArgumentNullException(nameof(settingType));
			Description = configDescription ?? ConfigDescription.Empty;
			if (Description.AcceptableValues != null &&
			    !SettingType.IsAssignableFrom(Description.AcceptableValues.ValueType)) throw new AggregateException("configDescription.AcceptableValues is for a different type than the type of this setting");
			DefaultValue = defaultValue;
			BoxedValue = defaultValue;
		}

		public ConfigDefinition Definition { get; }
		public ConfigDescription Description { get; }
		public Type SettingType { get; }
		public object DefaultValue { get; }
		public abstract object BoxedValue { get; set; }
		public string GetSerializedValue() => TomlTypeConverter.ConvertToString(BoxedValue, SettingType);
		public void SetSerializedValue(string value)
		{
			try
			{
				BoxedValue = TomlTypeConverter.ConvertToValue(value, SettingType);
			}
			catch (Exception ex)
			{
				ZioConfigFile.Logger.LogWarning($"Config value of setting \"{Definition}\" could not be parsed and will be ignored. Reason: {ex.Message}; Value: {value}");
			}
		}
		public T ClampValue<T>(T value) => Description.AcceptableValues != null ? (T) Description.AcceptableValues.Clamp(value) : value;

		public event Action<ZioConfigEntryBase, object, bool> SettingChanged;
		public void OnSettingChanged(ZioConfigEntryBase config, object oldValue) => SettingChanged?.Invoke(config, oldValue, DontSaveOnChange);
		public void WriteDescription(StreamWriter writer)
		{
			if (!string.IsNullOrEmpty(Description.Description))
				writer.WriteLine("## " + Description.Description.Replace("\n", "\n## "));
			writer.WriteLine("# Setting type: " + SettingType.Name);
			writer.WriteLine("# Default value: " + TomlTypeConverter.ConvertToString(DefaultValue, SettingType));
			if (Description.AcceptableValues != null)
			{
				writer.WriteLine(Description.AcceptableValues.ToDescriptionString());
			}
			else
			{
				if (!SettingType.IsEnum)
					return;
				writer.WriteLine("# Acceptable values: " + string.Join(", ", Enum.GetNames(SettingType)));
				if (!SettingType.GetCustomAttributes(typeof (FlagsAttribute), true).Any())
					return;
				writer.WriteLine("# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)");
			}
		}
	}
}