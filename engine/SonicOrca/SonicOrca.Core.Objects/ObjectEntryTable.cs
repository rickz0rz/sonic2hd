using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Objects;

/// <summary>
/// Holds the object entries of a level. Each entry comes from an object placement in the level binding.
/// </summary>
public class ObjectEntryTable : ICollection<ObjectEntry>, IEnumerable<ObjectEntry>, IEnumerable
{
	private readonly List<ObjectEntry> _entries = new List<ObjectEntry>();

	private readonly Level _level;

	public int Count => _entries.Count;

	public bool IsReadOnly => false;

	public ObjectEntryTable(Level level)
	{
		_level = level;
	}

	public void Initialise(LevelBinding binding)
	{
		_entries.Clear();
		foreach (ObjectPlacement objectPlacement in binding.ObjectPlacements)
		{
			Add(objectPlacement);
		}
	}

	public void RemoveFinishedEntries()
	{
		_entries.RemoveAll(x => x.FinishedForever);
	}

	public void Add(ObjectPlacement item)
	{
		Add(new ObjectEntry(_level, item));
	}

	public IEnumerable<ObjectEntry> GetAllInRegion(Rectanglei region)
	{
		return _entries.Where(x => !x.FinishedForever && region.IntersectsWith(x.LifetimeArea));
	}

	public IEnumerable<ObjectEntry> GetAll()
	{
		return _entries;
	}

	public int GetRingCount()
	{
		return _entries.Count(x => x.Type.Classification == ObjectClassification.Ring);
	}

	public int GetRingCountInRegion(Rectanglei region)
	{
		return GetAllInRegion(region).Count(x => x.Type.Classification == ObjectClassification.Ring);
	}

	public override string ToString()
	{
		return $"{_entries.Count} entries, {_entries.Count(x => x.Active != null)} loaded.";
	}

	public void Add(ObjectEntry item)
	{
		_entries.Add(item);
	}

	public void Clear()
	{
		_entries.Clear();
	}

	public bool Contains(ObjectEntry item)
	{
		return _entries.Contains(item);
	}

	public void CopyTo(ObjectEntry[] array, int arrayIndex)
	{
		_entries.CopyTo(array, arrayIndex);
	}

	public bool Remove(ObjectEntry item)
	{
		return _entries.Remove(item);
	}

	public IEnumerator<ObjectEntry> GetEnumerator()
	{
		return _entries.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _entries.GetEnumerator();
	}
}
