using System;
using System.Collections.Generic;

namespace DeepStrip.Core
{
	internal static class Extensions
	{
		public static void RemoveWhere<T>(this IList<T> @this, Func<T, bool> predicate, ref uint stats)
		{
			for (var i = @this.Count - 1; i >= 0; --i)
				if (predicate(@this[i]))
				{
					@this.RemoveAt(i);
					++stats;
				}
		}
	}
}
