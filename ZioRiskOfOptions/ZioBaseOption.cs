using RiskOfOptions.Options;
using ZioConfigFile;

namespace ZioRiskOfOptions
{
	public static class ZioBaseOption // : BaseOption
	{
		public static void SetProperties(BaseOption option, ZioConfigEntryBase configEntry)
		{
			if (configEntry == null) return;
			var config = option.GetConfig();
			option.SetCategoryName(configEntry.Definition.Section, config);
			option.SetName(configEntry.Definition.Key, config);
			option.SetDescription(configEntry.Description.Description, config);
		}
	}
}