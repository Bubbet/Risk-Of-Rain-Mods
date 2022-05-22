using System.Collections.Generic;
using BubbetsItems.Helpers;
using BubbetsItems.ItemBehaviors;
using HarmonyLib;
using RoR2;

namespace BubbetsItems.Items.VoidLunar
{
	public class Circlet : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			var name = GetType().Name.ToUpper();
			SimpleDescriptionToken = name + "_DESC_SIMPLE";
			AddToken(name + "_NAME", "Deluged Circlet");
			var convert = "Corrupts all Brittle Crowns.".Style(StyleEnum.Void);
			AddToken(name + "_DESC", "Decrease " + "skill cooldowns by {0:0%}".Style(StyleEnum.Utility) + " of gold gained. "+"Stop all gold gain for {1} seconds upon being hit. ".Style(StyleEnum.Health) + convert);
			AddToken(name + "_DESC_SIMPLE", "Decrease " + "skill cooldowns by 1% ".Style(StyleEnum.Utility) +"(+1% per stack)".Style(StyleEnum.Stack) + " of gold gained. " + "Temporarily stops any gold from being gained for 5 ".Style(StyleEnum.Health) +"(+5 per stack)".Style(StyleEnum.Stack) +" seconds upon being hit ".Style(StyleEnum.Health) + ". " + convert);
			AddToken(name + "_PICKUP", "Reduce "+"skill cooldowns".Style(StyleEnum.Utility) +" from gold gained… "+ "BUT stop gold gain on hit. ".Style(StyleEnum.Health) + convert);
			AddToken(name + "_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[m] * 0.01 * [a]", "Recharge Reduction", "[a] = item count; [m] = money earned");
			AddScalingFunction("5 * [a]", "No Gold Debuff Duration");
		}

		public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
		{
			scalingInfos[0].WorkingContext.m = 1;
			return base.GetFormattedDescription(inventory, token, forceHideExtended);
		}

		protected override void FillVoidConversions(List<ItemDef.Pair> pairs)
		{
			base.FillVoidConversions(pairs);
			AddVoidPairing(nameof(RoR2Content.Items.GoldOnHit));
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onServerDamageDealt += DamageDealt;
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			GlobalEventManager.onServerDamageDealt -= DamageDealt;
		}
		
		private void DamageDealt(DamageReport obj)
		{
			var body = obj.victimBody;
			if (!body) return;
			var inv = body.inventory;
			if (!inv) return;
			var amount = inv.GetItemCount(ItemDef);
			if (amount <= 0) return;
			var info = scalingInfos[1];
			body.AddTimedBuff(BuffDef, info.ScalingFunction(amount));
		}


		private static BuffDef? _buffDef;
		private static BuffDef? BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefCirclet");

		[HarmonyPrefix, HarmonyPatch(typeof(CharacterMaster), nameof(CharacterMaster.money), MethodType.Setter)]
		public static bool DisableMoney(CharacterMaster __instance, uint value)
		{
			var inv = __instance.inventory;
			if (!inv) return true;
			var inst = GetInstance<Circlet>();
			var amount = inv.GetItemCount(inst.ItemDef);
			if (amount <= 0) return true;
			var change = (int)value - (int)__instance.money;
			if (change < 0) return true;
			var body = __instance.GetBody();
			if (!body) return true;
			if (body.HasBuff(BuffDef)) return false;

			var info = inst.scalingInfos[0];
			info.WorkingContext.m = change;
			var reduction = info.ScalingFunction(amount);

			var locator = body.skillLocator;
			locator.primary.rechargeStopwatch += reduction;
			locator.secondary.rechargeStopwatch += reduction;
			locator.utility.rechargeStopwatch += reduction;
			locator.special.rechargeStopwatch += reduction;
			
			return true;
		}
	}
}