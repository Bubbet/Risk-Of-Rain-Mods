using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using UnityEngine;
using ZioConfigFile;

namespace ZioRiskOfOptions
{
	public class ZioColorOption : ColorOption
	{
		private readonly ZioConfigEntry<Color> _configEntry;
		protected override void SetProperties() => ZioBaseOption.SetProperties(this, _configEntry);

		public ZioColorOption(ZioConfigEntry<Color> configEntry) : this(configEntry, new ColorOptionConfig()) { }
		public ZioColorOption(ZioConfigEntry<Color> configEntry, bool restartRequired) : this(configEntry, new ColorOptionConfig {restartRequired = restartRequired}) { }
		public ZioColorOption(ZioConfigEntry<Color> configEntry, ColorOptionConfig config) : base(config, configEntry.Value)
		{
			_configEntry = configEntry;
		}

		public override Color Value
		{
			get => _configEntry.Value;
			set => _configEntry.Value = value;
		}
	}
}