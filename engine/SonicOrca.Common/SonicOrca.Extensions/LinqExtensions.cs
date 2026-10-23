using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Extensions;

public static class LinqExtensions
{
	public static T[] AsArray<T>(this IEnumerable<T> items)
	{
		T[] array = items as T[];
		if (array == null)
		{
			array = items.ToArray();
		}
		return array;
	}

	public static int GetCollectionHashCode<T>(this IEnumerable<T> e)
	{
		IEnumerator<T> enumerator = e.GetEnumerator();
		int num = 27;
		if (enumerator.MoveNext())
		{
			num *= ((enumerator.Current == null) ? (-1.GetHashCode()) : enumerator.Current.GetHashCode());
		}
		while (enumerator.MoveNext())
		{
			num = 13 * num + ((enumerator.Current == null) ? (-1.GetHashCode()) : enumerator.Current.GetHashCode());
		}
		return num;
	}

	public static int GetCollectionHashCode(params object[] items)
	{
		return items.GetCollectionHashCode();
	}

	public static bool HasSameElementsAs<T>(this IEnumerable<T> first, IEnumerable<T> second)
	{
		Dictionary<T, int> firstMap = (from x in first
									   group x by x).ToDictionary((IGrouping<T, T> x) => x.Key, (IGrouping<T, T> x) => x.Count());
		Dictionary<T, int> secondMap = (from x in second
										group x by x).ToDictionary((IGrouping<T, T> x) => x.Key, (IGrouping<T, T> x) => x.Count());
		if (firstMap.Keys.All((T x) => secondMap.Keys.Contains(x) && firstMap[x] == secondMap[x]))
		{
			return secondMap.Keys.All((T x) => firstMap.Keys.Contains(x) && secondMap[x] == firstMap[x]);
		}
		return false;
	}

	public static string CommaAndConcatenation<T>(this IEnumerable<T> strings)
	{
		T[] array = strings.ToArray();
		if (array.Length == 0)
		{
			return string.Empty;
		}
		if (array.Length == 1)
		{
			return array[0].ToString();
		}
		if (array.Length == 2)
		{
			return $"{array[0]} and {array[1]}";
		}
		return string.Format("{0} and {1}", string.Join(", ", array.Take(array.Length - 1)), array.Last());
	}

	public static IEnumerable<T> TakeAllButLast<T>(this IEnumerable<T> source)
	{
		IEnumerator<T> it = source.GetEnumerator();
		bool flag = true;
		T val = default;
		bool hasRemainingItems;
		do
		{
			hasRemainingItems = it.MoveNext();
			if (hasRemainingItems)
			{
				if (!flag)
				{
					yield return val;
				}
				val = it.Current;
				flag = false;
			}
		}
		while (hasRemainingItems);
	}

	public static IEnumerable<T> Intersperse<T>(this IEnumerable<T> source, T separator)
	{
		bool flag = true;
		foreach (T value in source)
		{
			if (!flag)
			{
				yield return separator;
			}
			yield return value;
			flag = false;
		}
	}

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
		return items.Any((string x) => string.Compare(x, query, ignoreCase) == 0);
	}

	public static string FirstOrDefault(this IEnumerable<string> items, string query, bool ignoreCase = false)
	{
		return items.FirstOrDefault((string x) => string.Compare(x, query, ignoreCase) == 0);
	}

	public static IEnumerable<string> Surround(this IEnumerable<string> strings, string surrounder)
	{
		return strings.Select((string x) => surrounder + x + surrounder);
	}
}
