using System;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DamageHistory
{
    public class DamageHistoryHUD : MonoBehaviour
    {
        private HUD _hud;
        private HGTextMeshProUGUI _textMesh;

        public void Awake()
        {
            _hud = GetComponent<HUD>();
            BuildHud();
        }

        private void BuildHud()
        {
            var uiContainer = new GameObject("DamageBreakdownUIContainer");
            RectTransform rectTransform = uiContainer.AddComponent<RectTransform>();
            uiContainer.transform.SetParent(_hud.mainContainer.transform);
            rectTransform.localPosition = new Vector3(2, -10, 0);
            //rectTransform.offsetMin = Vector2.left * 300;
            //rectTransform.offsetMin = new Vector2(0, 350);
            //rectTransform.offsetMax = new Vector2(-0.8f, -0.8f);

            rectTransform.anchorMin = Vector2.one;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.localScale = new Vector3(1, -1, 0.7f);
            rectTransform.sizeDelta = new Vector2(100, 1);
            rectTransform.anchoredPosition = new Vector2(0, 0.5f);
            rectTransform.eulerAngles = new Vector3(0, 6, 0);
            rectTransform.pivot = new Vector2(1, 0.5f);

            /*
            var image = uiContainer.AddComponent<Image>();

            var component = _hud.GetComponentInChildren<HudObjectiveTargetSetter>().transform.parent.GetComponent<Image>();
            var component = _hud.objectivePanelController.objectiveTrackerContainer.parent.GetComponent<Image>();
            image.sprite = component.sprite;
            image.color = component.color;
            image.type = Image.Type.Sliced;

            GameObject gameObject = new GameObject("RowIcon");
            RectTransform rectTransform3 = gameObject.AddComponent<RectTransform>();
            rectTransform3.localPosition = Vector3.zero;
            RawImage rawImage = gameObject.AddComponent<RawImage>();
            Texture icon = Resources.Load<Texture>("Textures/BodyIcons/texUnidentifiedKillerIcon");
            rawImage.texture = icon;
            gameObject.transform.parent = uiContainer.transform;
            */
            
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
            
            _textMesh.fontSize = 13;
            _textMesh.SetText("This is string");
        }

        public void Update()
        {
            if (_hud.targetBodyObject == null) return; 
            _textMesh.SetText(_hud.targetBodyObject.GetComponent<DamageHistoryBehavior>()?.BuildString().ToString() ?? "");
        }
    }
}