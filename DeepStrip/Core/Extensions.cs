using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace DeepStrip.Core
{
	internal static class Extensions
	{
		public static void RemoveWhere<T>(this IList<T> @this, Func<T, bool> predicate)
		{
			for (var i = @this.Count - 1; i >= 0; --i)
				if (predicate(@this[i]))
					@this.RemoveAt(i);
		}

		public static bool InheritsFrom(this TypeDefinition @this, string @base)
		{
			var parent = @this.BaseType;
			while (parent is not null)
			{
				if (parent.FullName == @base)
					return true;

				parent = parent.Resolve().BaseType;
			}

			return false;
		}
	}
}
