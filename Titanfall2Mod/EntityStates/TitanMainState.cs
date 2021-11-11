using EntityStates;
using UnityEngine;

namespace Titanfall2Mod.EntityStates
{
    public class TitanMainState : GenericCharacterMain
    {
        public override void ProcessJump()
        {
            if (!hasCharacterMotor)
                return;
            if (!jumpInputReceived || !(bool) characterBody || characterMotor.jumpCount >= characterBody.maxJumpCount)
                return;
            //TODO do sliding/dashing here
            Debug.Log("we do a little dashing");
        }
    }
}