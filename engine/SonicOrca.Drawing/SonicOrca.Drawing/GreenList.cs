using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Drawing;

/// <summary>
/// A list that grows by a fixed number of items. For a reference type, it fills new slots with new instances.
/// </summary>
internal class GreenList<T>
{
	private T[] _items;

	public int Count { get; private set; }

	public int Capacity { get; private set; }

	public int GrowFactor { get; set; }

	public T this[int index]
	{
		get
		{
			return _items[index];
		}
		set
		{
			_items[index] = value;
		}
	}

	public GreenList(int initialCapacity, int growFactor)
	{
		Capacity = initialCapacity;
		GrowFactor = growFactor;
		_items = new T[initialCapacity];
		if (typeof(T).IsClass)
		{
			for (int i = 0; i < initialCapacity; i++)
			{
				_items[i] = Activator.CreateInstance<T>();
			}
		}
	}

	public void EnsureCapacity(int targetCapacity)
	{
		if (Capacity >= targetCapacity)
		{
			return;
		}
		Array.Resize(ref _items, targetCapacity);
		if (typeof(T).IsClass)
		{
			// Note: the loop starts at (targetCapacity - Capacity), not at Capacity. It can replace existing items.
			for (int i = targetCapacity - Capacity; i < targetCapacity; i++)
			{
				_items[i] = Activator.CreateInstance<T>();
			}
		}
		Capacity = targetCapacity;
	}

	public void Add(T item)
	{
		if (Count == Capacity)
		{
			EnsureCapacity(Capacity + GrowFactor);
		}
		_items[Count++] = item;
	}

	public void AddRange(IEnumerable<T> items)
	{
		if (items is T[] array)
		{
			AddRange(array);
		}
		else if (items is IReadOnlyCollection<T> collection)
		{
			AddRange(collection);
		}
		else
		{
			AddRange(items.ToArray());
		}
	}

	public void AddRange(IReadOnlyCollection<T> items)
	{
		int count = items.Count;
		int freeSlots = Capacity - Count;
		int missingSlots = count - freeSlots;
		if (missingSlots > 0)
		{
			// Note: this adds the number of grow steps, not the number of slots. The result can be too small.
			int targetCapacity = Capacity + (missingSlots + (GrowFactor - 1)) / GrowFactor;
			EnsureCapacity(targetCapacity);
		}
		foreach (T item in items)
		{
			_items[Count++] = item;
		}
	}

	public void AddRange(T[] items)
	{
		int freeSlots = Capacity - Count;
		int missingSlots = items.Length - freeSlots;
		if (missingSlots > 0)
		{
			// Note: this adds the number of grow steps, not the number of slots. The result can be too small.
			int targetCapacity = Capacity + (missingSlots + (GrowFactor - 1)) / GrowFactor;
			EnsureCapacity(targetCapacity);
		}
		Array.Copy(items, 0, _items, Count, items.Length);
		Count += items.Length;
	}

	public void Clear()
	{
		Count = 0;
	}
}
