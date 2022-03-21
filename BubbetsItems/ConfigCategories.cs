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

		public static ConfigEntry<T> Bind<T>(this ConfigFile file, ConfigCategoriesEnum which, string key, T defaultValue, string description)
		{
			return file.Bind(Categories[(int) which], key, defaultValue, description);
		}
	}
}