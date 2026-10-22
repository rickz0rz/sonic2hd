using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SonicOrca.Core.Debugging;

/// <summary>
/// Per-frame timing of the main loop, enabled with the environment variable S2HD_PROFILE=1.
/// Logs a summary every few seconds and writes one CSV row per frame to profile.csv in the user data directory.
/// </summary>
public static class FrameProfiler
{
	public enum Segment
	{
		Update,
		Draw,
		GpuFinish,
		Swap,
		Wait
	}

	private const int SummaryFrames = 300;

	private static readonly int SegmentCount = Enum.GetValues(typeof(Segment)).Length;

	public static readonly bool Enabled = Environment.GetEnvironmentVariable("S2HD_PROFILE") == "1";

	private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;

	private static readonly double[][] _window = Enumerable.Range(0, SegmentCount + 1).Select(_ => new double[SummaryFrames]).ToArray();

	private static long _frameStart;

	private static long _lastMark;

	private static readonly double[] _current = new double[SegmentCount];

	private static int _windowCount;

	private static long _frameNumber;

	private static int _gen0;

	private static int _gen1;

	private static int _gen2;

	private static long _allocated;

	private static StreamWriter _csv;

	public static void Initialise(string directory)
	{
		if (!Enabled)
		{
			return;
		}
		string path = Path.Combine(directory, "profile.csv");
		_csv = new StreamWriter(path, append: false, Encoding.ASCII);
		_csv.WriteLine("frame,update_ms,draw_ms,gpu_finish_ms,swap_ms,wait_ms,total_ms,gen0,gen1,gen2,alloc_kb");
		_gen0 = GC.CollectionCount(0);
		_gen1 = GC.CollectionCount(1);
		_gen2 = GC.CollectionCount(2);
		_allocated = GC.GetAllocatedBytesForCurrentThread();
		Trace.WriteLine("Frame profiler enabled: " + path);
	}

	public static void BeginFrame()
	{
		if (!Enabled)
		{
			return;
		}
		_frameStart = _lastMark = Stopwatch.GetTimestamp();
		Array.Clear(_current, 0, _current.Length);
	}

	/// <summary>
	/// Attributes the time since the previous mark to <paramref name="segment"/>.
	/// </summary>
	public static void Mark(Segment segment)
	{
		if (!Enabled)
		{
			return;
		}
		long now = Stopwatch.GetTimestamp();
		_current[(int)segment] += (now - _lastMark) * TicksToMs;
		_lastMark = now;
	}

	public static void EndFrame()
	{
		if (!Enabled || _frameStart == 0)
		{
			return;
		}
		double total = (Stopwatch.GetTimestamp() - _frameStart) * TicksToMs;
		int gen0 = GC.CollectionCount(0);
		int gen1 = GC.CollectionCount(1);
		int gen2 = GC.CollectionCount(2);
		long allocated = GC.GetAllocatedBytesForCurrentThread();
		_frameNumber++;
		_csv?.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:F3},{2:F3},{3:F3},{4:F3},{5:F3},{6:F3},{7},{8},{9},{10:F1}",
			_frameNumber, _current[0], _current[1], _current[2], _current[3], _current[4], total,
			gen0 - _gen0, gen1 - _gen1, gen2 - _gen2, (allocated - _allocated) / 1024.0));
		_gen0 = gen0;
		_gen1 = gen1;
		_gen2 = gen2;
		_allocated = allocated;
		for (int i = 0; i < SegmentCount; i++)
		{
			_window[i][_windowCount] = _current[i];
		}
		_window[SegmentCount][_windowCount] = total;
		if (++_windowCount == SummaryFrames)
		{
			LogSummary();
			_windowCount = 0;
		}
	}

	private static readonly System.Collections.Generic.HashSet<int> CaptureFrames = new System.Collections.Generic.HashSet<int>(
		(Environment.GetEnvironmentVariable("S2HD_CAPTURE_FRAMES") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse));

	/// <summary>
	/// True when a screenshot was requested for this draw (S2HD_CAPTURE_FRAMES=2400,4200), for checking rendering
	/// without a visible window.
	/// </summary>
	public static bool IsCaptureFrame(int drawCount) => CaptureFrames.Count > 0 && CaptureFrames.Contains(drawCount);

	public static long Timestamp() => Enabled ? Stopwatch.GetTimestamp() : 0;

	/// <summary>
	/// Logs an operation that started at <paramref name="start"/> if it took at least <paramref name="thresholdMs"/>.
	/// </summary>
	public static void Note(string operation, long start, double thresholdMs = 0)
	{
		if (!Enabled)
		{
			return;
		}
		double ms = (Stopwatch.GetTimestamp() - start) * TicksToMs;
		if (ms >= thresholdMs)
		{
			Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "PROFILE frame {0}: {1} took {2:F1} ms", _frameNumber + 1, operation, ms));
		}
	}

	public static void Shutdown()
	{
		_csv?.Dispose();
		_csv = null;
	}

	private static void LogSummary()
	{
		var sb = new StringBuilder("PROFILE ");
		for (int i = 0; i <= SegmentCount; i++)
		{
			double[] values = (double[])_window[i].Clone();
			Array.Sort(values);
			string name = i < SegmentCount ? ((Segment)i).ToString() : "Total";
			sb.AppendFormat(CultureInfo.InvariantCulture, "{0} avg {1:F2} p95 {2:F2} max {3:F2} | ", name, values.Average(), values[(int)(values.Length * 0.95)], values[^1]);
		}
		int slowFrames = _window[SegmentCount].Count(x => x > 17.5);
		sb.AppendFormat("frames over 17.5ms: {0}/{1}", slowFrames, SummaryFrames);
		Trace.WriteLine(sb.ToString());
		_csv?.Flush();
	}
}
