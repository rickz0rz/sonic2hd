using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Extensions;

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
		T[] array = collection.Where((T x) => match(x)).ToArray();
		T[] array2 = array;
		foreach (T item in array2)
		{
			collection.Remove(item);
		}
		return array.Length;
	}
}
