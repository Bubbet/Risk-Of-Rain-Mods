using System.Collections.Generic;
using System.Linq;
using BubbetsItems.Helpers;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace BubbetsItems
{
	[CreateAssetMenu(menuName = "BubbetsItems/BubVoidEquipmentDef")]
	public class BubVoidEquipmentDef : BubEquipmentDef
	{
		[Tooltip("The default amount of uses when first picking up.")]
		public int defaultCharges = 1;
		[Tooltip("The amount of uses added when converting a equipment.")]
		public int addStock = 1;
		[Tooltip("Multiplied against amount of fuel cells then added to default changes. Or the amount of uses given when getting a new fuel cell.")]
		public int fuelCellMult = 1;
	}

	[HarmonyPatch]
	public static class VoidEquipmentManager
	{
		public static void Init()
		{
			
		}

		public static List<TransformationInfo> TransformationInfos = new();

		[HarmonyPrefix, HarmonyPatch(typeof(EquipmentDef), nameof(EquipmentDef.AttemptGrant))]
		public static bool HookGrant(ref PickupDef.GrantContext context)
		{
			var inventory = context.body.inventory;
			var current = inventory.currentEquipmentIndex;
			var currentEqDef = EquipmentCatalog.GetEquipmentDef(current) as BubVoidEquipmentDef;
			var pickup = PickupCatalog.GetPickupDef(context.controller.pickupIndex)?.equipmentIndex ?? EquipmentIndex.None;
			var pickupDef = EquipmentCatalog.GetEquipmentDef(pickup);
			var pickupEqDef = pickupDef as BubVoidEquipmentDef;

			if (!currentEqDef && pickupEqDef) // no equipment, pickup void
			{
				var comp = inventory.EnsureComponent<VoidEquipmentBehavior>();
				var behavior = context.controller.GetComponent<VoidEquipmentBehavior>();
				if (behavior)
				{
					comp.uses = behavior.uses;
				}
				else
				{
					comp.uses = pickupEqDef!.defaultCharges + inventory.GetItemCount(RoR2Content.Items.EquipmentMagazine) * pickupEqDef.fuelCellMult;
				}
			}

			if (currentEqDef && !pickupEqDef) // void eq, pickup normal
			{
				var comp = inventory.GetComponent<VoidEquipmentBehavior>();
				var transform = TransformationInfos.FirstOrDefault(x => x.originalEquipment == pickup);
				if (!transform.Equals(default)) //  (can be converted)
				{
					if (transform.transformedEquipment == current) // add one stock 
					{
						comp.uses += currentEqDef!.addStock;
						CharacterMasterNotificationQueue.PushEquipmentTransformNotification(context.body.master, PickupCatalog.GetPickupDef(context.controller.pickupIndex)?.equipmentIndex ?? EquipmentIndex.None, current, CharacterMasterNotificationQueue.TransformationType.ContagiousVoid);
						GameObject.Destroy(context.controller.gameObject);
						return false;
					}
					else // Void conversion of non stock adding items
					{ 
						var index = PickupCatalog.FindPickupIndex(transform.transformedEquipment);
						context.controller.NetworkpickupIndex = index;
						var eqdef = EquipmentCatalog.GetEquipmentDef(PickupCatalog.GetPickupDef(index)?.equipmentIndex ?? EquipmentIndex.None) as BubVoidEquipmentDef;
						comp.uses = eqdef!.defaultCharges + inventory.GetItemCount(RoR2Content.Items.EquipmentMagazine) * eqdef.fuelCellMult;
					}
				}
				else // (cant be converted)
				{
					var behavior = context.controller.EnsureComponent<VoidEquipmentBehavior>();
					behavior.uses = comp.uses;
				}
			}

			if (currentEqDef && pickupEqDef) // void eq, pickup void eq
			{
				var comp = inventory.GetComponent<VoidEquipmentBehavior>();
				if (context.controller.EnsureComponent<VoidEquipmentBehavior>(out var behavior))
				{
					var oldUses = behavior.uses;
					behavior.uses = comp.uses;
					comp.uses = oldUses;
				}
				else
				{
					behavior.uses = comp.uses;
				}
			}
			
			return true;
		}

		[HarmonyPrefix, HarmonyPatch(typeof(EquipmentSlot), nameof(EquipmentSlot.Execute))]
		public static bool HookActivate(EquipmentSlot __instance)
		{
			var behaviour = __instance.inventory.GetComponent<VoidEquipmentBehavior>();
			if (!behaviour) return true;
			behaviour.uses--;
			if (behaviour.uses > 0) return true;
			if (behaviour.uses != 0) return false;
			// This notification doesnt work for some reason, probably fails quietly because of newindex being none.
			CharacterMasterNotificationQueue.PushEquipmentTransformNotification(__instance.inventory.GetComponent<CharacterMaster>(), __instance.inventory.currentEquipmentIndex, EquipmentIndex.None, CharacterMasterNotificationQueue.TransformationType.ContagiousVoid);
			__instance.inventory.SetEquipmentIndex(EquipmentIndex.None);
			return true;
		}
		
		public struct TransformationInfo
		{
			public EquipmentIndex originalEquipment;
			public EquipmentIndex transformedEquipment;
		}
		
		public class VoidEquipmentBehavior : MonoBehaviour
		{
			public int uses;
		}
	}
}