using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using ZioConfigFile;

namespace ZioRiskOfOptions
{
	public class ZioStepSliderOption : StepSliderOption
	{
		private readonly ZioConfigEntry<float> _configEntry;
		public ZioStepSliderOption(ZioConfigEntry<float> configEntry) : this(configEntry, new StepSliderConfig()) { }

		public ZioStepSliderOption(ZioConfigEntry<float> configEntry, bool restartRequired) : this(configEntry, new StepSliderConfig { restartRequired = restartRequired }) { }

		public ZioStepSliderOption(ZioConfigEntry<float> configEntry, StepSliderConfig config) : base(config, configEntry.Value)
		{
			_configEntry = configEntry;
		}

		public override float Value { get => _configEntry.Value; set => _configEntry.Value = value; }
		protected override void SetProperties() => ZioBaseOption.SetProperties(this, _configEntry);
	}
}