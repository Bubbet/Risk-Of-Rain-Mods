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
            var configEntrys = new List<IConfigField>();
            foreach (var itemBase in SharedBase.Instances.OfType<ItemBase>().Where(itemBase => itemBase.defaultScalingFunction != null))
            {
                configEntrys.Add(new StringConfigField(itemBase.scaleConfig.Definition.Key, //itemBase.GetType().ToString(), 
                    () => itemBase.scaleConfig.Value, 
                    newValue =>
                    {
                        try
                        {
                            itemBase.scalingFunction = new Expression(newValue).ToLambda<ItemBase.ExpressionContext, float>();
                            itemBase.scaleConfig.Value = newValue;
                        } catch (EvaluationException) {}
                    }));
                //configEntrys.Add(ConfigFieldUtilities.CreateFromBepInExConfigEntry(itemBase.scaleConfig));
            }

            var configEntry = new ModConfigEntry();
            configEntry.DisplayName = "Bubbet's Items";
            configEntry.SectionFields.Add("Scaling Functions", configEntrys);
            foreach (var sharedBase in SharedBase.Instances)
            {
                sharedBase.MakeInLobbyConfig(configEntry);
            }
            ModConfigCatalog.Add(configEntry);
        }
    }
}