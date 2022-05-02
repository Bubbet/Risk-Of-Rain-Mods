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
			AddToken("BONEVISOR_DESC","Killing enemies stacks a buff that lasts for {0} seconds, and multiplies barrier decay by {1:0%}.");
			AddToken("BONEVISOR_PICKUP","");
			AddToken("BONEVISOR_LORE","");
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