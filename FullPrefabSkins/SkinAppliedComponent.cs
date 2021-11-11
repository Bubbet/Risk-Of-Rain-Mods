using System;
using UnityEngine;

namespace FullPrefabSkins
{
    public class SkinAppliedComponent : MonoBehaviour
    {
        public static Action<GameObject> skinApplied;

        public void Awake()
        {
            skinApplied?.Invoke(gameObject);
        }
    }
}