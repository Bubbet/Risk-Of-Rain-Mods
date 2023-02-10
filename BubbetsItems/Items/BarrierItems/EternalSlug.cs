using BubbetsItems.Helpers;
using System;
using BubbetsItems.Components;
using BubbetsItems.Behaviours;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BubbetsItems.Items.BarrierItems
{
	public class EternalSlug : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("ETERNALSLUG_NAME","Eternal Slug");
			//AddToken("ETERNALSLUG_DESC", "Stops " + "temporary barrier ".Style(StyleEnum.Heal) + "from decaying naturally past " + "{0:0%}".Style(StyleEnum.Heal) + ".");
			//AddToken("ETERNALSLUG_DESC_SIMPLE", "Stops " + "temporary barrier ".Style(StyleEnum.Heal) + "from decaying naturally past " + "36% ".Style(StyleEnum.Heal) + "(stacks logarithmically, caps at 80%)".Style(StyleEnum.Stack) + ".");
			AddToken("ETERNALSLUG_DESC", "Prevents temporary barrier decay at a low amount, and reduces barrier decay."); //
			AddToken("ETERNALSLUG_DESC_SIMPLE", "Prevents temporary barrier decay at 15% maxiumum health. Reduce barrier decay by 0 % (+10 % per stack)"); //
			SimpleDescriptionToken = "ETERNALSLUG_DESC_SIMPLE";
			//AddToken("ETERNALSLUG_PICKUP", "Stops barrier decay at a certain point.");
			AddToken("ETERNALSLUG_PICKUP", "Prevents temporary barrier decay at a low amount, and reduces barrier decay."); //
			AddToken("ETERNALSLUG_LORE", @"As I stand here, gazing upon this mysterious world enveloped in a foreign substance, I am struck by its remarkable resemblance to the earthly material known as amber. Yet upon closer examination, it is clear that the composition of this encasing substance is vastly different, emitting an otherworldly scent that can only be described as a fusion of strawberries and the unknown. As I meticulously study this planet and its inhabitants, I have discovered a most peculiar specimen - a slug-like creature entrapped within a shell of this strange substance. In the interest of furthering our understanding of this strange world, I have chosen to designate this fascinating organism as 'Fern,' and will diligently document my findings in the hopes of unlocking the secrets of this extraterrestrial realm. - Scientist G.");
		}

		/*protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("(0.8 - Pow(0.7, [a] + 1.3)) * [b]", "Minimum Barrier", desc: "[a] = item count; [h] = current health; [b] = full barrier; [p] = previous minimum, probably 0");
		}*/	
		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("((0.111 * [a])/((0.111 * [a])+1))", "Barrier Decay Scale", desc: "[a] = item count;", "(0.8 - Pow(0.7, [a] + 1.3)) * [b]");
			AddScalingFunction("0.15", "Barrier Stop Percentage");
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

		public static float EditMinimum(float previous, HealthComponent hc)
		{
			var body = hc.body;
			if (!body) return previous;
			if (body.GetBuffCount(BoneVisor.BuffDef) > 0) return previous;
			var inv = body.inventory;
			if (!inv) return previous;
			var inst = GetInstance<EternalSlug>();
			if (inst == null) return previous;
			var amount = inv.GetItemCount(inst.ItemDef);
			if (amount <= 0) return previous;
			var info = inst.scalingInfos[1];
			info.WorkingContext.h = hc.health;
			info.WorkingContext.b = hc.fullBarrier;
			info.WorkingContext.p = previous;
			return info.ScalingFunction(amount);
		}
		
		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			ExtraHealthBarSegments.AddType<SlugData>();
			CommonBodyPatches.CollectExtraStats += GetBarrierDecay;
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			CommonBodyPatches.CollectExtraStats -= GetBarrierDecay;
		}

		private void GetBarrierDecay(ref CommonBodyPatches.ExtraStats obj)
		{
			var count = obj.inventory.GetItemCount(ItemDef);
			if (count <= 0) return;
			obj.barrierDecayMult += scalingInfos[0].ScalingFunction(count);
		}

		public class SlugData : ExtraHealthBarSegments.BarData
		{
			private bool enabled;
			private float barPos;

			public override HealthBarStyle.BarStyle GetStyle()
			{
				var style = bar.style.barrierBarStyle;
				style.sizeDelta = bar.style.lowHealthOverStyle.sizeDelta;
				return style;
			}

			public override void CheckInventory(ref HealthBar.BarInfo info, Inventory inv, CharacterBody characterBody,
				HealthComponent healthComponent)
			{
				base.CheckInventory(ref info, inv, characterBody, healthComponent);
				
				var inst = GetInstance<EternalSlug>();
				if (inst == null) return;
				var amount = inv.GetItemCount(inst.ItemDef);
				if (amount <= 0)
				{
					enabled = false;
					return;
				}
				var sinfo = inst.scalingInfos[1];
				sinfo.WorkingContext.h = healthComponent.health;
				sinfo.WorkingContext.b = healthComponent.fullBarrier;
				sinfo.WorkingContext.p = 0f;
				barPos = sinfo.ScalingFunction(amount)/* / healthComponent.fullCombinedHealth*/;
				enabled = true;
			}

			public override void UpdateInfo(ref HealthBar.BarInfo info, HealthComponent.HealthBarValues healthBarValues)
			{
				info.enabled = enabled;
				var curse = 1f - healthBarValues.curseFraction;
				info.normalizedXMin = barPos * curse;
				info.normalizedXMax = barPos * curse + 0.005f;
				base.UpdateInfo(ref info, healthBarValues);
			}
		}
	}
}