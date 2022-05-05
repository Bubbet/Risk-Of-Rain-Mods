using BubbetsItems.Helpers;
using System;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

namespace BubbetsItems.Items.BarrierItems
{
	public class EternalSlug : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("ETERNALSLUG_NAME","Eternal Slug");
			AddToken("ETERNALSLUG_DESC", "Stops" + "temporary barrier".Style(StyleEnum.Health) + "from decaying naturally past" + "{0:0%}".Style(StyleEnum.Health) + ".");
			AddToken("ETERNALSLUG_PICKUP", "Stops barrier decay at a certain point.");
			AddToken("ETERNALSLUG_LORE",@"Fascinatingly, this alien species appears to have been perfectly preserved in an extra-terrestrial substance, similar to amber, yet with the smell of…. strawberries? Little Fern here certainly will live forever in this amber.”
	- Doctor Jyemo of Archeology, Researcher aboard the UES Contact Light");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("(0.8 - Pow(0.7, [a] + 1.3)) * [b]", "Minimum Barrier", desc: "[a] = item count; [h] = current health; [b] = full barrier; [p] = previous minimum, probably 0");
		}

		public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
		{
			scalingInfos[0].WorkingContext.b = 1;
			return base.GetFormattedDescription(inventory, token, forceHideExtended);
		}


		[HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.ServerFixedUpdate))]
		public static void EditMinimumBarrier(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(
				x => x.MatchCallOrCallvirt<HealthComponent>("set_Networkbarrier")
			);
			c.GotoPrev(x => x.MatchLdcR4(out _));
			c.GotoPrev(MoveType.After, x => x.MatchLdcR4(out _));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<float, HealthComponent, float>>(EditMinimum);
		}

		private static float EditMinimum(float previous, HealthComponent hc)
		{
			var body = hc.body;
			if (!body) return previous;
			var inv = body.inventory;
			if (!inv) return previous;
			var inst = GetInstance<EternalSlug>();
			if (inst == null) return previous;
			var amount = inv.GetItemCount(inst.ItemDef);
			if (amount <= 0) return previous;
			var info = inst.scalingInfos[0];
			info.WorkingContext.h = hc.health;
			info.WorkingContext.b = hc.fullBarrier;
			info.WorkingContext.p = previous;
			return info.ScalingFunction(amount);
		}
	}
}