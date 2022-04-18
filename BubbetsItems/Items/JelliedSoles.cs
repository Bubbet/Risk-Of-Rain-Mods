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
			AddToken("JELLIEDSOLES_PICKUP", "Reduces " + "fall damage.".Style(StyleEnum.Utility) + " Converts that reduction into your next attack.");
			AddToken("JELLIEDSOLES_DESC", "Reduces " + "fall damage ".Style(StyleEnum.Utility) + "by " + "{0:0%}".Style(StyleEnum.Utility) + ". Converts that reduction ({1}) into your next attack.");
			AddToken("JELLIEDSOLES_LORE", "");
		}
		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("Min([a] * 0.15, 1)", "Reduction");
			AddScalingFunction("[s] * [d]", "Damage Add", desc: "[a] = amount; [s] = stored damage; [d] = level scaled damage over base damage");
		}

		public override string GetFormattedDescription(Inventory inventory, string? token = null)
		{
			if (!inventory) return base.GetFormattedDescription(inventory, token);
			var body = inventory.GetComponent<CharacterMaster>().GetBody();
			if (!body) return base.GetFormattedDescription(inventory, token);
			var info = scalingInfos[1].WorkingContext;
			var beh = inventory.GetComponent<JelliedSolesBehavior>();
			if (beh)
				info.s = beh.storedDamage;
			else
				info.s = 0;
			info.d = body.damage / body.baseDamage;
			return base.GetFormattedDescription(inventory, token);
		}
		protected override void MakeBehaviours()
		{
			base.MakeBehaviours();
			Inventory.onInventoryChangedGlobal += OnInvChanged;
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
				var behavior = inv.GetComponent<JelliedSolesBehavior>();
				if (behavior.storedDamage <= 0) return amount;
				
				var info = instance.scalingInfos[1];
				
				info.WorkingContext.d = body.damage / body.baseDamage;
				info.WorkingContext.s = behavior.storedDamage;
				var x = info.ScalingFunction(count);
				amount += x;
				behavior.storedDamage = Mathf.Max(0, behavior.storedDamage - x);
				damageInfo.damageColorIndex = (DamageColorIndex) 145;
				EntitySoundManager.EmitSoundServer(hitSound.index, body.gameObject);

				return amount;
			});
			c.Emit(OpCodes.Stloc, num2);
		}

		private static NetworkSoundEventDef? _hitSound;
		public static NetworkSoundEventDef hitSound => (_hitSound ??= BubbetsItemsPlugin.ContentPack.networkSoundEventDefs.Find("JelliedSolesHitSound"))!;
		private static NetworkSoundEventDef? _hitGroundSound;
		public static NetworkSoundEventDef hitGroundSound => (_hitGroundSound ??= BubbetsItemsPlugin.ContentPack.networkSoundEventDefs.Find("JelliedSolesHitGround"))!;

		private static DamageInfo UpdateDamage(HealthComponent component, DamageInfo info)
		{
			var inv = component.body.inventory;
			if (!inv) return info;
			var instance = GetInstance<JelliedSoles>();
			var count = inv.GetItemCount(instance.ItemDef);
			if (count <= 0) return info;
			var behavior = inv.GetComponent<JelliedSolesBehavior>();
			var frac = instance.scalingInfos[0].ScalingFunction(count);
			behavior.storedDamage += info.damage * frac;
			info.damage *= 1f - frac;
			EntitySoundManager.EmitSoundServer(hitGroundSound.index, component.gameObject);
			return info;
		}

		
	}
}