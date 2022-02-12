using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WhatAmILookingAt
{
	public static class LinqExtensions
	{
		public static object FirstOrNull<T>(this IEnumerable<T> en, Func<T, bool> predicate)
		{
			try
			{
				return en.First(predicate);
			}
			catch (InvalidOperationException e)
			{
				return null;
			}
		}

		public static bool TryFirst<T>(this IEnumerable<T> en, Func<T, bool> predicate, out T value)
		{
			try
			{
				value = en.First(predicate);
				return true;
			}
			catch (InvalidOperationException e)
			{
				value = default;
				return false;
			} 
		}
	}
}