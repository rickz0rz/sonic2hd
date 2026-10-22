using System.Collections.Generic;

namespace SonicOrca.Extensions;

public static class DictionaryExtensions
{
	public static Tvalue GetValueOrDefault<Tkey, Tvalue>(this IDictionary<Tkey, Tvalue> dictionary, Tkey key)
	{
		if (!dictionary.TryGetValue(key, out var value))
		{
			return default;
		}
		return value;
	}

	public static Tvalue GetValueOrDefault<Tkey, Tvalue>(this IDictionary<Tkey, Tvalue> dictionary, Tkey key, Tvalue defaultValue)
	{
		if (!dictionary.TryGetValue(key, out var value))
		{
			return defaultValue;
		}
		return value;
	}
}
