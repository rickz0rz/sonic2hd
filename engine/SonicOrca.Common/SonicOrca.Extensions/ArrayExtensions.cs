using System;
using System.Collections.Generic;

namespace SonicOrca.Extensions;

/// <summary>
/// Contains extension methods for arrays.
/// </summary>
public static class ArrayExtensions
{
	public static IEnumerator<T> GetEnumeratorGeneric<T>(this T[] array)
	{
		return ((IEnumerable<T>)array).GetEnumerator();
	}

	public static T[] GetRange<T>(this T[] data, int index, int length)
	{
		T[] range = new T[length];
		Array.Copy(data, index, range, 0, length);
		return range;
	}
}
