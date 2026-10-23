using System.Collections.Generic;
using System.Linq;

namespace SonicOrca;

public class EaseTimeline
{
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
			return _entries.Max((Entry x) => x.Time);
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

	public double GetValueAt(int time)
	{
		Entry[] source = _entries.OrderBy((Entry x) => x.Time).ToArray();
		Entry entry = source.LastOrDefault((Entry x) => x.Time <= time);
		Entry entry2 = source.FirstOrDefault((Entry x) => x.Time >= time);
		if (entry == null)
		{
			entry = source.First();
		}
		if (entry2 == null)
		{
			entry2 = source.Last();
		}
		if (entry == entry2)
		{
			return entry.Value;
		}
		double num = (entry2.Value - entry.Value) / (entry2.Time - entry.Time);
		return entry.Value + num * (time - entry.Time);
	}
}
