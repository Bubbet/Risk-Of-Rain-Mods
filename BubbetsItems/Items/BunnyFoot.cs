using System;
using System.Reflection;
using BubbetsItems.Helpers;
using BubbetsItems.ItemBehaviors;
using EntityStates;
using EntityStates.Assassin2;
using EntityStates.Merc;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace BubbetsItems.Items
{
	public class BunnyFoot : ItemBase
	{
		protected override void MakeTokens()
		{
			base.MakeTokens();
			AddToken("BUNNYFOOT_NAME", "Bunny Foot");
			AddToken("BUNNYFOOT_DESC", "You gain the ability to bunny hop. Air control: {0}, Jump control: {3}");
			AddToken("BUNNYFOOT_DESC_SIMPLE", "Gain the ability to bunny hop, increasing air control by " + "150% ".Style(StyleEnum.Utility) + "(+150% per stack) ".Style(StyleEnum.Stack) + "and jump control by " + "50% ".Style(StyleEnum.Utility) + "(+50% per stack)".Style(StyleEnum.Stack) + ".");
			SimpleDescriptionToken = "BUNNYFOOT_DESC_SIMPLE";
			AddToken("BUNNYFOOT_PICKUP", "Your little feets start quivering.");
			AddToken("BUNNYFOOT_LORE", "haha source go brrrr\n\n\n\n\n\nIf you complain about this item being bad you're just outing yourself as bad at videogames.");
		}

		protected override void MakeConfigs()
		{
			base.MakeConfigs();
			AddScalingFunction("[a] * 1.5", "Air Control");
			AddScalingFunction("0.15", "On Ground Mercy");
			AddScalingFunction("1", "Jump velocity retention");
			AddScalingFunction("[a] * 0.5", "Jump Control");
			AddScalingFunction("3", "Auto Jump Requirement");
			AddScalingFunction("0.25", "Merc Dash Exit Mult");
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(ProjectileGrappleController.GripState), nameof(ProjectileGrappleController.GripState.FixedUpdateBehavior))]
		public static void FixGrapple(ILContext il)
		{
			// enable air control after grapple
			var c = new ILCursor(il);
			c.GotoNext(
				MoveType.After,
				x => x.MatchCall<Vector3>("op_Multiply"),
				x => x.MatchLdcI4(1),
				x => x.MatchLdcI4(1)
			);
			//c.Index--;
			//c.Remove();
			//c.Emit(OpCodes.Ldc_I4_0);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<bool, ProjectileGrappleController.GripState, bool>>((b, grip) => (!grip.owner.characterBody || grip.owner.characterBody.inventory.GetItemCount(GetInstance<BunnyFoot>().ItemDef) <= 0) && b);
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(Assaulter2), nameof(Assaulter2.OnEnter)), HarmonyPatch(typeof(Assaulter2), nameof(Assaulter2.OnExit)), HarmonyPatch(typeof(FocusedAssaultDash), nameof(FocusedAssaultDash.OnExit)), HarmonyPatch(typeof(EvisDash), nameof(EvisDash.OnExit))]//, HarmonyPatch(typeof(WhirlwindBase), nameof(WhirlwindBase.FixedUpdate))]
		public static void FixAssulter2Dash(ILContext il, MethodBase __originalMethod)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchStfld<CharacterMotor>("velocity"));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<Vector3, BaseSkillState, Vector3>>((vector3, assaulter2) =>
			{
				var count = assaulter2.characterBody.inventory.GetItemCount(GetInstance<BunnyFoot>()?.ItemDef);
				if (count <= 0) return vector3;
				if (__originalMethod.Name != "OnExit") return ((IPhysMotor) assaulter2.characterMotor).velocity;
				var outputVelocity = (Vector3) (__originalMethod.DeclaringType?.GetField("dashVector", (BindingFlags) (-1))?.GetValue(assaulter2) ?? Vector3.zero) * (float) (__originalMethod.DeclaringType?.GetField("speedCoefficient")?.GetValue(assaulter2) ?? 0f) * assaulter2.moveSpeedStat * GetInstance<BunnyFoot>().scalingInfos[5].ScalingFunction(count);
				var inputVelocity = ((IPhysMotor) assaulter2.characterMotor).velocity;
				return outputVelocity.normalized * Mathf.Sqrt(Mathf.Max(inputVelocity.sqrMagnitude, outputVelocity.sqrMagnitude));
			});
		}
		//[HarmonyILManipulator, HarmonyPatch(typeof(DashStrike), nameof(DashStrike.FixedUpdate))]
		public static void FixMercDash(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchCallOrCallvirt<CharacterMotor>("set_" + nameof(CharacterMotor.moveDirection)));
			var index = c.Index;
			c.GotoPrev(MoveType.After, x => x.MatchCallOrCallvirt<EntityState>("get_" + nameof(EntityState.characterMotor)));
			c.Emit(OpCodes.Dup);
			c.Index = index + 1; // for some reason this was emitting before the mult which is weird
			c.EmitDelegate<Func<CharacterMotor, Vector3, Vector3>>((motor, input) =>
			{
				if (motor.body.inventory.GetItemCount(GetInstance<BunnyFoot>()?.ItemDef) <= 0) return input;
				return input.normalized * Mathf.Sqrt(Mathf.Max(input.sqrMagnitude, motor.moveDirection.sqrMagnitude));
			});
			BubbetsItemsPlugin.Log.LogInfo(il);
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(GenericCharacterMain), nameof(GenericCharacterMain.ApplyJumpVelocity))]
		public static void FixJump(ILContext il)
		{
			// Clamp the jump speed add to not add speed when strafing over the speedlimit
			var c = new ILCursor(il);
			
			// if (vault)
			c.GotoNext(x => x.MatchStfld<CharacterMotor>("velocity"));
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate<Func<Vector3, CharacterBody, Vector3>>(DoJumpFix);
			c.Index++;
			
			// if (vault) else
			c.GotoNext(
				x => x.MatchStfld<CharacterMotor>("velocity")
			);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate<Func<Vector3, CharacterBody, Vector3>>(DoJumpFix);
		}

		public static Vector3 DoJumpFix(Vector3 vector, CharacterBody characterBody)
		{
			/*
			var horizontal = vector + Vector3.down * vector.y;
			var cmi = characterBody.characterMotor as IPhysMotor;
			var vhorizontal = cmi.velocity + Vector3.down * cmi.velocity.y;
			if (vhorizontal.sqrMagnitude > horizontal.sqrMagnitude) horizontal = vhorizontal;
			horizontal.y = vector.y;*/

			var bh = characterBody.GetComponent<BunnyFootBehavior>();
			if (!bh) return vector;

			var bunnyFoot = GetInstance<BunnyFoot>();
			var count = characterBody.inventory.GetItemCount(bunnyFoot.ItemDef);
			var grounded = true;

			var velocity = bh.hitGroundVelocity;
			var wishDir = vector.normalized;
			var wishSpeed = vector.magnitude;
			if (!characterBody.characterMotor.isGrounded)
			{
				//wishDir = velo.normalized;
				//wishSpeed = velo.magnitude;
				velocity = (characterBody.characterMotor as IPhysMotor).velocity;
				grounded = false;
			}

			var addvel = Accelerate(velocity, wishDir, wishSpeed,
				wishSpeed * bunnyFoot.scalingInfos[2].ScalingFunction(count),
				bunnyFoot.scalingInfos[3].ScalingFunction(count), 1f);

			addvel.y = vector.y;
			
			if (!grounded) return addvel;

			return Time.time - bh.hitGroundTime > bunnyFoot!.scalingInfos[1].ScalingFunction(characterBody.inventory.GetItemCount(bunnyFoot.ItemDef)) ? vector : addvel;
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(CharacterMotor), nameof(CharacterMotor.PreMove))]
		public static void PatchMovement(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(
				x => x.MatchMul(),
				x => x.MatchCall<Vector3>(nameof(Vector3.MoveTowards))
			);
			c.RemoveRange(2);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<Vector3, Vector3, float, float, CharacterMotor, Vector3>>(DoAirMovement);
		}

		public static Vector3 DoAirMovement(Vector3 velocity, Vector3 target, float num, float deltaTime, CharacterMotor motor)
		{
			var bunnyFoot = GetInstance<BunnyFoot>();
			var count = motor.body?.inventory?.GetItemCount(bunnyFoot.ItemDef) ?? 0; 
			if (count <= 0 || motor.disableAirControlUntilCollision || motor.Motor.GroundingStatus.IsStableOnGround)
				return Vector3.MoveTowards(velocity, target, num * deltaTime);

			var newTarget = target;
			if (!motor.isFlying)
				newTarget.y = 0;

			var wishDir = newTarget.normalized;
			var wishSpeed = motor.walkSpeed * wishDir.magnitude;

			return Accelerate(velocity, wishDir, wishSpeed, bunnyFoot.scalingInfos[0].ScalingFunction(count), motor.acceleration, deltaTime);
		}

		//Ripped from sbox or gmod, i dont remember
		public static Vector3 Accelerate(Vector3 velocity, Vector3 wishDir, float wishSpeed, float speedLimit, float acceleration, float deltaTime)
		{
			if ( speedLimit > 0 && wishSpeed > speedLimit )
				wishSpeed = speedLimit;

			// See if we are changing direction a bit
			var currentspeed = Vector3.Dot(velocity, wishDir );

			// Reduce wishspeed by the amount of veer.
			var addspeed = wishSpeed - currentspeed;

			// If not going to add any speed, done.
			if ( addspeed <= 0 )
				return velocity;

			// Determine amount of acceleration.
			var accelspeed = acceleration * deltaTime * wishSpeed; // * SurfaceFriction;

			// Cap at addspeed
			if ( accelspeed > addspeed )
				accelspeed = addspeed;

			return velocity + wishDir * accelspeed;
		}
		protected override void FillItemDisplayRules()
		{
			base.FillItemDisplayRules();

			var def = new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(0.12805F, 0.27567F, 0.09413F),
				localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
				localScale = new Vector3(0.4149F, 0.4149F, 0.4149F)
			};
			
			AddDisplayRules(ModdedIDRS.NemesisEnforcer, new ItemDisplayRule()
			{
				childName = "Hammer",
				localPos = new Vector3(0F, 0.01133F, -0.0109F),
				localAngles = new Vector3(301.6936F, 180F, 180F),
				localScale = new Vector3(0.015F, 0.015F, 0.015F),
			});
			AddDisplayRules(ModdedIDRS.Nemmando, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(0.00132F, 0.00189F, -0.00017F),
				localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
				localScale = new Vector3(0.00473F, 0.00473F, 0.00473F),
			});
			AddDisplayRules(VanillaIDRS.Commando, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(0.10964F, 0.2722F, 0.07702F),
				localAngles = new Vector3(297.8961F, 337.7542F, 3.62604F),
				localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
			});
			AddDisplayRules(VanillaIDRS.Huntress, def);
			AddDisplayRules(VanillaIDRS.Bandit, new ItemDisplayRule()
			{
				childName = "ThighR",
				localPos = new Vector3(-0.10279F, 0.35127F, 0.05551F),
				localAngles = new Vector3(327.1544F, 5.32863F, 5.95733F),
				localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
			});
			AddDisplayRules(VanillaIDRS.Mult, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(0.04776F, 1.88373F, 1.01038F),
				localAngles = new Vector3(333.9735F, 281.9487F, 357.2159F),
				localScale = new Vector3(2.37168F, 2.37168F, 2.37168F),
			});
			AddDisplayRules(ModdedIDRS.Executioner, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(-0.00189F, 0.00099F, -0.00019F),
				localAngles = new Vector3(286.0008F, 198.6392F, 327.8065F),
				localScale = new Vector3(0.00436F, 0.00436F, 0.00436F),
			});
			AddDisplayRules(ModdedIDRS.Enforcer, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(0.11708F, 0.24051F, 0.17615F),
				localAngles = new Vector3(324.415F, 293.5852F, 355.0646F),
				localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
			});
			AddDisplayRules(VanillaIDRS.Engineer, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(0.16015F, 0.27443F, 0.0233F),
				localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
				localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
			});
			AddDisplayRules(VanillaIDRS.Artificer, def);
			AddDisplayRules(VanillaIDRS.Mercenary, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(0.17358F, 0.26745F, 0.04173F),
				localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
				localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
			});
			AddDisplayRules(ModdedIDRS.Paladin, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(-0.18165F, 0.3724F, -0.19136F),
				localAngles = new Vector3(294.1706F, 158.5404F, 344.7267F),
				localScale = new Vector3(0.53494F, 0.53494F, 0.53494F),
			});
			AddDisplayRules(VanillaIDRS.Rex, new ItemDisplayRule()
			{
				childName = "PlatformBase",
				localPos = new Vector3(-0.68795F, -0.12907F, 0.03073F),
				localAngles = new Vector3(64.63686F, 352.9881F, 178.495F),
				localScale = new Vector3(0.67997F, 0.67997F, 0.67997F),
			});
			AddDisplayRules(VanillaIDRS.Loader, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(0.15646F, 0.28357F, 0.1099F),
				localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
				localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
			});
			AddDisplayRules(VanillaIDRS.Acrid, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(1.2091F, 0.51674F, 0.01721F),
				localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
				localScale = new Vector3(3.07934F, 3.07934F, 3.07934F),
			});
			AddDisplayRules(VanillaIDRS.Captain, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(0.1283F, 0.24574F, 0.0152F),
				localAngles = new Vector3(304.4732F, 6.59901F, 343.9665F),
				localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
			});
			/*
			AddDisplayRules(ModdedIDRS.ReinSniper, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(-0.13938F, -0.19064F, 0.00388F),
				localAngles = new Vector3(87.36492F, 337.0182F, 144.9593F),
				localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
			});
			*/
			AddDisplayRules(VanillaIDRS.Heretic, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(0.28362F, 0.29826F, -0.07479F),
				localAngles = new Vector3(358.4014F, 83.00351F, 56.10048F),
				localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
			});
			AddDisplayRules(ModdedIDRS.Miner, new ItemDisplayRule()
			{
				childName = "LegL",
				localPos = new Vector3(-0.00001F, 0.00156F, 0.00128F),
				localAngles = new Vector3(313.4082F, 274.817F, 356.2425F),
				localScale = new Vector3(0.00398F, 0.00398F, 0.00398F),
			});
			AddDisplayRules(ModdedIDRS.CHEF, new ItemDisplayRule()
			{
				childName = "LeftLeg",
				localPos = new Vector3(-0.00232F, -0.00503F, 0.00649F),
				localAngles = new Vector3(340F, 180F, 0F),
				localScale = new Vector3(0.01294F, 0.01294F, 0.01294F),
			});
			AddDisplayRules(ModdedIDRS.Hand, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(-0.52056F, 1.72429F, -0.37539F),
				localAngles = new Vector3(348.6181F, 178.1265F, 2.98409F),
				localScale = new Vector3(2.59279F, 2.59279F, 2.59279F),
			});
			AddDisplayRules(ModdedIDRS.BanditReloaded, new ItemDisplayRule()
			{
				childName = "ThighR",
				localPos = new Vector3(0.00024F, 0.27766F, 0.14929F),
				localAngles = new Vector3(326.1603F, 45.63481F, 355.6136F),
				localScale = new Vector3(0.4149F, 0.4149F, 0.4149F),
			});
			AddDisplayRules(VanillaIDRS.Scavenger, new ItemDisplayRule()
			{
				childName = "ThighL",
				localPos = new Vector3(2.51467F, 0.74224F, 0.46471F),
				localAngles = new Vector3(302.574F, 340.1447F, 21.65013F),
				localScale = new Vector3(3.77607F, 3.77607F, 3.77607F),
			});
			/*
			AddDisplayRules(VanillaIDRS.Engineer,
				new ItemDisplayRule
				{
					childName = "Chest",
					localPos = new Vector3(0F, 0F, 0F),
					localAngles = new Vector3(0F, 0F, 0F),
					localScale = new Vector3(0.99442F, 0.99442F, 0.99442F)
		
				}
			);
			AddDisplayRules(VanillaIDRS.Commando,
				new ItemDisplayRule
				{
		
				}
			);
			AddDisplayRules(VanillaIDRS.Huntress,
				new ItemDisplayRule
				{
		
				}
			);
			AddDisplayRules(VanillaIDRS.Bandit,
				new ItemDisplayRule
				{
		
				}
			);
			AddDisplayRules(VanillaIDRS.Mult,
				new ItemDisplayRule
				{
		
				}
			);
			AddDisplayRules(VanillaIDRS.Artificer,
				new ItemDisplayRule
				{
		
				}
			);
			AddDisplayRules(VanillaIDRS.Mercenary,
				new ItemDisplayRule
				{
		
				}
			);
			AddDisplayRules(VanillaIDRS.Rex,
				new ItemDisplayRule
				{
		
				}
			);
			AddDisplayRules(VanillaIDRS.Loader,
				new ItemDisplayRule
				{
		
				}
			);
			AddDisplayRules(VanillaIDRS.Acrid,
				new ItemDisplayRule
				{
		
				}
			);
			AddDisplayRules(VanillaIDRS.Captain,
				new ItemDisplayRule
				{
		
				}
			);
			AddDisplayRules(VanillaIDRS.RailGunner,
				new ItemDisplayRule
				{
		
				}
			);
			AddDisplayRules(VanillaIDRS.VoidFiend,
				new ItemDisplayRule
				{
		
				}
			);
			//*/
		}
	}
}