using System;
using BepInEx.Configuration;

namespace ZioConfigFile
{
	public class ZioConfigEntry<T> : ZioConfigEntryBase
	{
		public ZioConfigEntry(ConfigDefinition configDefinition, T defaultValue, ConfigDescription configDescription) : base(configDefinition, typeof(T), defaultValue, configDescription) {}

		private T _typedValue;

		public T Value
		{
			get => _typedValue;
			set
			{
				value = ClampValue(value);
				if (value.Equals(_typedValue)) return;
				var oldVal = _typedValue;
				_typedValue = value;
				OnSettingChanged(this, oldVal);
			}
		}

		public override object BoxedValue { get => Value; set => Value = (T) value; }
		
		public static implicit operator ConfigEntry<T>(ZioConfigEntry<T> zioEntry)
		{
			if (zioEntry.configEntryFallback is not null) return (ConfigEntry<T>) zioEntry.configEntryFallback;
			fallbackConfigFile ??= new ConfigFile("", false) {SaveOnConfigSet = false}; 
			
			var fallback = new ConfigEntry<T>(fallbackConfigFile, zioEntry.Definition, (T) zioEntry.DefaultValue, zioEntry.Description);
			fallback.SettingChanged += (_, _) =>
			{
				if (zioEntry.duckChanged) return;
				zioEntry.duckFallbackChanged = true;
				zioEntry.Value = fallback.Value;
				zioEntry.duckFallbackChanged = false;
			};
			zioEntry.SettingChanged += (_, _, _) =>
			{
				if (zioEntry.duckFallbackChanged) return;
				zioEntry.duckChanged = true;
				fallback.Value = zioEntry.Value;
				zioEntry.duckChanged = false;
			};
			zioEntry.configEntryFallback = fallback;
			
			return (ConfigEntry<T>) zioEntry.configEntryFallback;
		}
	}
}