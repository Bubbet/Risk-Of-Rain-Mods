using System;
using BepInEx.Configuration;
using RoR2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialHud
{
	public class DifficultyTextController : MonoBehaviour
	{
		public TextMeshProUGUI text;
		public string[] segmentTokens;
		public Image ringImage;
		public Image ringBehind;
		public RedToColorRemapperIndividual textRecolorer;
		public RedToColorRemapperIndividual ringRecolorer;
		public RedToColorRemapperIndividual ringBehindRecolorer;
		public RedToColorRemapperIndividual timerTextRecolorer;
		public RedToColorRemapperIndividual timerCentiTextRecolorer;
		public TextMeshProUGUI monsterLevel;
		public float levelsPerSegment = 3;
		private int _previousSegment = -1;
		private ConfigEntry<bool> _textRecolor;
		private ConfigEntry<bool> _ringLowerRecolor;
		private ConfigEntry<bool> _ringRecolor;
		private ConfigEntry<bool> _timerRecolor;
		private ConfigEntry<bool> _timerCentiRecolor;
		private ConfigEntry<bool> _monsterLevelEnabled;

		public static readonly Color[] DifficultyColors = {
			new(73f / 255f, 242f / 255f, 217f / 255f), 	//Easy: 49F2D9
			new(67f / 255f, 249f / 255f, 114f / 255f), 	//Normal: 43F972
			new(1f, 226f / 255f, 102f / 255f), 			//Hard: FFE266
			new(1f, 169f / 255f, 67f / 255f),				//Very Hard: FFA943
			new(1f, 124f / 255f, 36f / 255f), 			//Insane: FF7C24
			new(1f, 102f / 255f, 67f / 255f), 			//Impossible: FF6643
			new(1f, 67f / 255f, 36f / 255f),				//I see you: FF4324
			new(1f, 26f / 255f, 26f / 255f),				//I'm coming for you: E21A1A
			new(211f / 255f, 0f, 0f), 					//HAHAHA: D30000
		};

		private void Awake()
		{
			_textRecolor = ConfigHelper.Bind("General", "Difficulty Text Colored Per Difficulty", false, "Should the difficulty text be recolored based on current difficulty.");
			_ringLowerRecolor = ConfigHelper.Bind("General", "Difficulty Lower Ring Colored Per Difficulty", false, "Should the difficulty lower ring be recolored based on current difficulty.");
			_ringRecolor = ConfigHelper.Bind("General", "Difficulty Upper Ring Colored Per Difficulty", false, "Should the difficulty upper ring be recolored based on current difficulty.");
			_timerRecolor = ConfigHelper.Bind("General", "Timer Text Colored Per Difficulty", false, "Should the timer text be recolored based on current difficulty.");
			_timerCentiRecolor = ConfigHelper.Bind("General", "Centisecond Timer Text Colored Per Difficulty", false, "Should the timer centisecond text be recolored based on current difficulty.");
			_monsterLevelEnabled = ConfigHelper.Bind("General", "Monster Level Text Enable", false, "Should the monster level text be displayed.");
			_textRecolor.SettingChanged += TextColorerChanged;
			_ringLowerRecolor.SettingChanged += RingLowerColorerChanged;
			_ringRecolor.SettingChanged += RingColorerChanged;
			_timerRecolor.SettingChanged += TimerColorerChanged;
			_timerCentiRecolor.SettingChanged += CentiColorerChanged;
			_monsterLevelEnabled.SettingChanged += MonsterLevelChanged;
			TextColorerChanged(null, null);
			RingLowerColorerChanged(null, null);
			RingColorerChanged(null, null);
			TimerColorerChanged(null, null);
			CentiColorerChanged(null, null);
			MonsterLevelChanged(null, null);
		}

		private void MonsterLevelChanged(object sender, EventArgs e)
		{
			monsterLevel.gameObject.SetActive(_monsterLevelEnabled.Value);
		}

		private void OnDestroy()
		{
			_textRecolor.SettingChanged -= TextColorerChanged;
			_ringLowerRecolor.SettingChanged -= RingLowerColorerChanged;
			_ringRecolor.SettingChanged -= RingColorerChanged;
			_timerRecolor.SettingChanged -= TimerColorerChanged;
			_timerCentiRecolor.SettingChanged -= CentiColorerChanged;
		}

		private void RingColorerChanged(object sender, EventArgs e)
		{
			ringRecolorer.enabled = !_ringRecolor.Value;
			UpdateColors();
		}
		private void RingLowerColorerChanged(object sender, EventArgs e)
		{
			ringBehindRecolorer.enabled = !_ringLowerRecolor.Value;
			UpdateColors();
		}
		private void TextColorerChanged(object sender, EventArgs e)
		{
			textRecolorer.enabled = !_textRecolor.Value;
			UpdateColors();
		}
		
		private void CentiColorerChanged(object sender, EventArgs e)
		{
			timerCentiTextRecolorer.enabled = !_timerCentiRecolor.Value;
			UpdateColors();
		}

		private void TimerColorerChanged(object sender, EventArgs e)
		{
			timerTextRecolorer.enabled = !_timerRecolor.Value;
			UpdateColors();
		}

		public void Update()
		{
			if (!Run.instance) return;
			var ambient = Run.instance.ambientLevel - 1f;
			var ratio = ambient / levelsPerSegment;
			var floored = Mathf.FloorToInt(ratio);
			var remains = ratio - floored;

			if (floored >= segmentTokens.Length)
				remains = 1f;

			if(_monsterLevelEnabled.Value)
				monsterLevel.text = Language.GetStringFormatted("AMBIENT_LEVEL_DISPLAY_FORMAT", ambient + 1f);
			
			var token = Math.Min(segmentTokens.Length - 1, floored);
			if (token != _previousSegment)
			{
				text.text = Language.GetString(segmentTokens[token]).ToUpper();
				_previousSegment = token;
				UpdateColors();
			}
			
			ringImage.fillAmount = remains;
		}

		private void UpdateColors()
		{
			if (!Run.instance) return;
			var which = Math.Max(0, _previousSegment);
			var difficultyColor = DifficultyColors[which];
			if (_ringRecolor.Value)
				((Image) ringRecolorer.target).color = difficultyColor;
			if (_ringLowerRecolor.Value)
				((Image) ringBehindRecolorer.target).color = difficultyColor;
			if (_textRecolor.Value)
				((TextMeshProUGUI) textRecolorer.target).color = difficultyColor;
			if (_timerRecolor.Value)
				((TextMeshProUGUI) timerTextRecolorer.target).color = difficultyColor;
			if (_timerCentiRecolor.Value)
				((TextMeshProUGUI) timerCentiTextRecolorer.target).color = difficultyColor;
		}
	}
}