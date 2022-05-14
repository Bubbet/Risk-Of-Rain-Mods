using System;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.TextCore;

namespace MaterialHud
{
	[HarmonyPatch]
	public static class HarmonyPatches
	{
		[HarmonyILManipulator, HarmonyPatch(typeof(CameraRigController), nameof(CameraRigController.Start))]
		public static void ReplaceHUD(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchLdstr("Prefabs/HUDSimple"));
			c.RemoveRange(2);
			c.EmitDelegate<Func<GameObject>>(RiskUIPlugin.CreateHud);
		}

		public static readonly Type hBar = typeof(HealthBar);
		[HarmonyILManipulator, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.UpdateBarInfos))]
		public static void FixInfusionColor(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchLdsfld(hBar, "voidPanelColor"));
			c.Remove();
			c.EmitDelegate<Func<Color>>(() =>
			{
				var str = RiskUIPlugin.VoidColor.Value;//.Trim();
				if (str == Color.clear)
					return Color.HSVToRGB(Mathf.Sin(Time.time) * 0.5f + 0.5f, 1, 1);
				return str; //ColorUtility.TryParseHtmlString(str, out var color) ? color : Color.magenta;
			});
			c.GotoNext(x => x.MatchLdsfld(hBar, "infusionPanelColor"));
			c.Remove();
			c.EmitDelegate<Func<Color>>(() =>
			{
				var str = RiskUIPlugin.InfusionColor.Value;//.Trim();
				if (str == Color.clear)//"rainbow")
					return Color.HSVToRGB(Mathf.Sin(Time.time) * 0.5f + 0.5f, 1, 1);
				return str; //ColorUtility.TryParseHtmlString(str, out var color) ? color : Color.magenta;
			});
			c.GotoNext(x => x.MatchLdsfld(hBar, "voidShieldsColor"));
			c.Remove();
			c.EmitDelegate<Func<Color>>(() =>
			{
				var str = RiskUIPlugin.VoidShieldColor.Value;//.Trim();
				if (str == Color.clear)//"rainbow")
					return Color.HSVToRGB(Mathf.Sin(Time.time) * 0.5f + 0.5f, 1, 1);
				return str; //ColorUtility.TryParseHtmlString(str, out var color) ? color : Color.magenta;
			});
		}

		[HarmonyPrefix, HarmonyPatch(typeof(Run), nameof(Run.InstantiateUi))]
		public static bool OverwriteTracker(Run __instance, Transform uiRoot, ref GameObject __result)
		{
			if (!uiRoot) return true;
			switch (__instance.nameToken)
			{
				case "ECLIPSE_GAMEMODE_NAME":
				case "GAMEMODE_CLASSIC_RUN_NAME":
					__instance.uiInstance = GameObject.Instantiate(RiskUIPlugin.CreateClassicRunHud(), uiRoot);
					__result = __instance.uiInstance;
					return false;
				//case "INFINITETOWER_GAMEMODE_NAME":
					//__instance.uiInstance = GameObject.Instantiate(MaterialHudPlugin.CreateSimulcrum(), uiRoot);
					//__result = __instance.uiInstance;
					//return false;
				default:
					return true;
			}
		}

		/*
		[HarmonyILManipulator,
		 HarmonyPatch(typeof(InfiniteTowerWaveController), nameof(InfiniteTowerWaveController.InstantiateUi))]
		public static void ReplaceWaveUI(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchLdfld<InfiniteTowerWaveController>("uiPrefab"));
			c.GotoNext(x => x.MatchLdfld<InfiniteTowerWaveController>("uiPrefab"));
			c.Remove();
			c.Index++;
			c.Remove();
			c.EmitDelegate<Func<InfiniteTowerWaveController, Transform, GameObject>>((controller, transform) =>
			{
				var obj = GameObject.Instantiate(MaterialHudPlugin.baseWaveUI, transform);
				obj.transform.SetSiblingIndex(1);
				return obj;
			});
		}*/

		[HarmonyPrefix, HarmonyPatch(typeof(DifficultyDef), nameof(DifficultyDef.GetIconSprite))]
		public static bool SwapIcon(DifficultyDef __instance, ref Sprite __result)
		{
			if (__instance.nameToken == null || !RiskUIPlugin.DifficultyIconMap.ContainsKey(__instance.nameToken))
			{
				return true;
			}

			__result = RiskUIPlugin.DifficultyIconMap[__instance.nameToken];
			return false;
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(AllyCardManager), nameof(AllyCardManager.Awake))]
		public static void ReplaceAllyCards(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchLdstr("Prefabs/UI/AllyCard"));
			c.RemoveRange(2);
			c.EmitDelegate<Func<GameObject>>(RiskUIPlugin.CreateAllyCard);
		}

		//[HarmonyILManipulator, HarmonyPatch(typeof(VoidSurvivorController), nameof(VoidSurvivorController.OnEnable))]
		public static void MoveVoidSurvivorController(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchLdarg(0), x => x.MatchLdfld<VoidSurvivorController>("overlayChildLocatorEntry"));
			c.RemoveRange(2);
			c.Emit(OpCodes.Ldstr, "BottomLeftCluster");
		}

		[HarmonyPostfix, HarmonyPatch(typeof(EnemyInfoPanel), nameof(EnemyInfoPanel.Init))]
		public static void ChangeMonsterInventory()
		{
			EnemyInfoPanel.panelPrefab = RiskUIPlugin.EnemyInfoPanel;
		}
	}
}