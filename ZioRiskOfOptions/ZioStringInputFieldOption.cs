using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using ZioConfigFile;

namespace ZioRiskOfOptions
{
	public class ZioStringInputFieldOption : StringInputFieldOption
	{
		private readonly ZioConfigEntry<string> _configEntry;
		public ZioStringInputFieldOption(ZioConfigEntry<string> configEntry) : this(configEntry, new InputFieldConfig()) { }
		public ZioStringInputFieldOption(ZioConfigEntry<string> configEntry, bool restartRequired) : this(configEntry, new InputFieldConfig { restartRequired =  restartRequired }) { }
		public ZioStringInputFieldOption(ZioConfigEntry<string> configEntry, InputFieldConfig config) : base(config, configEntry.Value)
		{
			_configEntry = configEntry;
		}
		public override string Value { get => _configEntry.Value; set => _configEntry.Value = value; }
		protected override void SetProperties() => ZioBaseOption.SetProperties(this, _configEntry);
	}
}