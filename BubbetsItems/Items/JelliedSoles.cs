using System;
using BubbetsItems.Helpers;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Audio;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BubbetsItems.Items
{
	public class JelliedSolesBehavior : MonoBehaviour
	{
		public float storedDamage;
	}
	public class JelliedSoles : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("JELLIEDSOLES_NAME", "Jellied Soles");
			AddToken("JELLIEDSOLES_DESC", "Reduces " + "fall damage ".Style(StyleEnum.Utility) + "by " + "{0:0%}".Style(StyleEnum.Utility) + ". Converts that reduction ({1}) into your next attack.");
			AddToken("JELLIEDSOLES_DESC_SIMPLE", "Reduces " + "fall damage ".Style(StyleEnum.Utility) + "by " + "15% ".Style(StyleEnum.Utility) + "(+15% per stack) ".Style(StyleEnum.Stack) + "and converts " + "100% ".Style(StyleEnum.Utility) + "(+100% per stack) ".Style(StyleEnum.Stack) + "removed damage to your next attack. Scales by level.");
			SimpleDescriptionToken = "JELLIEDSOLES_DESC_SIMPLE";
			AddToken("JELLIEDSOLES_PICKUP", "Reduces " + "fall damage.".Style(StyleEnum.Utility) + " Converts that reduction into your next attack.");
			AddToken("JELLIEDSOLES_LORE", "");
		}
		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("Min([a] * 0.15, 1)", "Reduction");
			AddScalingFunction("[s] * [d] * [a]", "Damage Add", desc: "[a] = amount; [s] = stored damage; [d] = level scaled damage over base damage; [h] = the enemies combined health before the damage", oldDefault: "[s] * [d] * [a] * 0.1");
		}

		public override string GetFormattedDescription(Inventory? inventory, string? token = null, bool forceHideExtended = false)
		{
			if (!inventory) return base.GetFormattedDescription(inventory, token, forceHideExtended);
			var body = inventory.GetComponent<CharacterMaster>().GetBody();
			if (!body) return base.GetFormattedDescription(inventory, token, forceHideExtended);
			var info = scalingInfos[1].WorkingContext;
			var beh = inventory.GetComponent<JelliedSolesBehavior>();
			if (beh)
				info.s = beh.storedDamage;
			else
				info.s = 0;
			info.d = body.damage / body.baseDamage;
			return base.GetFormattedDescription(inventory, token, forceHideExtended);
		}
		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			Inventory.onInventoryChangedGlobal += OnInvChanged;
			ModdedDamageColors.ReserveColor(new Color(1, 0.4f, 0), out index);
		}

		protected override void DestroyBehaviours()
		{
			base.DestroyBehaviours();
			Inventory.onInventoryChangedGlobal -= OnInvChanged;
		}

		private void OnInvChanged(Inventory obj)
		{
			var comp = obj.GetComponent<JelliedSolesBehavior>(); 
			if (obj.GetItemCount(ItemDef) > 0)
			{
				obj.gameObject.AddComponent<JelliedSolesBehavior>();
			}
			else if(comp)
			{
				Object.Destroy(comp);
			}
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(GlobalEventManager), nameof(GlobalEventManager.OnCharacterHitGroundServer))]
		public static void NullifyFallDamage(ILContext il)
		{
			var c = new ILCursor(il);
			//var h = -1;
			//var d = -1;

			c.GotoNext(MoveType.After,
				x => x.MatchLdcI4(0),
				x => x.MatchStloc(out _)
			);
			
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldarg_2);
			c.Emit(OpCodes.Ldloc_0); // weak ass knees
			c.EmitDelegate<Action<CharacterBody, Vector3, bool>>(CollectDamage);
			
			c.GotoNext(x => x.MatchCallvirt<CharacterBody>("get_footPosition"));
			c.GotoNext(MoveType.Before, x => x.MatchLdloc(out _), x => x.MatchLdloc(out _), x => x.MatchCallvirt<HealthComponent>(nameof(HealthComponent.TakeDamage)));
			c.Index++;
			c.Emit(OpCodes.Dup);
			c.Index++;
			//c.Emit(OpCodes.Ldloc, h);
			//c.Emit(OpCodes.Ldloc, d); // Maybe move this up and before the check for ignores fall damage
			c.EmitDelegate<Func<HealthComponent, DamageInfo, DamageInfo>>(UpdateDamage);
		}

		
		// body.damage/body.basedamage * storedDamage
		[HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
		public static void IlTakeDamage(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(MoveType.Before, x => x.OpCode == OpCodes.Ldsfld && (x.Operand as FieldReference)?.Name == nameof(RoR2Content.Items.NearbyDamageBonus),
				x => x.MatchCallOrCallvirt(out _),
				x => x.MatchStloc(out _));
			var where = c.Index;
			int num2 = -1;
			c.GotoNext(x => x.MatchLdloc(out num2),
				x => x.MatchLdcR4(1f),
				x => x.MatchLdloc(out _));
			c.Index = where;
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc_1); // Body; 0 is master
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldloc, num2);
			c.EmitDelegate<Func<HealthComponent, CharacterBody, DamageInfo, float, float>>((hc, body, damageInfo, amount) =>
			{
				var inv = body.inventory;
				if (!inv) return amount;
				var instance = GetInstance<JelliedSoles>();
				var count = inv.GetItemCount(instance.ItemDef);
				if (count <= 0) return amount;
				var behavior = inv.GetComponent<JelliedSolesBehavior>(); // potential future incompat with holydll
				if (behavior.storedDamage <= 0) return amount;
				
				//[s] * [d] * [a] * 0.1
				//Min([s] * [d] * [a] * 0.1, Max([h] - [c], 0))/([d] * [a] * 0.1)

				// Initial setup
				var info = instance.scalingInfos[1];
				var context = info.WorkingContext;
				context.d = body.damage / body.baseDamage;
				context.h = hc.combinedHealth;
				
				//Solving for maximum damage.
				context.s = behavior.storedDamage;
				var a = Mathf.Min(info.ScalingFunction(count), Mathf.Max(0,hc.combinedHealth - damageInfo.damage));
				// Divide by the scaling amount
				context.s = 1f;
				a /= info.ScalingFunction(count);

				// Get final damage amount
				context.s = a;
				amount += info.ScalingFunction(count);
				
				behavior.storedDamage = Mathf.Max(0, behavior.storedDamage - a);
				damageInfo.damageColorIndex = index;
				EntitySoundManager.EmitSoundServer(hitSound.index, body.gameObject);

				return amount;
			});
			c.Emit(OpCodes.Stloc, num2);
		}

		private static NetworkSoundEventDef? _hitSound;
		public static NetworkSoundEventDef hitSound => (_hitSound ??= BubbetsItemsPlugin.ContentPack.networkSoundEventDefs.Find("JelliedSolesHitSound"))!;
		private static NetworkSoundEventDef? _hitGroundSound;
		public static DamageColorIndex index;
		public static NetworkSoundEventDef hitGroundSound => (_hitGroundSound ??= BubbetsItemsPlugin.ContentPack.networkSoundEventDefs.Find("JelliedSolesHitGround"))!;

		private static void CollectDamage(CharacterBody body, Vector3 impactVelocity, bool weakAssKnees)
		{
			var damage = Mathf.Max(Mathf.Abs(impactVelocity.y) - (body.jumpPower + 20f), 0f);
			if (damage <= 0f) return;
			var inv = body.inventory;
			if (!inv) return;
			var instance = GetInstance<JelliedSoles>();
			var count = inv.GetItemCount(instance.ItemDef);
			if (count <= 0) return;
			var behavior = inv.GetComponent<JelliedSolesBehavior>();
			
			damage /= 60f;
			damage *= body.maxHealth;
			if (weakAssKnees || body.teamComponent.teamIndex == TeamIndex.Player && Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse3)
				damage *= 2f;
			
			var frac = instance.scalingInfos[0].ScalingFunction(count);
			behavior.storedDamage += damage * frac;
			EntitySoundManager.EmitSoundServer(hitGroundSound.index, body.gameObject);
		}
		private static DamageInfo UpdateDamage(HealthComponent component, DamageInfo info)
		{
			var inv = component.body.inventory;
			if (!inv) return info;
			var instance = GetInstance<JelliedSoles>();
			var count = inv.GetItemCount(instance.ItemDef);
			if (count <= 0) return info;
			var frac = instance.scalingInfos[0].ScalingFunction(count);
			info.damage *= 1f - frac;
			return info;
		}

		
	}
}