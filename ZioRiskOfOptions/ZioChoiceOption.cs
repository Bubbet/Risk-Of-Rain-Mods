using System;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using ZioConfigFile;

namespace ZioRiskOfOptions
{
	public class ZioChoiceOption : ChoiceOption
	{
		private readonly ZioConfigEntryBase _configEntry;
		public ZioChoiceOption(ZioConfigEntryBase configEntry) : this(configEntry, new ChoiceConfig()) {}
		public ZioChoiceOption(ZioConfigEntryBase configEntry, bool restartRequired) : this(configEntry, new ChoiceConfig {restartRequired = restartRequired}) {}

		public ZioChoiceOption(ZioConfigEntryBase configEntry, ChoiceConfig config) : base(config, configEntry.BoxedValue)
		{
			_configEntry = configEntry;
		}
		
		public override object Value
		{
			get => _configEntry.BoxedValue;
			set => _configEntry.BoxedValue = Enum.Parse(_configEntry.SettingType, value.ToString());
		}
		
		protected override void SetProperties() => ZioBaseOption.SetProperties(this, _configEntry);
	}
}