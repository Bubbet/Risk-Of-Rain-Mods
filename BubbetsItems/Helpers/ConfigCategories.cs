using System;
using System.Linq;
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
			var sharedEntry = file.Bind("Do Not Touch", "Config Defaults Reset", "",
				"Stores the old defaults that have been reset so they wont be again. Its not going to break anything if you do for some reason need to touch this, its job is just to prevent the config from resetting multiple times if you change the value back to the old default.");
			if (oldDefault != null && !sharedEntry.Value.Contains(configEntry.Definition.Key + ": " + oldDefault) && Equals(configEntry.Value, (T) oldDefault))
			{
				configEntry.Value = (T) configEntry.DefaultValue;
				var entries = sharedEntry.Value.Split(new[]{";"}, StringSplitOptions.RemoveEmptyEntries).Where(x => !x.StartsWith(configEntry.Definition.Key)).ToArray();
				sharedEntry.Value = string.Join("; ", entries) +  configEntry.Definition.Key + ": " + oldDefault + "; ";
			}

			return configEntry;
		}
	}
}