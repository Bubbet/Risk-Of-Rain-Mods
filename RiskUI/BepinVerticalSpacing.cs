using System;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialHud
{
	public class BepinVerticalSpacing : MonoBehaviour
	{
		private ConfigEntry<float> configEntry;
		public string desc;
		public string key;
		private GridLayoutGroup target;

		public float Value
		{
			get => target.spacing.y;
			set
			{
				var targetSpacing = target.spacing;
				targetSpacing.y = value;
				target.spacing = targetSpacing;
			}
		}
		
		private void Awake()
		{
			target = GetComponent<GridLayoutGroup>();
			configEntry = ConfigHelper.Bind("Repositioning", key, Value, desc, riskOfOptionsExtra: 200f);
			configEntry.SettingChanged += SettingChanged;
		}

		private void OnEnable()
		{
			SettingChanged(null, null);
		}

		private void OnDestroy()
		{
			configEntry.SettingChanged -= SettingChanged;
		}

		private void SettingChanged(object sender, EventArgs e)
		{
			Value = configEntry.Value;
		}
	}
}