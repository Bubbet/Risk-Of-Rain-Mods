using EntityStates;
using RoR2;
using UnityEngine;

namespace Titanfall2Mod.EntityStates
{
    public class PilotMainState : GenericCharacterMain
    {
        //private bool _jumped;
        //private static readonly int JustJumped = Animator.StringToHash("justJumped");
        private static readonly int MoveSpeedFactor = Animator.StringToHash("moveSpeedFactor");
        private PilotBehavior _pilotBehavior;

        public string[] jumpAnims = {
            "a_Jump_start",
            "pt_Jump_Forward",
            "a_MP_DoubleJump"
        };

        public bool HasGrounded = true;

        public override void ProcessJump()
        { // TODO do some kind of check to make sure we dont shove away from the wall when being on the ground/first jump
            if (!hasCharacterMotor)
                return;

            if (_pilotBehavior.touchingWall && HasGrounded)
            {
                characterMotor.jumpCount = 0;
                HasGrounded = false;
            }

            var @case = 0;
            var layerIndex = -1;
            if (hasModelAnimator)
            {
                layerIndex = modelAnimator.GetLayerIndex("Body");
                if (layerIndex >= 0)
                {
                    var worldMoveVector = (bool) (Object) inputBank ? inputBank.moveVector : Vector3.zero;
                    var flag2 = worldMoveVector != Vector3.zero && (double) characterBody.moveSpeed > Mathf.Epsilon;
                    var firstJump = characterMotor.jumpCount == 0 || characterBody.baseJumpCount == 1;
                    if (!flag2 && firstJump) @case = 1;
                    else if (flag2 && firstJump) @case = 2;
                    else @case = 3;
                }
            }

            var flag = !jumpInputReceived || !(bool) characterBody ||
                       characterMotor.jumpCount >= characterBody.maxJumpCount;
            base.ProcessJump();

            if (flag) return;

            if (_pilotBehavior.touchingWall)
                characterMotor.velocity += (_pilotBehavior.lastTouchNormal - Vector3.up * 0.025f) * 50f;

            
            modelAnimator.CrossFadeInFixedTime(jumpAnims[@case-1], smoothingParameters.intoJumpTransitionTime, layerIndex);
        }

        public override void UpdateAnimationParameters()
        {
            base.UpdateAnimationParameters();

            /*
            if (_jumped)
            {
                modelAnimator.SetBool(JustJumped, true);
                _jumped = false;
            }
            else
            {
                modelAnimator.SetBool(JustJumped, false);
            }*/

            modelAnimator.SetFloat(MoveSpeedFactor,
                Mathf.Min(characterBody.moveSpeed / characterBody.baseMoveSpeed, 2f));
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _pilotBehavior = GetComponent<PilotBehavior>();
            characterMotor.onHitGroundServer += CharacterMotorOnHitGroundServer;
        }

        public override void OnExit()
        {
            base.OnExit();
            characterMotor.onHitGroundServer -= CharacterMotorOnHitGroundServer;
        }

        private void CharacterMotorOnHitGroundServer(ref CharacterMotor.HitGroundInfo hitgroundinfo)
        {
            HasGrounded = true;
        }
    }
}