using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
		private readonly Dictionary<string, HealthBarConfig> entries = new();
		
		public void Awake()
		{
			var style = healthBar.style;
			SetupConfig("Trailing Under", "When hurt the bit that is slow to catch up.", style, "trailingUnderHealthBarStyle");
			SetupConfig("Instant Health", "From medkits, etc.", style, "instantHealthBarStyle");
			SetupConfig("Trailing Over", "The general color of the healthbar.", style, "trailingOverHealthBarStyle");
			SetupConfig("Shield", "Shield", style, "shieldBarStyle");
			SetupConfig("Curse", "Curse refers to having Shaped glass / anything that temporarily lowers max HP & normally leaves you w/ the white outlined section.", style, "curseBarStyle");
			SetupConfig("Barrier", "Barrier", style, "barrierBarStyle");
			SetupConfig("Flash", "?", style, "flashBarStyle");
			SetupConfig("Cull", "Cull refers to things like guillotine / freeze, and is only seen when an allied minion (turret / zoea / drone, etc..) is hit by a Glacial Elite's death effect radius. Cull can't be applied to players however.", style, "cullBarStyle");
			SetupConfig("Low Health Over", "Color for delicate watches and things like that.", style, "lowHealthOverStyle");
			SetupConfig("Low Health Under", "?", style, "lowHealthUnderStyle");
			SetupConfig("Magnetic", "A unused??? healthbar style.", style, "magneticStyle");
			SetupConfig("OSP", "One shot protection color.", style, "ospStyle");
			OnEnable();
		}

		private void OnEnable()
		{
			foreach (var entry in entries)
			{
				entry.Value.ConfigChanged(null, null);
			}
		}

		
		private void SetupConfig(string key, string desc, HealthBarStyle style, string field)
		{
			var entryKey = "Recoloring " + whichBar + key;
			var config = new HealthBarConfig("Recoloring " + whichBar, key, desc, field, style);
			entries[entryKey] = config;
		}

		private void OnDestroy()
		{
			foreach (var entry in entries)
			{
				entry.Value.Destroy();
			}
		}

		private void Update()
		{
			var color = Color.HSVToRGB(Mathf.Sin(Time.time) * 0.5f + 0.5f, 1, 1);
			foreach (var rainbow in entries)
			{
				rainbow.Value.Update(color);
			}
		}

		public class HealthBarConfig
		{
			private static readonly Type StyleType = typeof(HealthBarStyle);
			public HealthBarStyle style;
			private readonly FieldInfo _fieldInfo;
			private ConfigEntry<Color> _configValue;
			private bool rainbow;

			public Color Color
			{
				get => ((HealthBarStyle.BarStyle) _fieldInfo.GetValue(style)).baseColor;
				set
				{
					var sty = (HealthBarStyle.BarStyle) _fieldInfo.GetValue(style);
					sty.baseColor = value;
					_fieldInfo.SetValue(style, sty);
				}
			}

			public HealthBarConfig(string category, string key, string desc, string field, HealthBarStyle styleIn)
			{
				style = styleIn;
				_fieldInfo = StyleType.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				_configValue = ConfigHelper.Bind(category, key, Color, desc);
				_configValue.SettingChanged += ConfigChanged;
			}

			public void Destroy()
			{
				_configValue.SettingChanged -= ConfigChanged;
			}

			public void ConfigChanged(object sender, EventArgs e)
			{
				rainbow = _configValue.Value == Color.clear;
				Color = _configValue.Value;
			}

			public void Update(Color color)
			{
				if (rainbow)
					Color = color;
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