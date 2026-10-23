using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Extensions;

/// <summary>
/// Adds and removes many items in a collection.
/// </summary>
public static class CollectionExtensions
{
	public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
	{
		foreach (T item in items)
		{
			collection.Add(item);
		}
	}

	public static void RemoveRange<T>(this ICollection<T> collection, IEnumerable<T> items)
	{
		foreach (T item in items)
		{
			collection.Remove(item);
		}
	}

	public static int RemoveAll<T>(this ICollection<T> collection, Predicate<T> match)
	{
		// Copy the matches first, because the collection cannot change while it is enumerated.
		T[] matches = collection.Where(x => match(x)).ToArray();
		foreach (T item in matches)
		{
			collection.Remove(item);
		}
		return matches.Length;
	}
}
