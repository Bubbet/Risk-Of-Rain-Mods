using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using UnityEngine;


namespace Bhop
{
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI))]
    [BepInPlugin("bubbet.plugins.bhop", "Bhop", "1.0.7.0")]
    [BepInDependency("com.bepis.r2api")]
    public class Bhop : BaseUnityPlugin
    {
        private ConfigEntry<float> _configBaseAirControl;
        private ConfigEntry<float> _configAddedAirControl;
        private ConfigEntry<int> _configItemTier;
        private ConfigEntry<int> _configScalingFunction;
        private ConfigEntry<float> _configScalingFunctionValA;

        private Func<float, float, int, float, float>[] _scalingFuncs = {
            (@base, added, count, _) => @base + (count-1) * added,
            (@base, added, count, arg1) => @base + Mathf.Pow(count, arg1) * added,
            (@base, added, count, arg1) => arg1 / (count-added) + @base
        };

        public void Awake()
        {
            _configBaseAirControl = Config.Bind("General", "Base Air Control", 3f, "Base air control, more = easier to turn and still gain speed.");
            _configAddedAirControl = Config.Bind("General", "Added Air Control", 1.5f, "Like Base air control but added per stack of item.");
            _configItemTier = Config.Bind("General", "Item Tier", 2, "Changes the spawn tier of the item. 1 = white, 2 = green, 3 = red, 4 = lunar, 5 = boss");
            _configScalingFunction = Config.Bind("Advanced", "Scaling Function", 0, "Changes the scaling function of the bunny feet: 0 = linear(base + (count-1)*added), 1 = power law (base + Mathf.Pow(count, arg1) * added), 2 = Rectangular Hyperbola (arg1 / (count-added) + base)");
            _configScalingFunctionValA = Config.Bind("Advanced", "Scaling Function Arg1", 0.5f, "Changes the first arbitrary value in scaling function: ex: power law's exponent (arg1)");
            //On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };
            /*
            IL.EntityStates.Loader.BaseSwingChargedFist.OnEnter += il => 
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchCall<EntityState>("get_characterMotor"),
                    x => x.MatchDup(),
                    x => x.MatchLdfld<CharacterMotor>("disableAirControlUntilCollision"),
                    x => x.MatchLdsfld<BaseSwingChargedFist>("disableAirControlUntilCollision"),
                    x => x.MatchOr(),
                    x => x.MatchStfld<CharacterMotor>("disableAirControlUntilCollision")
                );
                c.RemoveRange(5);
                c.Emit(OpCodes.Ldc_I4_0);
            };
            IL.EntityStates.Toolbot.ToolbotDualWieldStart.OnEnter += il =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCall<EntityState>("get_characterMotor"),
                    x => x.MatchLdcI4(1),
                    x => x.MatchStfld<CharacterMotor>("disableAirControlUntilCollision")
                );
                c.Index += 2;
                c.Remove();
                c.Emit(OpCodes.Ldc_I4_0);
            };*/
            IL.RoR2.Projectile.ProjectileGrappleController.GripState.FixedUpdateBehavior += il =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchCall<Vector3>("op_Multiply"),
                    x => x.MatchLdcI4(1),
                    x => x.MatchLdcI4(1)
                );
                c.Index += 2;
                c.Remove();
                c.Emit(OpCodes.Ldc_I4_0);
            };

            IL.EntityStates.GenericCharacterMain.ApplyJumpVelocity += il =>
            {
                var del = new Func<Vector3, CharacterMotor, CharacterBody, Vector3>((vector, cm, cb) =>
                {
                    var horizontal = vector + Vector3.down * vector.y;
                    var vhorizontal = cm.velocity + Vector3.down * cm.velocity.y;
                    if (vhorizontal.sqrMagnitude > horizontal.sqrMagnitude) horizontal = vhorizontal;
                    horizontal.y = vector.y;
                    return horizontal;
                });
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdloc(0),
                    x => x.MatchStfld<CharacterMotor>("velocity")
                );
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate(del);
                c.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdloc(2),
                    x => x.MatchStfld<CharacterMotor>("velocity")
                );
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate(del);
            };

            IL.RoR2.CharacterMotor.PreMove += il =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<CharacterMotor>("velocity"),
                    x => x.MatchLdloc(2),
                    x => x.MatchLdloc(0),
                    x => x.MatchLdarg(1),
                    x => x.MatchMul()
                );
                c.Index += 7; // velocity_from_MoveTowards

                c.Emit(OpCodes.Ldarg_0); // Fetch velocity field, pass this as "self" for method get
                c.Emit(OpCodes.Ldfld, typeof(CharacterMotor).GetRuntimeField("velocity")); // velocity_old

                c.Emit(OpCodes.Ldloc_2); // target
                
                c.Emit(OpCodes.Ldarg_1); // deltaTime

                c.Emit(OpCodes.Ldarg_0); // self

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(CharacterMotor).GetField("body", BindingFlags.NonPublic | BindingFlags.Instance)); // body
                
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(CharacterMotor).GetField("disableAirControlUntilCollision", BindingFlags.Public | BindingFlags.Instance)); // disableAirControlUntilCollision

                c.EmitDelegate<NewMove>(NewMoveMeth);
            };

            Assets.Init(Logger, (_configBaseAirControl.Value, _configAddedAirControl.Value, _configItemTier.Value));
        }
        
        delegate Vector3 NewMove(Vector3 velocityFromMoveTowards, Vector3 velocityOld, Vector3 target, float deltaTime, CharacterMotor self, CharacterBody body, bool disableAirControlUntilCollision);
        Vector3 NewMoveMeth(Vector3 velocityFromMoveTowards, Vector3 velocityOld, Vector3 target, float deltaTime, CharacterMotor self, CharacterBody body, bool disableAirControlUntilCollision)
        {
            if (!disableAirControlUntilCollision && !self.Motor.GroundingStatus.IsStableOnGround)
            {
                int count = body.inventory.GetItemCount(Assets.BhopFeatherDef);
                if (count <= 0) return velocityFromMoveTowards;
                        
                var newTarget = target; 
                if (!self.isFlying)
                {
                    newTarget.y = 0;
                }
                        
                var wishdir = newTarget.normalized;
                var wishspeed = self.walkSpeed * wishdir.magnitude;
                
                return Accelerate(velocityOld, wishdir, wishspeed, _scalingFuncs[_configScalingFunction.Value](_configBaseAirControl.Value, _configAddedAirControl.Value, count, _configScalingFunctionValA.Value), self.acceleration, deltaTime);
            }
            return velocityFromMoveTowards;
        }

        static Vector3 Accelerate(Vector3 velocity, Vector3 wishdir, float wishspeed, float speedLimit, float acceleration, float deltaTime)
        {
            if ( speedLimit > 0 && wishspeed > speedLimit )
                wishspeed = speedLimit;

            // See if we are changing direction a bit
            var currentspeed = Vector3.Dot(velocity, wishdir );

            // Reduce wishspeed by the amount of veer.
            var addspeed = wishspeed - currentspeed;

            // If not going to add any speed, done.
            if ( addspeed <= 0 )
                return velocity;

            // Determine amount of acceleration.
            var accelspeed = acceleration * deltaTime * wishspeed; // * SurfaceFriction;

            // Cap at addspeed
            if ( accelspeed > addspeed )
                accelspeed = addspeed;

            return velocity + wishdir * accelspeed;
        }
    }
}