using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using RoR2;
using RoR2.Networking;
using Unity.Audio;
using UnityEngine.Networking;

namespace BubbetsItems
{
	public enum ConfigCategoriesEnum
	{
		General,
		BalancingFunctions,
		DisableModParts,
		EquipmentCooldowns,
	}

	public static class ConfigCategories
	{
		public static readonly string[] Categories = {
			"General",
			"Balancing Functions",
			"Disable Mod Parts",
			"Equipment Cooldowns"
		};

		public static ConfigEntry<T> Bind<T>(this ConfigFile file, ConfigCategoriesEnum which, string key, T defaultValue, string description, T? oldDefault = default, bool networked = true)
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

			if (networked)
			{
				configEntry.SettingChanged += Changed;
				configEntries[Categories[(int) which] + "_" + key] = configEntry;
			}

			return configEntry;
		}

		private static void Changed(object sender, EventArgs e)
		{
			try
			{
				if (!NetworkServer.active) return;
				NetworkServer.SendToAll(MsgType, new ConfigSync((ConfigEntryBase) sender));
			}
			catch (Exception f)
			{
				BubbetsItemsPlugin.Log.LogError(f);
			}
		}

		public static Dictionary<string, ConfigEntryBase> configEntries = new();
		private const short MsgType = 389;

		public static void Init()
		{
			NetworkUser.onNetworkUserDiscovered += UserConnected;
			NetworkManagerSystem.onStartClientGlobal += RegisterMessages;
		}
		
		public static void RegisterMessages(NetworkClient client)
		{
			try
			{
				client.RegisterHandler(MsgType, ConfigSync.Handle);
				client.RegisterHandler(MsgType + 1, ConfigSyncAll.Handle);
			}
			catch (Exception f)
			{
				BubbetsItemsPlugin.Log.LogError(f);
			}
		}

		private static void UserConnected(NetworkUser networkuser)
		{
			try
			{
				if (!NetworkServer.active) return;
				if (networkuser.connectionToClient == null) return; // escape your own user connecting
				networkuser.connectionToClient.Send(MsgType + 1, new ConfigSyncAll());
			}
			catch (Exception f)
			{
				BubbetsItemsPlugin.Log.LogError(f);
			}
		}

		public class ConfigSync : MessageBase
		{
			public string category = null!;
			public string key = null!;
			public string valueSerialized = null!;
			public int type;
			public ConfigEntryBase entry = null!;
			private object? value;

			public ConfigSync(ConfigEntryBase config)
			{
				key = config.Definition.Key;
				category = config.Definition.Section;
				type = GetTypeFromValue(config.SettingType);
				valueSerialized = TomlTypeConverter.ConvertToString(config.BoxedValue, config.SettingType);
				entry = config;
				value = config.BoxedValue;
			}

			public ConfigSync(){}

			private static int GetTypeFromValue(Type configSettingType)
			{
				var i = 0;
				foreach (var type in TomlTypeConverter.GetSupportedTypes())
				{
					if (type == configSettingType) return i;
					i++;
				}
				return -1;
			}

			private static Type? GetValueFromType(int type)
			{
				var i = 0;
				foreach (var supportedType in TomlTypeConverter.GetSupportedTypes())
				{
					if (i == type) return supportedType;
					i++;
				}
				return null;
			}

			public override void Serialize(NetworkWriter writer)
			{
				base.Serialize(writer);
				writer.Write(category);
				writer.Write(key);
				writer.Write(type);
				writer.Write(valueSerialized);
			}

			public override void Deserialize(NetworkReader reader)
			{
				base.Deserialize(reader);
				category = reader.ReadString();
				key = reader.ReadString();
				type = reader.ReadInt32();
				valueSerialized = reader.ReadString();
				entry = configEntries[category + "_" + key];
				value = TomlTypeConverter.ConvertToValue(valueSerialized, GetValueFromType(type));
			}

			public static void Handle(NetworkMessage netmsg)
			{
				netmsg.ReadMessage<ConfigSync>().TempSet();
			}

			public void TempSet()
			{
				var save = entry.ConfigFile.SaveOnConfigSet;
				entry.ConfigFile.SaveOnConfigSet = false;
				entry.BoxedValue = value;
				entry.ConfigFile.SaveOnConfigSet = save;
			}
		}
	}

	public class ConfigSyncAll : MessageBase
	{
		private readonly List<ConfigCategories.ConfigSync> configs;
		public static void Handle(NetworkMessage netmsg)
		{
			netmsg.ReadMessage<ConfigSyncAll>().TempSet();
		}

		public ConfigSyncAll()
		{
			configs = ConfigCategories.configEntries.Values.Select(x => new ConfigCategories.ConfigSync(x)).ToList();
		}

		public void TempSet()
		{
			foreach (var config in configs)
			{
				config.TempSet();
			}
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);
			for(var i = 0; i < reader.ReadPackedUInt32(); i++){
				configs.Add(reader.ReadMessage<ConfigCategories.ConfigSync>());
			}
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);
			writer.WritePackedUInt32((uint) configs.Count);
			foreach (var config in configs)
			{
				writer.Write(config);
			}
		}
	}
}