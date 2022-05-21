using System;
using BubbetsItems.Helpers;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Items.VoidLunar
{
	public class Decent : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			var name = GetType().Name.ToUpper();
			SimpleDescriptionToken = name + "_DESC_SIMPLE";
			AddToken(name + "_NAME", "Deep Decent");
			var convert = "Corrupts all Gestures of the Drowned.".Style(StyleEnum.Void);
			AddToken(name + "_DESC", "Equipment effects " + "trigger an additional {0} times".Style(StyleEnum.Utility) + " per use. " + "Increases equipment cooldown by {1:0%}. ".Style(StyleEnum.Health) + convert);
			AddToken(name + "_DESC_SIMPLE", "Equipment effects will " + "trigger 1 ".Style(StyleEnum.Utility) + "(+1 per stack)".Style(StyleEnum.Stack) + " more time on use.".Style(StyleEnum.Utility) + " Increases Equipment cooldown by 50%".Style(StyleEnum.Health) + " (+15% per stack)".Style(StyleEnum.Stack) + "." + convert);
			AddToken(name + "_PICKUP", "Equipments "+"trigger more,".Style(StyleEnum.Utility) + " increases equipment cooldown. ".Style(StyleEnum.Health) + convert);
			AddToken(name + "_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[a]", "Equipment Activation Amount");
			AddScalingFunction("0.35 + 0.15 * [a]", "Equipment Cooldown");
		}

		[HarmonyPostfix, HarmonyPatch(typeof(Inventory), nameof(Inventory.CalculateEquipmentCooldownScale))]
		public static void ReduceCooldown(Inventory __instance, ref float __result)
		{
			var inst = GetInstance<Decent>();
			var amount = __instance.GetItemCount(inst.ItemDef);
			if (amount <= 0) return;
			__result *= 1f + inst.scalingInfos[1].ScalingFunction(amount);
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(EquipmentSlot), nameof(EquipmentSlot.ExecuteIfReady))]
		public static void DoDouble(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchCallOrCallvirt<EquipmentSlot>(nameof(EquipmentSlot.Execute)));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Action<EquipmentSlot>>(DuplicateExecute);
		}

		private static void DuplicateExecute(EquipmentSlot slot)
		{
			var inv = slot.inventory;
			if (!inv) return;
			var inst = GetInstance<Decent>();
			var amount = inv.GetItemCount(inst.ItemDef);
			if (amount <= 0) return;
			for (var i = 0; i < Mathf.FloorToInt(inst.scalingInfos[0].ScalingFunction(amount)); i++)
			{
				slot.Execute();
			}
		}
	}
}