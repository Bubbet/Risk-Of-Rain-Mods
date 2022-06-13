using BepInEx.Configuration;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using ZioConfigFile;

namespace ZioRiskOfOptions
{
	public class ZioKeyBindOption : KeyBindOption
	{
		private readonly ZioConfigEntry<KeyboardShortcut> _configEntry;
		public ZioKeyBindOption(ZioConfigEntry<KeyboardShortcut> configEntry) : this(configEntry, new KeyBindConfig()) { }
		public ZioKeyBindOption(ZioConfigEntry<KeyboardShortcut> configEntry, bool restartRequired) : this(configEntry, new KeyBindConfig(){restartRequired = restartRequired}) { }
		public ZioKeyBindOption(ZioConfigEntry<KeyboardShortcut> configEntry, KeyBindConfig config) : base(config, configEntry.Value)
		{
			_configEntry = configEntry;
		}
		public override KeyboardShortcut Value
		{
			get => _configEntry.Value;
			set => _configEntry.Value = value;
		}
		protected override void SetProperties() => ZioBaseOption.SetProperties(this, _configEntry);
	}
}