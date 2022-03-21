using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using InLobbyConfig;
using InLobbyConfig.Fields;
using NCalc;

namespace BubbetsItems
{
    public static class InLobbyConfigCompat
    {
        public static bool IsEnabled => Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.InLobbyConfig");
        public static void Init()
        {
            if (!IsEnabled) return;
            ModIsEnabledInit();
        }

        public static void ModIsEnabledInit()
        {
            var dict = new Dictionary<ConfigCategoriesEnum, List<object>>();

            var i = 0;
            foreach (var _ in ConfigCategories.Categories)
            {
                dict.Add((ConfigCategoriesEnum) i, new List<object>());
                i++;
            }
            
            var configEntry = new ModConfigEntry {DisplayName = "Bubbet's Items"};
            foreach (var sharedBase in SharedBase.Instances)
            {
                sharedBase.MakeInLobbyConfig(dict);
            }

            foreach (var pair in dict)
            {
                configEntry.SectionFields.Add(ConfigCategories.Categories[(int) pair.Key], pair.Value.Cast<IConfigField>());
            }
            ModConfigCatalog.Add(configEntry);
        }
    }
}