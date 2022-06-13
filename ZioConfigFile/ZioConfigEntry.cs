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
	}
}