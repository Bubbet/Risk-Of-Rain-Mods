using RoR2;
using UnityEngine;

namespace Titanfall2Mod
{
    
    [CreateAssetMenu(menuName = "LessRetardedUnlockableDef")]
    public class LessRetardedUnlockable : UnlockableDef
    {
        public string howToUnlockToken;
        public string unlockedToken;
        public int sortScoreValue;

        public new void Awake()
        {
            sortScore = sortScoreValue;
            getUnlockedString = () => unlockedToken;
            getHowToUnlockString = () => howToUnlockToken;
            cachedName = ((ScriptableObject) this).name;
        }
    }
}