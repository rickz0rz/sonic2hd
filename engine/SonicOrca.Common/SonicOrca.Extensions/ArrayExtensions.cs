using System;
using System.Collections.Generic;

namespace SonicOrca.Extensions;

public static class ArrayExtensions
{
	public static IEnumerator<T> GetEnumeratorGeneric<T>(this T[] array)
	{
		return ((IEnumerable<T>)array).GetEnumerator();
	}

	public static T[] GetRange<T>(this T[] data, int index, int length)
	{
		T[] array = new T[length];
		Array.Copy(data, index, array, 0, length);
		return array;
	}
}
