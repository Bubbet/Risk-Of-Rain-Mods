using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Titanfall2Mod
{
    public static class ExtensionMethods
    {
        /// <summary>
        ///   <para>Finds a child by name and returns it. A recursive function.</para>
        /// </summary>
        /// <param name="name">Name of child to be found.</param>
        /// <returns>
        ///   <para>The returned child transform or null if no child is found.</para>
        /// </returns>
        [CanBeNull]
        public static Transform FindInChildren(this Transform me, string name)
        {
            for(int i = 0; i < me.childCount; i++)
            {
                var child = me.GetChild(i);
                if (child.name == name) return child;
                var result = child.FindInChildren(name);
                if (result != null) return result;
            }

            return null;
        }
    }
}