using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Zio;
using Zio.FileSystems;

namespace ZioConfigFile
{
	public class ZioConfigFile : IDictionary<ConfigDefinition, ZioConfigEntryBase>
	{
		protected object _ioLock = new();
		public event Action ConfigReloaded;
		public event Action<ZioConfigEntryBase, object> SettingChanged;
		public BepInPlugin OwnerMetadata { get; }
		public FileSystem FileSystem { get; }
		public UPath FilePath { get; }
		public Dictionary<ConfigDefinition, ZioConfigEntryBase> Entries { get; } = new();
		protected Dictionary<ConfigDefinition, string> OrphanedEntries { get; } = new();
		public static ManualLogSource Logger { get; } = new("ZioConfigFile");
		public static FileSystem BepinConfigFileSystem { get; } = new SubFileSystem(new PhysicalFileSystem(), Paths.ConfigPath);

		public bool SaveOnConfigSet { get; set; } = true;

		public ZioConfigFile(BepInPlugin plugin, bool saveOnInit = true) : this(BepinConfigFileSystem, plugin.Name, saveOnInit, plugin){}
		public ZioConfigFile(FileSystem fileSystem, UPath path, bool saveOnInit) : this(fileSystem, path, saveOnInit, null) {}

		public ZioConfigFile(FileSystem fileSystem, UPath path, bool saveOnInit, BepInPlugin bepInPlugin)
		{
			OwnerMetadata = bepInPlugin;
			FileSystem = fileSystem;
			FilePath = path;
			if (fileSystem.FileExists(path))
			{
				Reload();
			}
			else
			{
				if(!saveOnInit) return;
				Save();
			}
		}

		public void Reload()
		{
			lock (_ioLock)
			{
				OrphanedEntries.Clear();
				using var stream = FileSystem.OpenFile(FilePath, FileMode.OpenOrCreate, FileAccess.Read);
				using var textReader = new StreamReader(stream, Encoding.UTF8);
				
				if (!textReader.EndOfStream)
				{
					var section = "";
					var line = textReader.ReadLine();
					do
					{
						var trim = line!.Trim();
						if (trim.StartsWith("#")) continue;
						if (trim.StartsWith("[") && trim.EndsWith("]"))
						{
							section = trim.Substring(1, trim.Length - 2);
							continue;
						}

						var args = trim.Split('=');
						if (args.Length != 2) continue;
						var definition = new ConfigDefinition(section, args[0].Trim());
						if (Entries.TryGetValue(definition, out var entry))
						{
							entry.SetSerializedValue(args[1].Trim());
						}
						else
						{
							OrphanedEntries[definition] = args[1].Trim();
						}
					} while ((line = textReader.ReadLine()) != null);
				}
			}
			OnConfigReloaded();
		}

		public void OnConfigReloaded()
		{
			var events = ConfigReloaded?.GetInvocationList();
			if (events is null) return;
			foreach (var target in events)
			{
				try
				{
					(target as Action)?.Invoke();
				}
				catch (Exception ex)
				{
					Logger.LogError(ex);
				}
			}
		}

		public void OnSettingChanged(ZioConfigEntryBase changedSetting, object valueBefore)
		{
			var events = SettingChanged?.GetInvocationList();
			if (events is null) return;
			if(SaveOnConfigSet) Save();
			foreach (var target in events)
			{
				try
				{
					(target as Action<ZioConfigEntryBase, object>)?.Invoke(changedSetting, valueBefore);
				}
				catch (Exception ex)
				{
					Logger.LogError(ex);
				}
			}
		}

		public void Save()
		{
			lock (_ioLock)
			{
				using var stream = FileSystem.OpenFile(FilePath, FileMode.OpenOrCreate, FileAccess.Write);
				using var textWriter = new StreamWriter(stream, Encoding.UTF8);
				
				if (OwnerMetadata != null)
				{
					textWriter.WriteLine($"## Settings file was created by plugin {OwnerMetadata.Name} v{OwnerMetadata.Version}");
					textWriter.WriteLine("## Plugin GUID: " + OwnerMetadata.GUID);
					textWriter.WriteLine();
				}

				foreach (var pair in Entries.Select(x => new{x.Key, entry = x.Value, value = x.Value.GetSerializedValue()})
					.Concat(OrphanedEntries.Select(x => new {x.Key, entry = (ZioConfigEntryBase) null, value = x.Value}))
					.GroupBy(x => x.Key.Section).OrderBy(x => x.Key))
				{
					textWriter.WriteLine("[" + pair.Key + "]");
					foreach (var data in pair)
					{
						textWriter.WriteLine();
						data.entry?.WriteDescription(textWriter);
						textWriter.WriteLine(data.Key.Key + " = " + data.value);
					}
					textWriter.WriteLine();
				}
			}
		}

