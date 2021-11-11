using System;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;

namespace Titanfall2Mod
{
    public class MeterBehavior : MonoBehaviour
    {
        public HUD hud;
        public TextMeshProUGUI text;
        public RectTransform fillTransform;
        public RectTransform fillTempTransform;
        public RectTransform rectTransform;
        public RectTransform markTransform;
        public float Ratio => hud.targetMaster.GetComponent<IMeterBuilding>()?.GetBoostRatio() ?? -1f;
        public float Meter => hud.targetMaster.GetComponent<IMeterBuilding>()?.GetMeter() ?? -1f;
        public float Tempmeter => hud.targetMaster.GetComponent<IMeterBuilding>()?.GetTempMeter() ?? -1f;

        private void FixedUpdate()
        {
            var sizeDelta = rectTransform.sizeDelta;

            var fillTransformSizeDelta = fillTransform.sizeDelta;
            fillTransformSizeDelta.x = Meter * sizeDelta.x;
            fillTransform.sizeDelta = fillTransformSizeDelta;
            
            fillTransformSizeDelta.x = Tempmeter * sizeDelta.x;
            fillTempTransform.sizeDelta = fillTransformSizeDelta;

            var fillTransformPosition = fillTransform.localPosition;
            fillTransformPosition.x = Meter * sizeDelta.x * 0.5f - sizeDelta.x * 0.5f;
            fillTransform.localPosition = fillTransformPosition;
            
            fillTransformPosition.x = fillTransform.sizeDelta.x + Tempmeter * sizeDelta.x * 0.5f - sizeDelta.x * 0.5f;
            fillTempTransform.localPosition = fillTransformPosition;

            if (Tempmeter + Meter > Ratio)
            {
                markTransform.gameObject.SetActive(false);
            }
            else
            {
                markTransform.gameObject.SetActive(true);
                fillTransformPosition.x = sizeDelta.x * Ratio - sizeDelta.x * 0.5f;
                markTransform.localPosition = fillTransformPosition;
            }

            text.SetText($"{Meter + Tempmeter:P}");
        }

        private void OnDestroy()
        {
            Titanfall2ModPlugin.registeredHuds.Remove(hud);
        }
    }
}