using System;
using System.Collections.Generic;
using System.Linq;
using BubbetsItems.Helpers;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
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
			AddEquipmentType<BubVoidEquipmentDef>(100f);
		}
		
		private static List<EquipmentInfo> _typesToCheck = new();
		public static Dictionary<Type, List<PickupIndex>> EquipmentDropTables = new();

		public struct EquipmentInfo
		{
			public Type type;
			public float dropChance;
			public float shrineChance;
			public float shopChance;
			public float scavChance;

			public EquipmentInfo(Type typ, float chance, float shrineChance, float shopChance, float scavChance)
			{
				type = typ;
				dropChance = chance;
				this.shrineChance = shrineChance;
				this.shopChance = shopChance;
				this.scavChance = scavChance;
			}
		}

		public static void AddEquipmentType<T>(float dropChance = 0f, float shrineChance = 0f, float shopChance = 0f, float scavChance = 0f) where T : EquipmentDef
		{
			var typ = typeof(T);
			if (_typesToCheck.Any(x => x.type == typ)) return;
			_typesToCheck.Add(new EquipmentInfo(typ, dropChance, shrineChance, shopChance, scavChance));
		}

		#region dropRelated
		[HarmonyPostfix, HarmonyPatch(typeof(BasicPickupDropTable), nameof(BasicPickupDropTable.GenerateWeightedSelection))]
		public static void AddToDropTable(BasicPickupDropTable __instance)
		{
			foreach (var info in _typesToCheck)
			{
				__instance.Add(EquipmentDropTables[info.type], info.dropChance);
			}
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(Inventory), nameof(Inventory.GiveRandomEquipment), new Type[] { })]
		public static void GiveRandomEquipment(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchStloc(out _));
			c.EmitDelegate<Func<PickupIndex, PickupIndex>>(index => RandomPickup(Run.instance.treasureRng, index, "randomEquipment"));
		}
		[HarmonyILManipulator, HarmonyPatch(typeof(Inventory), nameof(Inventory.GiveRandomEquipment), typeof(Xoroshiro128Plus))]
		public static void GiveRandomEquipmentXoro(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(
				x => x.MatchCallOrCallvirt<Run>("get_instance"),
				x => x.MatchLdfld<Run>(nameof(Run.availableEquipmentDropList)),
				x => x.MatchCallOrCallvirt<Xoroshiro128Plus>(nameof(Xoroshiro128Plus.NextElementUniform))
			);
			c.Emit(OpCodes.Dup);
			c.Index += 3;
			c.EmitDelegate<Func<Xoroshiro128Plus, PickupIndex, PickupIndex>>((rng, index) => RandomPickup(rng, index, "randomEquipment"));
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(MultiShopController), nameof(MultiShopController.CreateTerminals))]
		public static void TerminalFix(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(
				x => x.MatchCallOrCallvirt<Run>("get_instance"),
				x => x.MatchLdfld<Run>(nameof(Run.availableEquipmentDropList)),
				x => x.MatchCallOrCallvirt<Xoroshiro128Plus>(nameof(Xoroshiro128Plus.NextElementUniform))
			);
			c.Emit(OpCodes.Dup);
			c.Index += 3;
			c.EmitDelegate<Func<Xoroshiro128Plus, PickupIndex, PickupIndex>>((rng, index) => RandomPickup(rng, index, "shop"));
		}

		private static PickupIndex RandomPickup(Xoroshiro128Plus rng, PickupIndex index, string type)
		{
			var selection = new WeightedSelection<PickupIndex>(1 + _typesToCheck.Count);
			selection.AddChoice(index, 1f);
			foreach (var info in _typesToCheck)
			{
				var chance = type switch
				{
					"shop" => info.shopChance,
					"randomEquipment" => info.dropChance,
					_ => 0f
				};
				selection.AddChoice(rng.NextElementUniform(EquipmentDropTables[info.type]), chance);
			}

			return selection.Evaluate(rng.nextNormalizedFloat);
		}
		[HarmonyILManipulator, HarmonyPatch(typeof(ScavBackpackBehavior), nameof(ScavBackpackBehavior.RollEquipment))]
		public static void ScavFix(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchCallOrCallvirt<ScavBackpackBehavior>(nameof(ScavBackpackBehavior.PickFromList)));
			c.EmitDelegate<Func<List<PickupIndex>, List<PickupIndex>>>(list =>
			{
				var selection = new WeightedSelection<List<PickupIndex>>(1 + _typesToCheck.Count);
				selection.AddChoice(list, 1f);
				foreach (var info in _typesToCheck)
				{
					selection.AddChoice(EquipmentDropTables[info.type], info.scavChance);
				}
				return selection.Evaluate(Run.instance.treasureRng.nextNormalizedFloat);
			});
		}
		
		[HarmonyILManipulator, HarmonyPatch(typeof(ShrineChanceBehavior), nameof(ShrineChanceBehavior.AddShrineStack))]
		public static void FixShrineFallback(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(MoveType.After, x => x.MatchLdcI4(8));
			c.EmitDelegate<Func<int, int>>(i => i + _typesToCheck.Count);
			c.GotoNext(MoveType.After,
				x => x.MatchDup(),
				x => x.MatchLdloc(out _),
				x => x.MatchLdarg(out _),
				x => x.MatchLdfld<ShrineChanceBehavior>(nameof(ShrineChanceBehavior.equipmentWeight)),
				x=> (x.OpCode == OpCodes.Call || x.OpCode == OpCodes.Callvirt) && (x.Operand as MethodReference)?.Name == nameof(WeightedSelection<PickupIndex>.AddChoice)
			);
			c.Emit(OpCodes.Dup);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Action<WeightedSelection<PickupIndex>, ShrineChanceBehavior>>((selection, behaviour) =>
			{
				foreach (var info in _typesToCheck)
				{
					selection.AddChoice(behaviour.rng.NextElementUniform(EquipmentDropTables[info.type]), info.shrineChance);
				}
			});
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(Run), nameof(Run.EnableEquipmentDrop)), HarmonyPatch(typeof(Run), nameof(Run.DisableEquipmentDrop))]
		public static void EnableDisableEquipment(ILContext il)
		{
			var c = new ILCursor(il);
			var pickup = -1;
			c.GotoNext(x => x.MatchLdloc(out pickup), x => x.MatchCallOrCallvirt(typeof(PickupCatalog), nameof(PickupCatalog.GetPickupDef)), x => x.MatchLdfld<PickupDef>(nameof(PickupDef.isLunar)));
			c.GotoNext(MoveType.After,
				x => x.MatchLdfld<Run>(nameof(Run.availableEquipmentDropList))
			);
			c.Emit(OpCodes.Ldloc, pickup);
			c.EmitDelegate<Func<List<PickupIndex>, PickupIndex, List<PickupIndex>>>((original, index) =>
			{
				var equipment = EquipmentCatalog.GetEquipmentDef(PickupCatalog.GetPickupDef(index)!.equipmentIndex);
				var type = equipment.GetType();
				return _typesToCheck.Any(info => info.type == type) ? EquipmentDropTables[type] : original;
			});
		}
	
		[HarmonyILManipulator, HarmonyPatch(typeof(Run), nameof(Run.BuildDropTable))]
		public static void DisableRegularPool(ILContext il)
		{
			var c = new ILCursor(il);
			c.EmitDelegate<Action>(() =>
			{
				foreach (var info in _typesToCheck.Where(info => EquipmentDropTables.ContainsKey(info.type)))
				{
					EquipmentDropTables[info.type].Clear();
				}
			});
			c.GotoNext( MoveType.After,
				x => x.MatchLdloc(out _),
				x => x.MatchCallOrCallvirt(typeof(EquipmentCatalog),nameof(EquipmentCatalog.GetEquipmentDef)),
				x => x.MatchStloc(out _)
			);
			var where = c.Index;
			ILLabel? jump = null;
			c.GotoNext(x => x.MatchBrfalse(out jump));
			c.Index = where - 1;
			c.Emit(OpCodes.Dup);
			c.Index++;
			c.EmitDelegate<Func<EquipmentDef, bool>>((def) =>
			{
				var typ = def.GetType();
				if (_typesToCheck.All(x => x.type != typ)) return false;
				
				if (!EquipmentDropTables.ContainsKey(typ))
				{
					EquipmentDropTables.Add(typ, new List<PickupIndex>());
				}
				EquipmentDropTables[typ].Add(PickupCatalog.FindPickupIndex(def.equipmentIndex));
				return true;
			});
			c.Emit(OpCodes.Brtrue, jump);
		}
		#endregion

		#region Void Equipment
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
		#endregion
	}
}