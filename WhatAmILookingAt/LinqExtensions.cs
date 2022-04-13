using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WhatAmILookingAt
{
	public static class LinqExtensions
	{
		public static bool IsInScene(this GameObject go)
		{
			// test if the object is from the scene
			var current = go.transform;
			while (current.parent)  // This is a nasty solution
			{
				current = current.parent;
			}

			var nam = current.name;
			if (nam.Contains("HOLDER") || nam.StartsWith("FOLIAGE") || nam.Contains("RaidAL") || nam.StartsWith("RaidVoid") || nam.Contains("mdlSP") || nam.StartsWith("SKYBOX") || nam == "GAMEPLAY SPACE")
				return true;
			return false;
		}
		
		public static bool TryFirst<T>(this IEnumerable<T> en, Func<T, bool> predicate, out T value)
		{
			try
			{
				value = en.First(predicate);
				return true;
			}
			catch (InvalidOperationException)
			{
				value = default;
				return false;
			} 
		}
	}
}