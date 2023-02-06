using System.Linq;
using BepInEx.Bootstrap;

namespace BubbetsItems
{
	public static class ItemStatsCompat
	{
		public static bool IsEnabled => Chainloader.PluginInfos.ContainsKey("com.Moffein.ItemStats");
		public static void Init()
		{
			if (IsEnabled)
				ModIsEnabledInit();
		}

		public static void ModIsEnabledInit()
		{
			ItemStats.ItemStats.IgnoredItems.AddRange(ItemBase.Items.Select(x => x.ItemDef));
			ItemStats.ItemStats.IgnoredEquipment.AddRange(EquipmentBase.Equipments.Select(x => x.EquipmentDef));
		}
	}
}