using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using ZioConfigFile;

namespace ZioRiskOfOptions
{
	public class ZioCheckBoxOption : CheckBoxOption
	{
		private readonly ZioConfigEntry<bool> _configEntry;
		public ZioCheckBoxOption(ZioConfigEntry<bool> configEntry) : this(configEntry, new CheckBoxConfig()) { }
		public ZioCheckBoxOption(ZioConfigEntry<bool> configEntry, bool restartRequired) : this(configEntry, new CheckBoxConfig { restartRequired = restartRequired }) { }
		public ZioCheckBoxOption(ZioConfigEntry<bool> configEntry, CheckBoxConfig config) : base(config, configEntry.Value)
		{
			_configEntry = configEntry;
		}

		public override bool Value
		{
			get => _configEntry.Value;
			set => _configEntry.Value = value;
		}

		protected override void SetProperties() => ZioBaseOption.SetProperties(this, _configEntry);
	}
	/*
	public class ZioCheckBoxOption : ZioBaseOption, ITypedValueHolder<bool>
	{
		private readonly bool _originalValue;
		private readonly ZioConfigEntry<bool> _configEntry;
		protected readonly CheckBoxConfig config;

		public ZioCheckBoxOption(ZioConfigEntry<bool> configEntry) : this(configEntry, new CheckBoxConfig()) {}

		public ZioCheckBoxOption(ZioConfigEntry<bool> configEntry, bool restartRequired) : this(configEntry, new CheckBoxConfig {restartRequired = restartRequired}) {}

		public ZioCheckBoxOption(ZioConfigEntry<bool> configEntry, CheckBoxConfig config)
		{
			_originalValue = configEntry.Value;
			_configEntry = configEntry;
			this.config = config;
		}
		
		public override GameObject CreateOptionGameObject(GameObject prefab, Transform parent)
		{
			var gameObject = Object.Instantiate(prefab, parent);
			var component = gameObject.GetComponentInChildren<ModSettingsBool>();
			component.nameToken = GetNameToken();
			component.settingToken = Identifier;
			gameObject.name = "Mod Option Checkbox, " + Name;
			return gameObject;
		}

		public override BaseOptionConfig GetConfig() => config;

		public override string OptionTypeName { get; protected set; } = "checkbox"; // can't be named ziocheckbox because its used in tokens
		public override ZioConfigEntryBase ConfigEntry => _configEntry;

		public bool GetOriginalValue() => _originalValue;

		public bool ValueChanged()
		{
			throw new NotImplementedException();
		}

		public bool Value
		{
			get => _configEntry.Value;
			set => _configEntry.Value = value;
		}
	}*/
}