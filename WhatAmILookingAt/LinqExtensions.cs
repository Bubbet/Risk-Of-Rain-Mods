using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WhatAmILookingAt
{
	public static class LinqExtensions
	{
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