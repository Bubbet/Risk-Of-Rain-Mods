using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RoR2;
using RoR2.UI;
using RoR2.UI.LogBook;
using UnityEngine;
using UnityEngine.UI;

namespace BubbetsItems
{
    [HarmonyPatch]
    public class LogBookPageScalingGraph : MonoBehaviour //Graphic
    {
        public RectTransform RectTransform;
        private ItemBase _item;
        public ManagerGraphic LineRenderer;

        public ItemBase Item
        {
            get => _item;
            set
            {
                _item = value;
            }
        }

        private void FillWidthAndHeight()
        {
            //width = RectTransform.rect.width;
            //height = RectTransform.rect.height;
            //width = RectTransform.sizeDelta.x * 5;
            //height = RectTransform.sizeDelta.y * 5;
            //var rect = transform.parent.GetComponent<RectTransform>().rect;
            var rect = RectTransform.rect;
            width = rect.width;
            height = rect.height;
            //Debug.Log(width);
            LineRenderer.size = new Vector2(width, height);
        }

        public bool built;
        /* TODO remake this
        public void FixedUpdate()
        {
            if (!built && Math.Abs(RectTransform.rect.width - 100) > 0.01f)
                BuildGraph();
        }*/

        //private Func<int, float> test = i => Mathf.Log(i)*3f + 1.15f;
        public void BuildGraph()
        {
            built = true;
            //if (Item.scalingFunction == null) return;
            FillWidthAndHeight();
            var points = new List<float>();
            /*
            for (var i = 0; i < 50; i++)
            {
                points.Add(Item.GraphScalingFunction(i+1));
                //points.Add(test(i+1));
            }*/
            var max = Mathf.Ceil(points.Max());
            for (var i = 0; i < 50; i++)
            {
                var pos = new Vector2(Mathf.Clamp01((float) (i) / 49f) * width - 0.5f * width,
                    Mathf.Clamp01(points[i] / max) * height - 0.5f * height);
                
                GameObject go = new GameObject();
                go.transform.SetParent(LineRenderer.transform, false);

                var img = go.AddComponent<Image>();
                img.rectTransform.sizeDelta = new Vector2(8.0f, 8.0f);
                img.rectTransform.localPosition = pos;
                
                var tooltip = go.AddComponent<TooltipProvider>();
                tooltip.SetContent(new TooltipContent {overrideTitleText = "Scaling Value", overrideBodyText = $"Amount: {i+1}, Value: {points[i]}", titleColor = Color.grey});
                
                LineRenderer.lineStrip.Add(pos);
            }

            LineRenderer.gridSize.y = (int) max;
            
            LineRenderer.SetVerticesDirty();
        }
        
        public int Granularity = 50;

        public Vector2Int gridSize = new Vector2Int(1, 1);
        public float thickness = 10f;

        private float width;
        private float height;




        /*
        private void BuildGraph()
        {
            var granularity = 50;
            var gover2 = granularity * 0.5;
            var sizeDelta = RectTransform.sizeDelta;
            var xwidth = sizeDelta.x / granularity;
            var amounts = new List<float>();
            for (var i = 0; i < granularity; i++) amounts.Add(Item.scalingFunction(new ItemBase.ExpressionContext(i)));
            var max = amounts.Max();
            var i2 = 0;
            //LineRenderer.SetPositions(Positions(new Vector3[] {});
            //LineRenderer.positionCount = granularity;
            var positions = new List<Vector3>();
            foreach (var yAmount in amounts)
            {
                var y = (float) (yAmount / max * sizeDelta.y - sizeDelta.y * 0.5);
                var x = (float) (xwidth * i2 - gover2);
                var pos = new Vector3(x, y, 1);
                positions.Add(pos);
                i2++;
            }
            LineRenderer.SetPositions(positions.ToArray());
        }*/

        //[HarmonyPostfix, HarmonyPatch(typeof(PageBuilder), nameof(PageBuilder.AddSimplePickup))] TODO
        public static void AddGraph(PageBuilder __instance, PickupIndex pickupIndex)
        {
            if (!SharedBase.PickupIndexes.ContainsKey(pickupIndex)) return;
            var item = SharedBase.PickupIndexes[pickupIndex] as ItemBase;
            //if (item?.scalingFunction == null) return;
            __instance.AddSimpleTextPanel("Scaling Function:");
            var obj = __instance.managedObjects[__instance.managedObjects.Count - 1];
            var graph = Instantiate(BubbetsItemsPlugin.AssetBundle.LoadAsset<GameObject>("LogBookGraph"), obj.transform);
            graph.GetComponent<LogBookPageScalingGraph>().Item = item;
        }
    }
}