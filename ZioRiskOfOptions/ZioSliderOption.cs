using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using ZioConfigFile;

namespace ZioRiskOfOptions
{
	public class ZioSliderOption : SliderOption
	{
		private readonly ZioConfigEntry<float> _configEntry;
		public ZioSliderOption(ZioConfigEntry<float> configEntry) : this(configEntry, new SliderConfig()) { }
        
		public ZioSliderOption(ZioConfigEntry<float> configEntry, bool restartRequired) : this(configEntry, new SliderConfig { restartRequired = restartRequired }) { }

		public ZioSliderOption(ZioConfigEntry<float> configEntry, SliderConfig config) : base(config, configEntry.Value)
		{
			_configEntry = configEntry;
		}

		public override float Value { get => _configEntry.Value; set => _configEntry.Value = value; }
		protected override void SetProperties() => ZioBaseOption.SetProperties(this, _configEntry);
	}
}