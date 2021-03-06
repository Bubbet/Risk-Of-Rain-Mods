using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BetterUI;
using HarmonyLib;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using SimpleJSON;
using UnityEngine;
using Path = System.IO.Path;
using SearchableAttribute = HG.Reflection.SearchableAttribute;
[assembly: SearchableAttribute.OptIn]

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace MaterialHud
{
	[BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInPlugin("bubbet.riskui", "Risk UI", "1.3.1")]
	[BepInDependency("com.Dragonyck.Synergies", BepInDependency.DependencyFlags.SoftDependency)]
	public class RiskUIPlugin : BaseUnityPlugin
	{
		public AssetBundle assetBundle;
		private static GameObject _newHud;
		private static GameObject _newClassicRunHud;
		private static GameObject _newSimulacrumHud;
		private static GameObject _allyCard;
		public static GameObject BaseWaveUI;
		public static GameObject EnemyInfoPanel;

		public static readonly Dictionary<string, Sprite> DifficultyIconMap = new();
		public static ConfigEntry<Color> VoidColor;
		public static ConfigEntry<Color> InfusionColor;
		public static ConfigEntry<Color> VoidShieldColor;
		public static ConfigFile ConfigFile;
		private string description;
		private Sprite icon;
		public static ConfigEntry<bool> Enabled;
		private Harmony harm;
		private PatchClassProcessor patcher;

		public static bool RiskOfOptionsEnabled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
		public void Awake()
		{
			var path = Path.GetDirectoryName(Info.Location);
			try
			{
				var jsonNode = JSON.Parse(File.OpenText(Path.Combine(path, "manifest.json")).ReadToEnd());
				description = jsonNode["description"].ToString();
				var iconStream = File.ReadAllBytes(Path.Combine(path, "icon.png"));
				var tex = new Texture2D(256, 256);
				tex.LoadImage(iconStream);
				icon = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
			}
			catch (Exception)
			{
				// ignored
			}

			harm = new Harmony(Info.Metadata.GUID);
			patcher = new PatchClassProcessor(harm, typeof(HarmonyPatches));

			ConfigFile = Config;
			Enabled = ConfigHelper.Bind("General", "Enabled", true, "Should the hud be replaced. Only updates on hud awake, so stage change and starting new runs.");
			Enabled.SettingChanged += EnabledChanged;
			EnabledChanged();
			
			if (RiskOfOptionsEnabled)
				MakeRiskofOptions();

			if (Chainloader.PluginInfos.ContainsKey("com.Dragonyck.Synergies") && Chainloader.PluginInfos["com.Dragonyck.Synergies"].Metadata.Version <= new Version("2.0.3"))
				DisableSynergies();
			
			VoidColor = ConfigHelper.Bind("Recoloring Player HealthBar", "Void Color", (Color) new Color32(181, 100, 189, 255), "Color of void, Void Fiends health bar.");
			InfusionColor = ConfigHelper.Bind("Recoloring Player HealthBar", "Infusion Color", (Color) new Color32(221, 44, 38, 255), "Color of infusion.");
			VoidShieldColor = ConfigHelper.Bind("Recoloring Player HealthBar", "Void Shield Color", (Color) new Color32(229, 127, 240, 255), "Color of void shield.");
			
			assetBundle = AssetBundle.LoadFromFile(Path.Combine(path, "riskui"));
			_newHud = assetBundle.LoadAsset<GameObject>("RiskUI");
			_newClassicRunHud = assetBundle.LoadAsset<GameObject>("MaterialClassicRunInfoHudPanel");
			_allyCard = assetBundle.LoadAsset<GameObject>("MaterialAllyCard");
			EnemyInfoPanel = assetBundle.LoadAsset<GameObject>("MaterialMonsterItemInventory");
			
			_newSimulacrumHud = assetBundle.LoadAsset<GameObject>("MaterialSimulacrum");
			BaseWaveUI = assetBundle.LoadAsset<GameObject>("MaterialDefaultWaveUI");

			DifficultyIconMap["SUNNY_NAME"] = assetBundle.LoadAsset<Sprite>("Sunny (More Difficulties)");
			DifficultyIconMap["RAINSOON_NAME"] = assetBundle.LoadAsset<Sprite>("ThunderStorm (More Difficulties)");
			DifficultyIconMap["HIFU_DIFFICULTY_NAME"] = assetBundle.LoadAsset<Sprite>("Inferno (Inferno)");
			DifficultyIconMap["DIFFICULTY_CONFIGURABLEDIFFICULTYMOD_NAME"] = assetBundle.LoadAsset<Sprite>("Pluviculture (ConfigurableDifficulty)");
			DifficultyIconMap["Mico27_DIFFICULTY_TROPICALSTORM_NAME"] = assetBundle.LoadAsset<Sprite>("Tropical Storm (Tropical Storm)");
			DifficultyIconMap["GROOVYDIFFICULTY_4_NAME"] = assetBundle.LoadAsset<Sprite>("Deluge (UntitledDifficultyMod)");
			DifficultyIconMap["CALYPSO_NAME"] = assetBundle.LoadAsset<Sprite>("Calypso (More Difficulties)");
			DifficultyIconMap["GROOVYDIFFICULTY_5_NAME"] = assetBundle.LoadAsset<Sprite>("Charybdis (UntitledDifficultyMod)");
			DifficultyIconMap["TEMPEST_NAME"] = assetBundle.LoadAsset<Sprite>("Tempest (More Difficulties)");
			DifficultyIconMap["SCYLLA_NAME"] = assetBundle.LoadAsset<Sprite>("Armageddon (More Difficulties)");

			DifficultyIconMap["DIFFICULTY_EASY_NAME"] = assetBundle.LoadAsset<Sprite>("Drizzle");
			DifficultyIconMap["DIFFICULTY_NORMAL_NAME"] = assetBundle.LoadAsset<Sprite>("Rainstorm");
			DifficultyIconMap["DIFFICULTY_HARD_NAME"] = assetBundle.LoadAsset<Sprite>("Monsoon");
			
		}

		private void DisableSynergies()
		{
			new PatchClassProcessor(harm, typeof(DisableSynergies)).Patch();
		}

		private void EnabledChanged()
		{
			if (Enabled.Value)
			{
				patcher.Patch();
				return;
			}
			harm.UnpatchSelf();
		}

		private void EnabledChanged(object sender, EventArgs e)
		{
			EnabledChanged();
		}

		private void MakeRiskofOptions()
		{
			if(icon)
				ModSettingsManager.SetModIcon(icon);
			if(description != null)
				ModSettingsManager.SetModDescription(description);
			ModSettingsManager.AddOption(new GenericButtonOption("Report An Issue", "General", "If you find a bug in the mod, reporting an issue is the best way to ensure it gets fixed.","Open Link", () =>
			{
				Application.OpenURL("https://github.com/Bubbet/Risk-Of-Rain-Mods/issues/new");
			}));
			ModSettingsManager.AddOption(new GenericButtonOption("Donate to Bubbet", "General", "Donate to the programmer of RiskUI.","Open Link", () =>
			{
				Application.OpenURL("https://ko-fi.com/bubbet");
			}));
		}

		[SystemInitializer]
		private void CheckForBetterUI()
		{
			if (Chainloader.PluginInfos.ContainsKey("com.xoxfaby.BetterUI"))
			{
				EditBetterUIConfigs();
			}
		}
		
		private void EditBetterUIConfigs()
		{
			ConfigManager.StatsDisplayPanelBackground.Value = false;
			ConfigManager.DPSMeterWindowBackground.Value = false;
			ConfigManager.ConfigFileStatsDisplay.Save();
			ConfigManager.ConfigFileDPSMeter.Save();
		}

		public static GameObject CreateHud()
		{
			return _newHud;
		}

		public static GameObject CreateClassicRunHud()
		{
			return _newClassicRunHud;
		}
		public static GameObject CreateSimulcrum()
		{
			return _newSimulacrumHud;
		}
		public static GameObject CreateAllyCard()
		{
			return _allyCard;
		}
	}
}