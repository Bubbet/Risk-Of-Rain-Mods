using RoR2;
using UnityEngine;

namespace Titanfall2Mod
{
    public class SoundManager : MonoBehaviour
    {
        public CharacterBody Body;
        public SkillLocator Locator;
        public InputBankTest InputBankTest;

        private uint _currentlyPlaying;
        public void FixedUpdate()
        {
            if (Locator.primary.IsReady() && InputBankTest.skill1.down && _currentlyPlaying == 0)
            {
                //_currentlyPlaying = AkSoundEngine.PostEvent("StartCarShootPointBlank", gameObject);
            }

            if ((Locator.primary.stock <= 1 || !InputBankTest.skill1.down) && _currentlyPlaying != 0)
            {
                AkSoundEngine.PostEvent("StopCarShootPointBlank", gameObject);
                _currentlyPlaying = 0;
            }
        }
    }
}