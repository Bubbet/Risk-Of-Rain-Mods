using System.Reflection;
using BepInEx.Configuration;

namespace Titanfall2Mod
{
    public static class Config
    {
        public static ConfigEntry<float> DamageNerf;
        public static ConfigEntry<float> MeterGainKillAsPilot;
        public static ConfigEntry<float> MeterGainKillAsTitan;
        public static ConfigEntry<float> MeterGainDamageToBossesAsTitan;
        public static ConfigEntry<float> MeterGainPerSecondAsPilot;
        public static ConfigEntry<float> MeterLossPerSecondAsPilot;
        public static ConfigEntry<float> MeterGainMultiplierForElite;
        public static ConfigEntry<float> RangeMult;
        public static ConfigEntry<float> RangeAdd;
        public static ConfigEntry<float> FireRateMult;

        public static void Init(ConfigFile configFile)
        {
            DamageNerf = configFile.Bind("Balancing", "Damage Nerf",10f, "Damage Nerf for skills. larger = less damage dealt");
            RangeMult = configFile.Bind("Balancing", "Range Mult",0.05f, "Range Multiplier for Hammer to Unity Conversion");
            RangeAdd = configFile.Bind("Balancing", "Range Add",1000f, "Added range on top of weapons standard range");
            FireRateMult = configFile.Bind("Balancing", "Fire Rate Mult",1.175f, "Fire Rate Multiplier");

            MeterGainKillAsPilot = configFile.Bind("Balancing", "MeterGainKillAsPilot", 0.04f, "Temp meter reward for kill.");
            MeterGainKillAsTitan = configFile.Bind("Balancing", "MeterGainKillAsTitan", 0.01f, "Meter reward for kill.");
            MeterGainDamageToBossesAsTitan = configFile.Bind("Balancing", "MeterGainDamageToBossesAsTitan", 0.01f, "Meter reward for damaging bosses."); // TODO figure out a value for this; Beetle queen has 2100 hp at default
            MeterGainPerSecondAsPilot = configFile.Bind("Balancing", "MeterGainPerSecondAsPilot", 0.003236245955f, "Meter per second. Default works out to exactly 309 seconds (just over 5 minutes)");
            MeterLossPerSecondAsPilot = configFile.Bind("Balancing", "MeterLossPerSecondAsPilot", 0.0033f, "Temp meter loss per second. (Orange Meter) Should be slightly higher than gain per second.");
            MeterGainMultiplierForElite = configFile.Bind("Balancing", "MeterGainMultiplierForElite", 5f, "Meter reward * by this value for killing/damaging an elite.");
            
            IgnoreConfigs();
        }

        private static void IgnoreConfigs()
        {
            foreach (var field in typeof(Config).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                var variable = field.GetValue(null);
                variable.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance).SetValue(variable, ((ConfigEntryBase) variable).DefaultValue);
            }
        }
    }
}