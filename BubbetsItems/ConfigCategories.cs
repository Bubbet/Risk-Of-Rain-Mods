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
		private static string[] _categories = {
			"General",
			"Balancing Functions",
			"Disable Mod Parts"
		};

		public static ConfigEntry<T> Bind<T>(this ConfigFile file, ConfigCategoriesEnum which, string key, T defaultValue, string description)
		{
			return file.Bind(_categories[(int) which], key, defaultValue, description);
		}
	}
}