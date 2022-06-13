using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using ZioConfigFile;

namespace ZioRiskOfOptions
{
	public class ZioIntSliderOption : IntSliderOption
	{
		protected override void SetProperties() => ZioBaseOption.SetProperties(this, _configEntry);
		private readonly ZioConfigEntry<int> _configEntry;
		public ZioIntSliderOption(ZioConfigEntry<int> configEntry) : this(configEntry, new IntSliderConfig()) { }
		public ZioIntSliderOption(ZioConfigEntry<int> configEntry, bool restartRequired) : this(configEntry, new IntSliderConfig(){restartRequired = restartRequired}) { }
		public ZioIntSliderOption(ZioConfigEntry<int> configEntry, IntSliderConfig config) : base(config, configEntry.Value)
		{
			_configEntry = configEntry;
		}

		public override int Value
		{
			get => _configEntry.Value;
			set => _configEntry.Value = value;
		}
	}
}