using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Core.Debugging;

/// <summary>
/// Hooks for automated tests. Each one is off unless its environment variable is set.
/// <list type="bullet">
/// <item>S2HD_RANDOM_SEED=1: seeds the random number generators of the level and the particle manager.</item>
/// <item>S2HD_PHYSICS=hd or original: plays the levels in this physics mode, whatever the settings say.</item>
/// <item>S2HD_CAPTURE_LEVEL_TICKS=60,600: saves a screenshot when the level draws these ticks, counted from the
/// level start. Unlike frame numbers, ticks do not depend on how long the level took to load.</item>
/// <item>S2HD_EXIT_AFTER_CAPTURES=1: quits after the last screenshot.</item>
/// <item>S2HD_TEST_START_RINGS=60: gives the player this number of rings when a level starts.</item>
/// <item>S2HD_TEST_EMERALDS=7: gives the player this number of emeralds when a level starts.</item>
/// <item>S2HD_TEST_SPECIAL_STAGE_AT_TICK=300: at this level tick, goes to the special stage as from a star circle.</item>
/// <item>S2HD_TEST_EDITOR_INPUT="5:mouse 900 500;6:down left;7:up left": replaces the mouse and keyboard of the level
/// editor with these commands. Each command has the editor frame where it starts (counted over all the times that
/// the editor opens) and one of: "mouse X Y" (1920 x 1080 screen), "down left|right|middle", "up left|right|middle",
/// "wheel N", "keydown SCANCODE", "keyup SCANCODE", "tap SCANCODE" (down for one frame) or "type TEXT". While the
/// editor is open, S2HD_CAPTURE_LEVEL_TICKS uses 10000 plus this frame number.</item>
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

	public static readonly PhysicsMode? Physics =
		Enum.TryParse(Environment.GetEnvironmentVariable("S2HD_PHYSICS"), ignoreCase: true, out PhysicsMode mode) ? mode : null;

	public static readonly bool ExitAfterCaptures = Environment.GetEnvironmentVariable("S2HD_EXIT_AFTER_CAPTURES") == "1";

	public static readonly int? StartRings =
		int.TryParse(Environment.GetEnvironmentVariable("S2HD_TEST_START_RINGS"), out int rings) ? rings : null;

	/// <summary>The emeralds that the player has at the start of a level, to test Super Sonic.</summary>
	public static readonly int? Emeralds =
		int.TryParse(Environment.GetEnvironmentVariable("S2HD_TEST_EMERALDS"), out int emeralds) ? emeralds : null;

	public static readonly int? SpecialStageAtTick =
		int.TryParse(Environment.GetEnvironmentVariable("S2HD_TEST_SPECIAL_STAGE_AT_TICK"), out int tick) ? tick : null;

	/// <summary>Set when the special stage test hook was used, so that it runs once.</summary>
	public static bool SpecialStageTestDone { get; set; }

	/// <summary>The scripted input of the level editor, by editor frame, or null.</summary>
	public static readonly IReadOnlyDictionary<int, string[]> EditorInput = ParseEditorInput(Environment.GetEnvironmentVariable("S2HD_TEST_EDITOR_INPUT"));

	/// <summary>The number of frames that the level editor updated, over all the times that it opened.</summary>
	public static int EditorFrame { get; set; }

	/// <summary>The capture number of an editor frame. It is larger than the level ticks of the tests.</summary>
	public const int EditorCaptureOffset = 10000;

	/// <summary>The level tick drawn in the current frame, or -1 when no level was drawn.</summary>
	public static int DrawnLevelTick { get; set; } = -1;

	public static Random CreateRandom() => RandomSeed is int value ? new Random(value) : new Random();

	/// <summary>A random number generator with the test seed when it is set, otherwise with <paramref name="seed"/>.</summary>
	public static Random CreateRandom(int seed) => new Random(RandomSeed ?? seed);

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

	private static IReadOnlyDictionary<int, string[]> ParseEditorInput(string script)
	{
		if (string.IsNullOrWhiteSpace(script))
		{
			return null;
		}
		return script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(command => command.Split(':', 2))
			.GroupBy(parts => int.Parse(parts[0]), parts => parts[1].Trim())
			.ToDictionary(group => group.Key, group => group.ToArray());
	}
}
