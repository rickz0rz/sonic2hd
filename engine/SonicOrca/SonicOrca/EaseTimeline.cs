using System.Collections.Generic;
using System.Linq;

namespace SonicOrca;

/// <summary>
/// A list of values at points in time. It gives the value at any time by linear interpolation.
/// </summary>
public class EaseTimeline
{
	/// <summary>
	/// A value at a point in time.
	/// </summary>
	public class Entry
	{
		private readonly int _time;

		private readonly double _value;

		public int Time => _time;

		public double Value => _value;

		public Entry(int time, double value)
		{
			_time = time;
			_value = value;
		}

		public override string ToString()
		{
			return $"Time = {_time} Value = {_value}";
		}
	}

	private readonly List<Entry> _entries = new List<Entry>();

	public IList<Entry> Entries => _entries;

	public int Length
	{
		get
		{
			if (_entries.Count <= 0)
			{
				return 0;
			}
			return _entries.Max(x => x.Time);
		}
	}

	public EaseTimeline()
	{
	}

	public EaseTimeline(params Entry[] entries)
		: this((IEnumerable<Entry>)entries)
	{
	}

	public EaseTimeline(IEnumerable<Entry> entries)
	{
		_entries.AddRange(entries);
	}

	/// <summary>
	/// Gets the value at <paramref name="time"/>. Before the first entry, the value comes from the first entry.
	/// After the last entry, the value comes from the last entry.
	/// </summary>
	public double GetValueAt(int time)
	{
		Entry[] sortedEntries = _entries.OrderBy(x => x.Time).ToArray();
		Entry previous = sortedEntries.LastOrDefault(x => x.Time <= time) ?? sortedEntries.First();
		Entry next = sortedEntries.FirstOrDefault(x => x.Time >= time) ?? sortedEntries.Last();
		if (previous == next)
		{
			return previous.Value;
		}
		double slope = (next.Value - previous.Value) / (next.Time - previous.Time);
		return previous.Value + slope * (time - previous.Time);
	}
}
