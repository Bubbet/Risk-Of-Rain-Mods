using BepInEx.Configuration;

namespace BubbetsItems
{
	public enum ConfigCategoriesEnum
	{
		General,
		BalancingFunctions,
		DisableModParts
	}

	public static class ConfigCategories
	{
		public static readonly string[] Categories = {
			"General",
			"Balancing Functions",
			"Disable Mod Parts"
		};

		public static ConfigEntry<T> Bind<T>(this ConfigFile file, ConfigCategoriesEnum which, string key, T defaultValue, string description, T? oldDefault = default)
		{
			var configEntry = file.Bind(Categories[(int) which], key, defaultValue, description);
			var sharedEntry = file.Bind("Do Not Touch", "Config Defaults Reset", "", "Stores the old defaults that have been reset so they wont be again. Its not going to break anything if you do for some reason need to touch this, its job is just to prevent the config from resetting multiple times if you change the value back to the old default.");
			
			if (oldDefault == null || sharedEntry.Value.Contains(configEntry.Definition.Key + ": " + oldDefault) || !Equals(configEntry.Value, (T) oldDefault)) return configEntry;
			configEntry.Value = (T) configEntry.DefaultValue;
			sharedEntry.Value += configEntry.Definition.Key + ": " + oldDefault + "; ";

			return configEntry;
		}
	}
}