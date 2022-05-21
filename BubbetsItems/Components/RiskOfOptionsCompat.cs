using BepInEx.Bootstrap;

namespace BubbetsItems
{
	public static class RiskOfOptionsCompat
	{
		public static bool IsEnabled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
		public static void Init()
		{
			if (IsEnabled)
				ModIsEnabledInit();
		}

		private static void ModIsEnabledInit()
		{
			foreach (var sharedBase in SharedBase.Instances)
			{
				sharedBase.MakeRiskOfOptions();
			}
		}
	}
}