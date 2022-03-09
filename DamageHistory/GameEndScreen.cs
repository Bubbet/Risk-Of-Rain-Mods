using HarmonyLib;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DamageHistory
{
	[HarmonyPatch]
	public static class GameEndScreen
	{
		private static HGTextMeshProUGUI _textMesh;

		private static void BuildHud(RectTransform instanceStatContentArea)
		{
			var uiContainer = new GameObject("DamageBreakdownUIContainer");
			RectTransform rectTransform = uiContainer.AddComponent<RectTransform>();
			rectTransform.SetParent(instanceStatContentArea);
			rectTransform.position = instanceStatContentArea.position;
			rectTransform.sizeDelta = instanceStatContentArea.sizeDelta;
			rectTransform.localScale = new Vector3(1f, -1f, 1f);

			GameObject gameObject2 = new GameObject("DisplayText");
			RectTransform rectTransform2 = gameObject2.AddComponent<RectTransform>();
			_textMesh = gameObject2.AddComponent<HGTextMeshProUGUI>();
			gameObject2.AddComponent<LayoutElement>();
			gameObject2.transform.SetParent(uiContainer.transform);
			rectTransform2.localPosition = Vector3.zero;
			rectTransform2.anchorMin = Vector2.zero;
			rectTransform2.anchorMax = Vector2.one;
			rectTransform2.localScale = new Vector3(1f, -1f, 1f);
			rectTransform2.sizeDelta = Vector2.zero;
			rectTransform2.anchoredPosition = Vector2.zero;

			LayoutElement layoutElement = gameObject2.AddComponent<LayoutElement>();
			layoutElement.minWidth = 100f;
			layoutElement.minHeight = 2f;
			layoutElement.flexibleHeight = 1000f;
			layoutElement.flexibleWidth = 1f;

			var fuckoff = rectTransform2.localPosition;
			fuckoff.x = 300f;
			fuckoff.y += 50f;
			rectTransform2.localPosition = fuckoff;
            
			_textMesh.fontSize = 13;
			_textMesh.SetText("This is string");
		}
		
		
		[HarmonyPostfix, HarmonyPatch(typeof(GameEndReportPanelController), nameof(GameEndReportPanelController.Awake))]
		private static void MakeBoxForDamageInfo(GameEndReportPanelController __instance)
		{
			BuildHud(__instance.statContentArea);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(GameEndReportPanelController), nameof(GameEndReportPanelController.SetPlayerInfo))]
		private static void FillBoxWithPlayerInfo(RunReport.PlayerInfo playerInfo)
		{
			var body = playerInfo.master?.GetBody();
			if (body != null) {
				var behavior = body.GetComponent<DamageHistoryBehavior>();
				if (behavior is not null) _textMesh.SetText(DamageHistoryHUD.BuildString(behavior.history, who: body.GetUserName(), verbose: true));
			}
			else
			{
				_textMesh.SetText("");
				DamageHistoryPlugin.Log.LogWarning($"Body not found for player {playerInfo.name}. (Probably cleaned up)");
			}
		}
	}
}