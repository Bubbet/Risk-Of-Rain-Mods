using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialHud
{
	public class HealthbarRecolor : MonoBehaviour
	{
		public string whichBar = "Player HealthBar";
		public HealthBar healthBar;
		private readonly Dictionary<string, ConfigEntry<Color>> entries = new();
		private readonly Dictionary<string, Action<Color>> updateFunctions = new();
		public void Awake()
		{
			var style = healthBar.style;
			SetupConfig("Trailing Under", "When hurt the bit that is slow to catch up.", style.trailingUnderHealthBarStyle.baseColor, color => style.trailingUnderHealthBarStyle.baseColor = color);
			SetupConfig("Instant Health", "From medkits, etc.", style.instantHealthBarStyle.baseColor, color => style.instantHealthBarStyle.baseColor = color);
			SetupConfig("Trailing Over", "The general color of the healthbar.", style.trailingOverHealthBarStyle.baseColor, color => style.trailingOverHealthBarStyle.baseColor = color);
			SetupConfig("Shield", "Shield", style.shieldBarStyle.baseColor, color => style.shieldBarStyle.baseColor = color);
			SetupConfig("Curse", "?", style.curseBarStyle.baseColor, color => style.curseBarStyle.baseColor = color);
			SetupConfig("Barrier", "Barrier", style.barrierBarStyle.baseColor, color => style.barrierBarStyle.baseColor = color);
			SetupConfig("Flash", "?", style.flashBarStyle.baseColor, color => style.flashBarStyle.baseColor = color);
			SetupConfig("Cull", "?", style.cullBarStyle.baseColor, color => style.cullBarStyle.baseColor = color);
			SetupConfig("Low Health Over", "Color for delicate watches and things like that.", style.lowHealthOverStyle.baseColor, color => style.lowHealthOverStyle.baseColor = color);
		}

		private void OnEnable()
		{
			foreach (var entry in entries)
			{
				OnSettingChanged(entry.Value, null);
			}
		}


		public readonly List<string> Rainbows = new();
		private void SetupConfig(string key, string desc, Color baseColor, Action<Color> changed)
		{
			var entryKey = "Recoloring " + whichBar + key;
			entries[entryKey] = ConfigHelper.Bind("Recoloring " + whichBar, key, baseColor, desc);
			entries[entryKey].SettingChanged += OnSettingChanged;
			updateFunctions[entryKey] = changed;
			//if (ColorUtility.TryParseHtmlString(entries[entryKey].Value.Trim(), out var color))
				//updateFunctions[entryKey](color);
		}

		private void OnDestroy()
		{
			foreach (var entry in entries)
			{
				entry.Value.SettingChanged -= OnSettingChanged;
			}
		}

		private void OnSettingChanged(object sender, EventArgs e)
		{
			var config = sender as ConfigEntryBase;
			var entryKey = config.Definition.Section + config.Definition.Key;
			var entry = entries[entryKey];
			
			if (entry.Value == Color.clear)
			{
				if (!Rainbows.Contains(entryKey))
					Rainbows.Add(entryKey);
			}
			else if (Rainbows.Contains(entryKey))
			{
				Rainbows.Remove(entryKey);
			}

			//if (!ColorUtility.TryParseHtmlString(entry.Value.Trim(), out var color)) return;
			updateFunctions[entryKey](entry.Value);
		}

		private void Update()
		{
			if (!Rainbows.Any()) return;
			var color = Color.HSVToRGB(Mathf.Sin(Time.time) * 0.5f + 0.5f, 1, 1);
			foreach (var rainbow in Rainbows)
			{
				updateFunctions[rainbow](color);
			}
		}
	}

	public class RedToColorRemapperIndividual : MonoBehaviour
	{
		public string configKey;
		public string configDesc;
		private Color defaultColor; // = new Color32(240, 91, 91, 255); //"#F05B5B";
		private ConfigEntry<Color> _configEntry;
		public MonoBehaviour target;
		private bool _rainbow;

		public static Dictionary<string, ConfigEntry<string>> entries = new();

		public void Awake()
		{
			defaultColor = GetColor();
			_configEntry = ConfigHelper.Bind("Recoloring", configKey, defaultColor, configDesc);
			_configEntry.SettingChanged += UpdateColor;
		}

		private void OnEnable()
		{
			UpdateColor();
		}

		private void OnDestroy()
		{
			_configEntry.SettingChanged -= UpdateColor;
		}

		private void UpdateColor(object sender, EventArgs e)
		{
			UpdateColor();
		}

		private void SetColor(Color color)
		{
			switch (target)
			{
				case TextMeshProUGUI text:
					text.color = color;
					break;
				case Image image:
					image.color = color;
					break;
				case HealthBar healthBar:
					healthBar.style.trailingOverHealthBarStyle.baseColor = color;
					break;
			}
		}
		
		private Color GetColor()
		{
			switch (target)
			{
				case TextMeshProUGUI text:
					return text.color;
				case Image image:
					return image.color;
				case HealthBar healthBar:
					return healthBar.style.trailingOverHealthBarStyle.baseColor;
			}
			return Color.black;
		}
		
		private void UpdateColor()
		{
			if (_configEntry.Value == Color.clear)
			{
				_rainbow = true;
				return;
			}
			//if (!ColorUtility.TryParseHtmlString(_configEntry.Value.Trim(), out var color)) return;
			SetColor(_configEntry.Value);
		}

		private void Update()
		{
			if (!_rainbow) return;
			SetColor(Color.HSVToRGB(Mathf.Sin(Time.time) * 0.5f + 0.5f, 1, 1));
		}
	}
}