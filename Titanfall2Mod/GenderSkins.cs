using RoR2;
using UnityEngine;

namespace Titanfall2Mod
{
    [CreateAssetMenu(menuName = "GenderSkins")]
    public class GenderSkins : ScriptableObject
    {
        public SkinDef[] male;
        public SkinDef[] female;
    }
}