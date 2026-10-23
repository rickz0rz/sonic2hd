using System;
using System.Collections.Generic;

namespace SonicOrca.Extensions;

/// <summary>
/// Finds items in read-only lists.
/// </summary>
public static class ListExtensions
{
	public static int IndexOf<T>(this IReadOnlyList<T> list, Predicate<T> match)
	{
		return list.IndexOf(0, match);
	}

	public static int IndexOf<T>(this IReadOnlyList<T> list, int startIndex, Predicate<T> match)
	{
		for (int i = startIndex; i < list.Count; i++)
		{
			if (match(list[i]))
			{
				return i;
			}
		}
		return -1;
	}
}
