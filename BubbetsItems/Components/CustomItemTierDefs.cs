using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BubbetsItems
{
	[HarmonyPatch]
	public static class CustomItemTierDefs
	{
		public static List<ItemTierDef> recyclerTiers = new();
		public static List<ItemTierDef> voidTiers = new();

		//public static GameObject VoidLunarDroplet;
		[HarmonyILManipulator,
		 HarmonyPatch(typeof(PickupTransmutationManager), nameof(PickupTransmutationManager.RebuildPickupGroups))]
		public static void RebuildCustomTierRecycler(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchCallOrCallvirt(out _),
				x => x.MatchStsfld(typeof(PickupTransmutationManager), nameof(PickupTransmutationManager.pickupGroups)));
			c.Emit(OpCodes.Dup);
			c.EmitDelegate<Action<List<PickupIndex[]>>>(groups =>
			{
				foreach (var tier in recyclerTiers)
				{
					AddItemTierGroup(tier.tier, ref groups);
				}
			});
		}

		[HarmonyPostfix,
		 HarmonyPatch(typeof(VoidSurvivorController), nameof(VoidSurvivorController.OnInventoryChanged))]
		public static void AddExtraTiersToVoidSurvivor(VoidSurvivorController __instance)
		{
			var inv = __instance.characterBody.inventory;
			if (!inv) return;
			__instance.voidItemCount += voidTiers.Sum(x => inv.GetTotalItemCountOfTier(x.tier));
		}

		public static PickupIndex[] AddItemTierGroup(ItemTier tier, ref List<PickupIndex[]> groups)
		{
			PickupIndex[] array = (from itemDef in ItemCatalog.allItems.Select(ItemCatalog.GetItemDef) where itemDef.tier == tier && !itemDef.ContainsTag(ItemTag.WorldUnique) select PickupCatalog.FindPickupIndex(itemDef.itemIndex)).ToArray();
			
			groups.Add(array);
			foreach (var pickupInd in array)
			{
				PickupTransmutationManager.pickupGroupMap[pickupInd.value] = array;
			}
			
			var pickupIndex = PickupCatalog.FindPickupIndex(tier);
			if (pickupIndex != PickupIndex.none)
			{
				PickupTransmutationManager.pickupGroupMap[pickupIndex.value] = array;
			}
			
			return array;
		}
		
		public static void RebuildCustomTierRecyclerFucked(ILContext il)
		{
			var c = new ILCursor(il);
			object refclass = null;
			MethodReference call = null;
			/*c.GotoNext(MoveType.After,
				x =>
				{
					var val = x.OpCode == OpCodes.Ldloca_S;
					if (val)
						refclass = x.Operand;
					return val;
				},*/
			c.GotoNext(
				x => x.MatchCallOrCallvirt(out call),
				x => x.MatchStsfld(typeof(PickupTransmutationManager), nameof(PickupTransmutationManager.itemTierLunarGroup)));
			c.Emit(OpCodes.Dup);
			if (call == null) return;
			var method = typeof(PickupTransmutationManager).GetMethod(call.Name, BindingFlags.Static | BindingFlags.NonPublic);
			//c.Emit(OpCodes.Ldloca_S, refclass);
			c.EmitDelegate<RefAction>((ref object o) =>
			{
				Debug.Log(o.GetType());
				
				var para = new[] {null!, o};
				foreach (var tier in recyclerTiers)
				{
					para[0] = tier.tier;
					method?.Invoke(null, para);
				}
				o = para[1];
			});
		}

		public delegate void RefAction(ref object obj);

		public static void Init(BubsItemsContentPackProvider bubsItemsContentPackProvider)
		{
			var old = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier3Def.asset").WaitForCompletion();
			//var pickup = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/GenericPickup.prefab").WaitForCompletion();
			//var droplet = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/PickupDroplet.prefab").WaitForCompletion();
			
			/*
			var parent = new GameObject("VoidLunarDropletParent");
			parent.SetActive(false);
			GameObject.DontDestroyOnLoad(parent);
			VoidLunarDroplet = GameObject.Instantiate(old.dropletDisplayPrefab, parent.transform);
			var transform = VoidLunarDroplet.transform.Find("VFX");

			var core = transform.Find("Core").GetComponent<ParticleSystem>();
			var light = transform.Find("Point light").GetComponent<Light>();
			var glow = transform.Find("PulseGlow").GetComponent<ParticleSystem>();

			var mainModule = core.main;
			mainModule.startColor = new ParticleSystem.MinMaxGradient(ColorCatalogPatches.VoidLunarColor);
			
			var mainModule2 = glow.main;
			mainModule2.startColor = new ParticleSystem.MinMaxGradient(ColorCatalogPatches.VoidLunarColor);

			light.color = ColorCatalogPatches.VoidLunarColor;
			*/


			foreach (var itemTierDef in bubsItemsContentPackProvider.itemTierDefs)
			{
				itemTierDef.highlightPrefab = old.highlightPrefab;
				itemTierDef.dropletDisplayPrefab = old.dropletDisplayPrefab;
				recyclerTiers.Add(itemTierDef);
			}

			BubbetsItemsPlugin.VoidLunarTier = bubsItemsContentPackProvider.itemTierDefs[0];
			voidTiers.Add(BubbetsItemsPlugin.VoidLunarTier);
		}
	}
}