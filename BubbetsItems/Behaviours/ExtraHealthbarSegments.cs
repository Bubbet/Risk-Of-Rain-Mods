using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BubbetsItems.Behaviours
{
	[HarmonyPatch]
	public static class ExtraHealthBarSegments
	{
		private static List<Type> barDataTypes = new();

		public static void AddType<T>() where T : BarData, new()
		{
			barDataTypes.Add(typeof(T));
		}
		
		/*
		public static event Func<BarData> collectExtraHealthBarStyles;
		public static IEnumerable<BarData> CollectBarInfosInvoke()
		{
			return collectExtraHealthBarStyles.GetInvocationList().Select(dele => ((Func<BarData>) dele).Invoke());
		}*/
		
		[HarmonyPostfix, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.Awake))]
		public static void AddTracker(HealthBar __instance)
		{
			__instance.gameObject.AddComponent<BubsExtraHealthbarInfoTracker>().Init(__instance);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.CheckInventory))]
		public static void CheckInventory(HealthBar __instance)
		{
			var tracker = __instance.GetComponent<BubsExtraHealthbarInfoTracker>();
			if (!tracker) return;
			var source = __instance.source;
			if (!source) return;
			var body = source.body;
			if (!body) return;
			var inv = body.inventory;
			if (!inv) return;
			tracker.CheckInventory(inv, body, source);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.UpdateBarInfos))]
		public static void UpdateInfos(HealthBar __instance)
		{
			var tracker = __instance.GetComponent<BubsExtraHealthbarInfoTracker>();
			tracker.UpdateInfo();
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.ApplyBars))]
		public static void ApplyBar(ILContext il)
		{
			var c = new ILCursor(il);

			var cls = -1;
			FieldReference fld = null;
			c.GotoNext(
				x => x.MatchLdloca(out cls),
				x => x.MatchLdcI4(0),
				x => x.MatchStfld(out fld)
			);
			
			c.GotoNext(MoveType.After,
				x => x.MatchCallOrCallvirt<HealthBar.BarInfoCollection>(nameof(HealthBar.BarInfoCollection.GetActiveCount))
			);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<int, HealthBar, int>>((i, bar) =>
			{
				var tracker = bar.GetComponent<BubsExtraHealthbarInfoTracker>();
				i += tracker.barInfos.Count(x => x.info.enabled);
				return i;
			});
			c.Index = il.Instrs.Count - 2;
			c.Emit(OpCodes.Ldloca, cls);
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloca, cls);
			c.Emit(OpCodes.Ldfld, fld);
			c.EmitDelegate<Func<HealthBar, int, int>>((bar, i) =>
			{
				var tracker = bar.GetComponent<BubsExtraHealthbarInfoTracker>();
				tracker.ApplyBar(ref i);
				return i;
				//return tracker.ApplyBar();
			});
			c.Emit(OpCodes.Stfld, fld);;
			/*
			c.Index = il.Instrs.Count - 1;
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc_0);
			c.EmitDelegate<Func<HealthBar, object, object>>((HealthBar bar, object fuck) =>
			{
				var tracker = bar.GetComponent<BubsExtraHealthbarInfoTracker>();
				tracker.ApplyBar(ref fuck);
				return fuck;
			});
			c.Emit(OpCodes.Stloc_0);*/
		}
		
		public abstract class BarData
		{
			public BubsExtraHealthbarInfoTracker tracker;
			public HealthBar bar;
			public HealthBar.BarInfo info;
			public HealthBarStyle.BarStyle? cachedStyle;
			private Image _imageReference;
			public virtual Image ImageReference
			{
				get => _imageReference;
				set
				{
					if (_imageReference && _imageReference != value)
					{
						_imageReference.material = bar.barAllocator.elementPrefab.GetComponent<Image>().material;
					}
					_imageReference = value;
				}
			}

			public abstract HealthBarStyle.BarStyle GetStyle();

			public virtual void UpdateInfo(ref HealthBar.BarInfo info, HealthComponent.HealthBarValues healthBarValues)
			{
				if (cachedStyle == null) cachedStyle = GetStyle();
				var style = cachedStyle.Value;
				
				info.enabled &= style.enabled;
				info.color = style.baseColor;
				info.imageType = style.imageType;
				info.sprite = style.sprite;
				info.sizeDelta = style.sizeDelta;
			}

			public virtual void CheckInventory(ref HealthBar.BarInfo info, Inventory inventory, CharacterBody characterBody, HealthComponent healthComponent) {}
			public virtual void ApplyBar(ref HealthBar.BarInfo info, Image image, ref int i)
			{
				image.type = info.imageType;
				image.sprite = info.sprite;
				image.color = info.color;

				var rectTransform = (RectTransform) image.transform;
				rectTransform.anchorMin = new Vector2(info.normalizedXMin, 0f);
				rectTransform.anchorMax = new Vector2(info.normalizedXMax, 1f);
				rectTransform.anchoredPosition = Vector2.zero;
				rectTransform.sizeDelta = new Vector2(info.sizeDelta * 0.5f + 1f, info.sizeDelta + 1f);

				i++;
			}
		}

		public class BubsExtraHealthbarInfoTracker : MonoBehaviour
		{
			public List<BarData> barInfos;
			public HealthBar healthBar;
			
			public void CheckInventory(Inventory inv, CharacterBody characterBody, HealthComponent healthComponent)
			{
				foreach (var barInfo in barInfos)
				{
					barInfo.CheckInventory(ref barInfo.info, inv, characterBody, healthComponent);
				}
			}
			public void UpdateInfo()
			{
				if (!healthBar || !healthBar.source) return;
				var healthBarValues = healthBar.source.GetHealthBarValues();
				foreach (var barInfo in barInfos)
				{
					if(barInfo.tracker == null)
						barInfo.tracker = this;
					if(barInfo.bar == null) // I cant do this in the init because it loses its reference somehow
						barInfo.bar = healthBar;
					barInfo.UpdateInfo(ref barInfo.info, healthBarValues);
				}
			}
			public void ApplyBar(ref int i)
			{
				foreach (var barInfo in barInfos)
				{
					ref var info = ref barInfo.info;
					if (!info.enabled)
					{
						barInfo.ImageReference = null; // Release the reference.
						continue;
					}

					Image image = healthBar.barAllocator.elements[i];
					barInfo.ImageReference = image;
					barInfo.ApplyBar(ref barInfo.info, image, ref i);
				}
			}

			public void Init(HealthBar healthBar)
			{
				this.healthBar = healthBar;
				barInfos = barDataTypes.Select(dataType => (BarData) Activator.CreateInstance(dataType)).ToList();
			}
		}
	}
}