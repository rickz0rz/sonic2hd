using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Extensions;

/// <summary>
/// Contains more LINQ-style operations on sequences.
/// </summary>
public static class LinqExtensions
{
	private const int HashSeed = 27;

	private const int HashMultiplier = 13;

	/// <summary>
	/// The hash code that a null element adds to a collection hash code.
	/// </summary>
	private const int NullElementHashCode = -1;

	public static T[] AsArray<T>(this IEnumerable<T> items)
	{
		return items as T[] ?? items.ToArray();
	}

	public static int GetCollectionHashCode<T>(this IEnumerable<T> e)
	{
		IEnumerator<T> enumerator = e.GetEnumerator();
		int hash = HashSeed;
		if (enumerator.MoveNext())
		{
			// The first element multiplies the seed. Each element after it is added.
			hash *= GetElementHashCode(enumerator.Current);
		}
		while (enumerator.MoveNext())
		{
			hash = HashMultiplier * hash + GetElementHashCode(enumerator.Current);
		}
		return hash;
	}

	private static int GetElementHashCode<T>(T element)
	{
		return element == null ? NullElementHashCode : element.GetHashCode();
	}

	public static int GetCollectionHashCode(params object[] items)
	{
		return items.GetCollectionHashCode();
	}

	/// <summary>
	/// Tests if the two sequences contain the same elements the same number of times, in any order.
	/// </summary>
	public static bool HasSameElementsAs<T>(this IEnumerable<T> first, IEnumerable<T> second)
	{
		Dictionary<T, int> firstMap = first.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
		Dictionary<T, int> secondMap = second.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
		if (firstMap.Keys.All(x => secondMap.Keys.Contains(x) && firstMap[x] == secondMap[x]))
		{
			return secondMap.Keys.All(x => firstMap.Keys.Contains(x) && secondMap[x] == firstMap[x]);
		}
		return false;
	}

	/// <summary>
	/// Joins the items as text in the form "a, b and c".
	/// </summary>
	public static string CommaAndConcatenation<T>(this IEnumerable<T> strings)
	{
		T[] items = strings.ToArray();
		switch (items.Length)
		{
			case 0:
				return string.Empty;
			case 1:
				return items[0].ToString();
			case 2:
				return $"{items[0]} and {items[1]}";
			default:
				return string.Format("{0} and {1}", string.Join(", ", items.Take(items.Length - 1)), items.Last());
		}
	}

	public static IEnumerable<T> TakeAllButLast<T>(this IEnumerable<T> source)
	{
		IEnumerator<T> it = source.GetEnumerator();
		bool isFirst = true;
		T previous = default;
		bool hasRemainingItems;
		do
		{
			hasRemainingItems = it.MoveNext();
			if (hasRemainingItems)
			{
				// Give the previous item only when a next item exists, so the last item is never given.
				if (!isFirst)
				{
					yield return previous;
				}
				previous = it.Current;
				isFirst = false;
			}
		}
		while (hasRemainingItems);
	}

	public static IEnumerable<T> Intersperse<T>(this IEnumerable<T> source, T separator)
	{
		bool isFirst = true;
		foreach (T value in source)
		{
			if (!isFirst)
			{
				yield return separator;
			}
			yield return value;
			isFirst = false;
		}
	}

	/// <summary>
	/// Splits the sequence into groups. Each item for which <paramref name="shouldSplit"/> is true starts a new group.
	/// </summary>
	public static IEnumerable<IEnumerable<T>> SplitWhen<T>(this IEnumerable<T> source, Func<T, bool> shouldSplit)
	{
		List<T> currentGroup = new List<T>();
		foreach (T item in source)
		{
			if (shouldSplit(item) && currentGroup.Count > 0)
			{
				yield return currentGroup.ToArray();
				currentGroup.Clear();
			}
			currentGroup.Add(item);
		}
		if (currentGroup.Count > 0)
		{
			yield return currentGroup.ToArray();
		}
	}

	public static bool Contains(this IEnumerable<string> items, string query, bool ignoreCase = false)
	{
		return items.Any(x => string.Compare(x, query, ignoreCase) == 0);
	}

	public static string FirstOrDefault(this IEnumerable<string> items, string query, bool ignoreCase = false)
	{
		return items.FirstOrDefault(x => string.Compare(x, query, ignoreCase) == 0);
	}

	public static IEnumerable<string> Surround(this IEnumerable<string> strings, string surrounder)
	{
		return strings.Select(x => surrounder + x + surrounder);
	}
}
