using System.Linq;
using BubbetsItems.Helpers;
using HarmonyLib;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;

namespace BubbetsItems.Items.BarrierItems
{
	public class GemCarapace : ItemBase
	{
		//onhit get stacking buff and also timed buff(hidden) that refreshes
		private static BuffDef? _buffDefStacking;
		public static BuffDef? BuffDefStacking => _buffDefStacking ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefGemCarapaceStack");
		private static BuffDef? _buffDefRefresh;
		public static BuffDef? BuffDefRefresh => _buffDefRefresh ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefGemCarapaceRefresh");
		protected override void FillDefsFromSerializableCP(SerializableContentPack serializableContentPack)
		{
			base.FillDefsFromSerializableCP(serializableContentPack);
			// yeahh code based content because TK keeps fucking freezing
			var buff = ScriptableObject.CreateInstance<BuffDef>();
			buff.isCooldown = true;
			buff.name = "BuffDefGemCarapaceRefresh";
			buff.buffColor = new Color(r: 1, g: 0.80784315f, b: 0, a: 1);
			buff.iconSprite = BubbetsItemsPlugin.AssetBundle.LoadAsset<Sprite>("CarapaceBuff");
			
			var buff2 = ScriptableObject.CreateInstance<BuffDef>();
			buff2.canStack = true;
			buff2.name = "BuffDefGemCarapaceStack";
			buff2.buffColor = new Color(r: 1, g: 0.80784315f, b: 0, a: 1);
			buff2.iconSprite = BubbetsItemsPlugin.AssetBundle.LoadAsset<Sprite>("CarapaceBuff");
			serializableContentPack.buffDefs = serializableContentPack.buffDefs.AddItem(buff).AddItem(buff2).ToArray();
		}

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("GEMCARAPACE_NAME", "Gem Carapace");
			//AddToken("GEMCARAPACE_DESC", "{1} seconds after getting hurt, gain a " + "{0:0%} temporary barrier".Style(StyleEnum.Heal) + ". Triggers up to {2} times.");
			//AddToken("/GEMCARAPACE_DESC_SIMPLE", "Gain an " + "10% temporary barrier ".Style(StyleEnum.Heal) + "after 1 " + "(+0.75 per stack) ".Style(StyleEnum.Stack) + "seconds of taking damage. " + "Triggers up to 1 " + "(+1 per stack)".Style(StyleEnum.Stack) + " times.");
			AddToken("GEMCARAPACE_DESC", "Grants temporary barrier from all attacks. Recharges over time.");
			AddToken("/GEMCARAPACE_DESC_SIMPLE", "Grants a " + "temporary barrier ".Style(StyleEnum.Heal) + "for " + " {0:0%}".Style(StyleEnum.Heal) + " (+10% hyperbolically per stack)".Style(StyleEnum.Stack) + " of " + "maximum health ".Style(StyleEnum.Heal) + "from " + "incoming damage".Style(StyleEnum.Damage) + ". Recharges every " + "{1} ".Style(StyleEnum.Utility) + "seconds.");
			SimpleDescriptionToken = "GEMCARAPACE_DESC_SIMPLE";
			AddToken("GEMCARAPACE_PICKUP", "Receive a delayed temporary barrier after taking damage.");
			AddToken("GEMCARAPACE_LORE", "");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("(0.1[a] / (0.1[a] + 1))  * [m]", "Barrier Add", desc: "[a] = item count; [b] = buff stacks; [m] = maximum barrier", oldDefault: "(0.075 * [b] + 0.1) * [m]");
			AddScalingFunction("2", "Refresh Duration", oldDefault: "1");
			AddScalingFunction("1", "Max Buff Stacks", oldDefault: "[a]");
		}

		public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
		{
			var context = scalingInfos[0].WorkingContext;
			context.b = 1;
			context.m = 1;
			
			return base.GetFormattedDescription(inventory, token, forceHideExtended);
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onServerDamageDealt += OnHit;
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			GlobalEventManager.onServerDamageDealt -= OnHit;
		}

		private void OnHit(DamageReport obj)
		{
			if (!obj.victim) return;
			var body = obj.victim.body;
			if (!body) return;
			var inv = body.inventory;
			if (!inv) return;
			var amount = inv.GetItemCount(ItemDef);
			if (amount <= 0) return;
			body.AddTimedBuff(BuffDefRefresh, scalingInfos[1].ScalingFunction(amount));
			if (body.GetBuffCount(BuffDefStacking) < Mathf.FloorToInt(scalingInfos[2].ScalingFunction(amount)))
				body.AddBuff(BuffDefStacking);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.OnBuffFinalStackLost))]
		public static void StackLost(CharacterBody __instance, BuffDef buffDef)
		{
			if (buffDef != BuffDefRefresh) return;
			var inv = __instance.inventory;
			if (!inv) return;
			var inst = GetInstance<GemCarapace>();
			if (inst == null) return;
			var count = inv.GetItemCount(inst.ItemDef);
			
			var info = inst.scalingInfos[0];
			info.WorkingContext.b = __instance.GetBuffCount(BuffDefStacking);
			info.WorkingContext.m = __instance.maxBarrier;

			__instance.healthComponent.AddBarrier(info.ScalingFunction(count));
			__instance.SetBuffCount(BuffDefStacking!.buffIndex, 0);
		}
	}
}