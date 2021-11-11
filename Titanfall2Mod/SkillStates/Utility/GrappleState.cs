using System.Linq;
using EntityStates;
using RoR2;
using Titanfall2Mod.EntityStates;

namespace Titanfall2Mod.SkillStates.Utility
{
    public class GrappleState : BaseSkillState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            outer.SetNextStateToMain();
            var moveState = (PilotMainState) outer.GetComponents<EntityStateMachine>().FirstOrDefault(x => x.customName == "Body")?.state;
            if (moveState != null) moveState.HasGrounded = true;
        }
    }
}