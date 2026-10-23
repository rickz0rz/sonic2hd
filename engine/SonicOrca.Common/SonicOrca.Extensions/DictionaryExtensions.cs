using System.Collections.Generic;

namespace SonicOrca.Extensions;

/// <summary>
/// Gets values from dictionaries with a fallback value.
/// </summary>
public static class DictionaryExtensions
{
	public static Tvalue GetValueOrDefault<Tkey, Tvalue>(this IDictionary<Tkey, Tvalue> dictionary, Tkey key)
	{
		if (!dictionary.TryGetValue(key, out Tvalue value))
		{
			return default;
		}
		return value;
	}

	public static Tvalue GetValueOrDefault<Tkey, Tvalue>(this IDictionary<Tkey, Tvalue> dictionary, Tkey key, Tvalue defaultValue)
	{
		if (!dictionary.TryGetValue(key, out Tvalue value))
		{
			return defaultValue;
		}
		return value;
	}
}
