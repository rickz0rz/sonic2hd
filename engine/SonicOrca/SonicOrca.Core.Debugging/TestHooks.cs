using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Core.Debugging;

/// <summary>
/// Hooks for automated tests. Each one is off unless its environment variable is set.
/// <list type="bullet">
/// <item>S2HD_RANDOM_SEED=1: seeds the random number generators of the level and the particle manager.</item>
/// <item>S2HD_CAPTURE_LEVEL_TICKS=60,600: saves a screenshot when the level draws these ticks, counted from the
/// level start. Unlike frame numbers, ticks do not depend on how long the level took to load.</item>
/// <item>S2HD_EXIT_AFTER_CAPTURES=1: quits after the last screenshot.</item>
/// </list>
/// </summary>
public static class TestHooks
{
	private static readonly int? RandomSeed =
		int.TryParse(Environment.GetEnvironmentVariable("S2HD_RANDOM_SEED"), out int seed) ? seed : null;

	private static readonly SortedSet<int> PendingLevelTickCaptures = new SortedSet<int>(
		(Environment.GetEnvironmentVariable("S2HD_CAPTURE_LEVEL_TICKS") ?? "")
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse));

	private static readonly bool HasLevelTickCaptures = PendingLevelTickCaptures.Count > 0;

	public static readonly bool ExitAfterCaptures = Environment.GetEnvironmentVariable("S2HD_EXIT_AFTER_CAPTURES") == "1";

	/// <summary>The level tick drawn in the current frame, or -1 when no level was drawn.</summary>
	public static int DrawnLevelTick { get; set; } = -1;

	public static Random CreateRandom() => RandomSeed is int value ? new Random(value) : new Random();

	/// <summary>
	/// True once for each requested tick, when the frame drew that level tick. Returns the file name to use.
	/// </summary>
	public static bool TakeLevelTickCapture(out string filename)
	{
		filename = null;
		if (DrawnLevelTick < 0 || !PendingLevelTickCaptures.Remove(DrawnLevelTick))
		{
			return false;
		}
		filename = $"level-tick-{DrawnLevelTick:D5}.png";
		return true;
	}

	public static bool AllCapturesTaken => HasLevelTickCaptures && PendingLevelTickCaptures.Count == 0;
}
