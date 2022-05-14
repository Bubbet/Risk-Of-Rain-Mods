using BubbetsItems.Helpers;
using HarmonyLib;
using RoR2;

namespace BubbetsItems.Items.BarrierItems
{
	public class BoneVisor : ItemBase
	{
		private static BuffDef? _buffDef;
		private static BuffDef? BuffDef => _buffDef ??= BubbetsItemsPlugin.ContentPack.buffDefs.Find("BuffDefBoneVisor");

		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("BONEVISOR_NAME","Bone Visor");
			AddToken("BONEVISOR_DESC", "Killing an enemy " + "multiplies barrier decay ".Style(StyleEnum.Heal) + "by " + "{1:0%}".Style(StyleEnum.Heal) + ", lasting for " + "{0} ".Style(StyleEnum.Utility) + "seconds.");
			AddToken("BONEVISOR_DESC_SIMPLE", "Killing an enemy " + "multiplies barrier decay ".Style(StyleEnum.Heal) + "by " + "95% ".Style(StyleEnum.Heal) + "(-5% per stack)".Style(StyleEnum.Stack) + ", lasting for " + "3 ".Style(StyleEnum.Utility) + "(+2 per stack)".Style(StyleEnum.Stack) + "seconds.");
			SimpleDescriptionToken = "BONEVISOR_DESC_SIMPLE";
			AddToken("BONEVISOR_PICKUP", "Killing an enemy grants a buff that slows barrier decay temporarily.");
			AddToken("BONEVISOR_LORE","Was there a bone tribe somewhere lost here? I've found this near an ancient ruins on Mars and still haven't figured out the origins to it yet.");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("1 + [a] * 2", "Buff Duration");
			AddScalingFunction("1 - [b] * 0.05", "Barrier Decay", desc:"[a] = item count; [b] = buff amount; [m] = max barrier");
		}

		public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
		{
			scalingInfos[1].WorkingContext.b = 1;
			return base.GetFormattedDescription(inventory, token, forceHideExtended);
		}

		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onCharacterDeathGlobal += OnDeath;
		}

		protected override void DestroyBehaviours()
		{
			base.MakeBehaviours();
			GlobalEventManager.onCharacterDeathGlobal -= OnDeath;
		}

		private void OnDeath(DamageReport obj)
		{
			var body = obj.attackerBody;
			if (!body) return;
			var inv = body.inventory;
			if (!inv) return;
			var amount = inv.GetItemCount(ItemDef);
			if (amount <= 0) return;
			body.AddTimedBuff(BuffDef, scalingInfos[0].ScalingFunction(amount));
		}
		
		[HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
		public static void FixBarrier(CharacterBody __instance)
		{
			var inv = __instance.inventory;
			if (!inv) return;
			var instance = GetInstance<BoneVisor>();
			if (instance == null) return;
			var count = inv.GetItemCount(instance.ItemDef);
			if (count <= 0) return;
			var info = instance.scalingInfos[1];
			info.WorkingContext.b = __instance.GetBuffCount(BuffDef);
			info.WorkingContext.m = __instance.maxBarrier;
			__instance.barrierDecayRate *= info.ScalingFunction(count);
		}
	}
}