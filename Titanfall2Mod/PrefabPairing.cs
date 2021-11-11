using System.Collections.Generic;
using UnityEngine;

namespace Titanfall2Mod
{
    [CreateAssetMenu(menuName = "PrefabPairing")]
    public class PrefabPairing : ScriptableObject
    {
        public GameObject[] prefabs;
        public string[] keys;

        public Dictionary<string, GameObject> ToDict()
        {
            var dict = new Dictionary<string, GameObject>();
            for (var i = 0; i < prefabs.Length; i++)
            {
                dict[keys[i]] = prefabs[i];
            }
            return dict;
        }
    }
}