using System.Collections.Generic;
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
			AddToken(name + "_DESC", "");
			AddToken(name + "_DESC_SIMPLE", "Decrease skill cooldowns by 1%(+1% per stack) of gold gained. Stop all gold gain temporarily for 5 (+5 per stack) seconds. Corrupts all Brittle Crowns.");
			AddToken(name + "_PICKUP", "Reduce skill cooldowns from gold gained… BUT stop gold gain on hit. Corrupts all Brittle Crowns.");
			AddToken(name + "_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[m] * 0.01 * [a]", "Recharge Reduction", "[a] = item count; [m] = money earned");
			AddScalingFunction("5 * [a]", "No Gold Debuff Duration");
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