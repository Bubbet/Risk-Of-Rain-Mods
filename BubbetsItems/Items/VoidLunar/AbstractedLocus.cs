﻿using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace BubbetsItems.Items.VoidLunar
{
	public class AbstractedLocus : ItemBase
	{
		public static ConfigEntry<bool> disableEnemyDamageInArena;

		protected override void MakeTokens()
		{
			base.MakeTokens();
			var name = GetType().Name.ToUpper();
			SimpleDescriptionToken = name + "_DESC_SIMPLE";
			AddToken(name + "_NAME", "Abstracted Locus");
			AddToken(name + "_DESC", "");
			AddToken(name + "_DESC_SIMPLE", "Teleporter zone is 50% (+20% per stack) bigger. Outside of the teleporter radius is filled with Void Fog. Staying in the Void Fog charges the teleporters 60% (+60% per stack) faster. Corrupts all Focused Convergences.");
			AddToken(name + "_PICKUP", "Teleporter zone is larger, outside of the zone is void fog, being in the fog increases teleporter charge speed. Corrupts all Focused Convergences.");
			AddToken(name + "_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[r] * ([a] * 0.2 + 1.3)", "Teleporter Radius", desc: "[a] = item count; [r] = current radius;");
			AddScalingFunction("[r] * ([a] * 0.6 * Max(0, [p]) + 1)", "Void Fog Charge Increase", desc: "[a] = item count; [p] = outside players; [r] = charging rate");
			disableEnemyDamageInArena = sharedInfo.ConfigFile.Bind(ConfigCategoriesEnum.General, "Abstracted Locus Disable Enemy Damage In Arena", false, "Should the void fog hurt the enemies in the Void Fields.");
		}

		public override void MakeRiskOfOptions()
		{
			base.MakeRiskOfOptions();
			ModSettingsManager.AddOption(new CheckBoxOption(disableEnemyDamageInArena));
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			AddVoidPairing(nameof(RoR2Content.Items.FocusConvergence));
		}

		[HarmonyPostfix, HarmonyPatch(typeof(HoldoutZoneController), nameof(HoldoutZoneController.Start))]
		public static void AddBehaviour(HoldoutZoneController __instance)
		{
			if (__instance.applyFocusConvergence)
			{
				__instance.gameObject.AddComponent<AbstractedLocusController>();
			}
		}
	}

	public class AbstractedLocusController : MonoBehaviour
	{
		private HoldoutZoneController holdoutZoneController;
		private Run.FixedTimeStamp enabledTime;
		public int amount;
		private float currentValue;
		private readonly Color materialColor =  new Color( 3.9411764f, 0f, 5f, 1f);
		private AbstractedLocus inst;
		private FogDamageController fogController;
		private bool added;

		private void Awake()
		{
			if (!NetworkServer.active) return;
			inst = SharedBase.GetInstance<AbstractedLocus>()!;
			holdoutZoneController = GetComponent<HoldoutZoneController>();

			var got = false;
			var parent = GameObject.Find("AbstractedLocusFog(Clone)");
			if (parent == null)
			{
				var asset = BubbetsItemsPlugin.AssetBundle.LoadAsset<GameObject>("AbstractedLocusFog");
				parent = Instantiate(asset);
				NetworkServer.Spawn(parent);
			}
			
			fogController = parent.GetComponent<FogDamageController>();
			fogController.AddSafeZone(holdoutZoneController);
		}
		private void OnEnable()
		{
			enabledTime = Run.FixedTimeStamp.now;
			holdoutZoneController.calcRadius += ApplyRadius;
			holdoutZoneController.calcChargeRate += ApplyRate;
			holdoutZoneController.calcColor += ApplyColor;
		}
		private void OnDisable()
		{
			holdoutZoneController.calcColor -= ApplyColor;
			holdoutZoneController.calcChargeRate -= ApplyRate;
			holdoutZoneController.calcRadius -= ApplyRadius;
		}

		private void FixedUpdate()
		{
			amount = Util.GetItemCountForTeam(holdoutZoneController.chargingTeam, inst.ItemDef.itemIndex, true, false);
			if (enabledTime.timeSince < HoldoutZoneController.FocusConvergenceController.startupDelay)
			{
				amount = 0;
			}
			amount = Mathf.Min(amount, 3); // TODO replace 3 with configurable cap
			var target = (float) amount > 0f ? 1f : 0f;
			var num = Mathf.MoveTowards(currentValue, target, 5f * Time.fixedDeltaTime); // TODO replace 5 with configurable cap
			if (currentValue <= 0f && num > 0f)
			{
				//Util.PlaySound("Play_item_lunar_focusedConvergence", gameObject);
			}
			currentValue = num;
		}

		private void ApplyRadius(ref float radius)
		{
			if (amount <= 0) return;
			var info = inst.scalingInfos[0];
			var context = info.WorkingContext;
			context.r = radius;
			radius = info.ScalingFunction(amount);
		}

		private void ApplyRate(ref float rate)
		{
			if (amount <= 0) return;
			var living = HoldoutZoneController.CountLivingPlayers(holdoutZoneController.chargingTeam);
			var charging = HoldoutZoneController.CountPlayersInRadius(holdoutZoneController, transform.position, holdoutZoneController.currentRadius * holdoutZoneController.currentRadius, holdoutZoneController.chargingTeam);
			var outside = living - charging;
			
			if (!holdoutZoneController.isAnyoneCharging)
				rate = Mathf.Pow((float)outside / living, holdoutZoneController.playerCountScaling) / holdoutZoneController.baseChargeDuration;
			
			var info = inst.scalingInfos[1];
			var context = info.WorkingContext;
			context.p = outside;
			context.r = rate;
			rate = info.ScalingFunction(amount);
		}

		private void ApplyColor(ref Color color)
		{
			color = Color.Lerp(color, materialColor, HoldoutZoneController.FocusConvergenceController.colorCurve.Evaluate(currentValue));
		}
	}

	public class AbstractedLocusFogController : MonoBehaviour
	{
		private FogDamageController fog;

		public void Awake()
		{
			fog = GetComponent<FogDamageController>();
			fog.dangerBuffDef = RoR2Content.Buffs.VoidFogMild;

			if (AbstractedLocus.disableEnemyDamageInArena.Value && SceneManager.GetActiveScene().name == "arena")
			{
				var filter = GetComponent<TeamFilter>();
				filter.teamIndex = TeamIndex.Player;
				fog.invertTeamFilter = false;
			}
		}

		bool lastEnabled;
		public void FixedUpdate()
		{
			var fogEnabled = fog.safeZones.Any(x =>
			{
				var hold = x as HoldoutZoneController;
				if (!hold) return false;
				var locus = hold!.GetComponent<AbstractedLocusController>();
				return !hold.wasCharged && locus && locus.amount > 0;
			});
			if (fogEnabled == lastEnabled) return;
			fog.enabled = fogEnabled;
			lastEnabled = fogEnabled;
		}
	} 
}