		public bool TryGetEntry<T>(string section, string key, out ZioConfigEntry<T> entry) => TryGetEntry(new ConfigDefinition(section, key), out entry);

		public bool TryGetEntry<T>(ConfigDefinition configDefinition, out ZioConfigEntry<T> entry)
		{
			lock (_ioLock)
			{
				if (Entries.TryGetValue(configDefinition, out var configEntryBase))
				{
					entry = (ZioConfigEntry<T>) configEntryBase;
					return true;
				}

				entry = null;
				return false;
			}
		}
		
		public ZioConfigEntry<T> Bind<T>(ConfigDefinition configDefinition, T defaultValue, ConfigDescription configDescription = null)
		{
			if (!TomlTypeConverter.CanConvert(typeof (T)))
				throw new ArgumentException($"Type {typeof(T)} is not supported by the config system. Supported types: {string.Join(", ", TomlTypeConverter.GetSupportedTypes().Select(x => x.Name).ToArray())}");
			lock (_ioLock)
			{
				if (Entries.TryGetValue(configDefinition, out var configEntryBase)) return (ZioConfigEntry<T>) configEntryBase;
				
				var configEntry = new ZioConfigEntry<T>(configDefinition, defaultValue, configDescription);
				Entries[configDefinition] = configEntry;
				configEntry.SettingChanged += OnSettingChanged;
				if (OrphanedEntries.TryGetValue(configDefinition, out var str))
				{
					configEntry.SetSerializedValue(str);
					OrphanedEntries.Remove(configDefinition);
				}
				if (SaveOnConfigSet)
					Save();
				return configEntry;
			}
		}
		public ZioConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description) => Bind(new ConfigDefinition(section, key), defaultValue, new ConfigDescription(description));
		public ZioConfigEntry<T> Bind<T>(string section, string key, T defaultValue, ConfigDescription configDescription = null) => Bind(new ConfigDefinition(section, key), defaultValue, configDescription);

		public IEnumerator<KeyValuePair<ConfigDefinition, ZioConfigEntryBase>> GetEnumerator() => Entries.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		void ICollection<KeyValuePair<ConfigDefinition, ZioConfigEntryBase>>.Add(KeyValuePair<ConfigDefinition, ZioConfigEntryBase> item)
		{
			lock (_ioLock)
			{
				Entries.Add(item.Key, item.Value);
			}
		}

		public bool Contains(KeyValuePair<ConfigDefinition, ZioConfigEntryBase> item) => Entries.Contains(item);
		public void CopyTo(KeyValuePair<ConfigDefinition, ZioConfigEntryBase>[] array, int arrayIndex)
		{
			lock (_ioLock)
			{
				((ICollection<KeyValuePair<ConfigDefinition, ZioConfigEntryBase>>) Entries).CopyTo(array, arrayIndex);
			}
		}

		public bool Remove(KeyValuePair<ConfigDefinition, ZioConfigEntryBase> item)
		{
			lock (_ioLock)
				return Entries.Remove(item.Key);
		}

		public int Count
		{
			get
			{
				lock (_ioLock)
				{
					return Entries.Count;
				}
			}
		}

		public bool IsReadOnly => false;

		public bool ContainsKey(ConfigDefinition key)
		{
			lock (_ioLock)
			{
				return Entries.ContainsKey(key);
			}
		}

		public void Add(ConfigDefinition key, ZioConfigEntryBase value) => throw new InvalidOperationException("Directly adding a config entry is not supported");
		public bool Remove(ConfigDefinition key)
		{
			lock (_ioLock)
				return Entries.Remove(key);
		}

		public void Clear()
		{
			lock (_ioLock)
			{
				Entries.Clear();
			}
		}

		public bool TryGetValue(ConfigDefinition key, out ZioConfigEntryBase value)
		{
			lock (_ioLock)
			{
				return Entries.TryGetValue(key, out value);
			}
		}

		ZioConfigEntryBase IDictionary<ConfigDefinition, ZioConfigEntryBase>.this[ConfigDefinition key]
		{
			get
			{
				lock (_ioLock)
				{
					return Entries[key];
				}
			}
			set => throw new InvalidOperationException("Directly setting a config entry is not supported");
		}

		public ZioConfigEntryBase this[ConfigDefinition key]
		{
			get
			{
				lock (_ioLock)
				{
					return Entries[key];
				}
			}
		}
		public ZioConfigEntryBase this[string section, string key] => this[new ConfigDefinition(section, key)];

		public ICollection<ConfigDefinition> Keys
		{
			get
			{
				lock (_ioLock)
					return Entries.Keys;
			}
		}
		ICollection<ZioConfigEntryBase> IDictionary<ConfigDefinition, ZioConfigEntryBase>.Values
		{
			get
			{
				lock (_ioLock)
					return Entries.Values;
			}
		}
	}
}