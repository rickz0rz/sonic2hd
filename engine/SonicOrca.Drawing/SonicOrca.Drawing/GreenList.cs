using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Drawing;

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
		if (items is T[] items2)
		{
			AddRange(items2);
		}
		else if (items is IReadOnlyCollection<T> items3)
		{
			AddRange(items3);
		}
		else
		{
			AddRange(items.ToArray());
		}
	}

	public void AddRange(IReadOnlyCollection<T> items)
	{
		int count = items.Count;
		int num = Capacity - Count;
		int num2 = count - num;
		if (num2 > 0)
		{
			int targetCapacity = Capacity + (num2 + (GrowFactor - 1)) / GrowFactor;
			EnsureCapacity(targetCapacity);
		}
		foreach (T item in items)
		{
			_items[Count++] = item;
		}
	}

	public void AddRange(T[] items)
	{
		int num = Capacity - Count;
		int num2 = items.Length - num;
		if (num2 > 0)
		{
			int targetCapacity = Capacity + (num2 + (GrowFactor - 1)) / GrowFactor;
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
