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
/// <item>S2HD_TEST_PLAYER2_PLAYBACK=file: plays an input recording as the input of player 2: the sidekick, or Tails in versus mode.</item>
/// <item>S2HD_TEST_VERSUS_ZOOM=in,out: the zoom of player 1 and player 2 when a versus match starts.</item>
/// <item>S2HD_TEST_KILL_PLAYER2_AT_TICK=300: kills the character of player 2 in versus mode at this level tick.</item>
/// <item>S2HD_TEST_VERSUS_ZONE=3: a versus match starts this zone at once, without the setup screen.</item>
/// <item>S2HD_TEST_VERSUS_SPECIAL_STAGE_SEGMENTS="10:4,3:8,0:2|5:1,2:2,1:0": each versus special stage ends at once with
/// the rings of the next group, one pair of rings for each segment.</item>
/// <item>S2HD_TEST_DEFEAT_BOSS_AT_TICK=900: at this level tick, a boss takes its last hit, to test what happens after the fight.</item>
/// <item>S2HD_TEST_EMERALDS=7: gives the player this number of emeralds when a level starts.</item>
/// <item>S2HD_TEST_SPECIAL_STAGE_AT_TICK=300: at this level tick, goes to the special stage as from a star circle.</item>
/// <item>S2HD_TEST_SPECIAL_STAGE_RESULTS=30,1,1: the special stage goes to its results screen at once, with these rings,
/// the emerald (1) or not (0), and all the rings collected (1) or not (0).</item>
/// <item>S2HD_TEST_EDITOR_INPUT="5:mouse 900 500;6:down left;7:up left": replaces the mouse and keyboard of the level
/// editor with these commands. Each command has the editor frame where it starts (counted over all the times that
/// the editor opens) and one of: "mouse X Y" (1920 x 1080 screen), "down left|right|middle", "up left|right|middle",
/// "wheel N", "hwheel N" (left and right), "keydown SCANCODE", "keyup SCANCODE", "tap SCANCODE" (down for one frame) or "type TEXT". While the
/// editor is open, S2HD_CAPTURE_LEVEL_TICKS uses 10000 plus this frame number. In the special stage, it uses 20000
/// plus the tick of the stage.</item>
/// <item>S2HD_TEST_STORY=1 and S2HD_TEST_LOG=file: plays the story without a player and writes a log (see
/// <see cref="StoryTest"/>).</item>
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

	/// <summary>An input recording for player 2, the sidekick, or null. A test of co-op uses it, because it cannot press keys.</summary>
	public static readonly string Player2Playback = Environment.GetEnvironmentVariable("S2HD_TEST_PLAYER2_PLAYBACK") is { Length: > 0 } path ? path : null;

	/// <summary>
	/// The zoom of each player when a versus match starts: true for zoomed out. Null for the default (both zoomed
	/// out). A test of the zoom uses it, because it cannot press the zoom keys.
	/// </summary>
	public static readonly bool[] VersusZoomedOut = Environment.GetEnvironmentVariable("S2HD_TEST_VERSUS_ZOOM") is { Length: > 0 } zoom
		? zoom.Split(',').Select(value => value.Trim() != "in").ToArray()
		: null;

	/// <summary>The level tick when a boss takes its last hit, or null.</summary>
	public static readonly int? DefeatBossAtTick =
		int.TryParse(Environment.GetEnvironmentVariable("S2HD_TEST_DEFEAT_BOSS_AT_TICK"), out int bossTick) ? bossTick : null;

	/// <summary>The level tick when the character of player 2 dies in versus mode, to test the respawn, or null.</summary>
	public static readonly int? KillPlayer2AtTick =
		int.TryParse(Environment.GetEnvironmentVariable("S2HD_TEST_KILL_PLAYER2_AT_TICK"), out int killTick) ? killTick : null;

	/// <summary>
	/// The item of each versus monitor, from 1 (the extra life of Sonic) to 8 (the teleport), to test the items, or
	/// null for an item from the level tick.
	/// </summary>
	public static readonly int? VersusMonitorItem =
		int.TryParse(Environment.GetEnvironmentVariable("S2HD_TEST_VERSUS_MONITOR_ITEM"), out int item) && item >= 1 && item <= 8 ? item : null;

	public static readonly int? StartRings =
		int.TryParse(Environment.GetEnvironmentVariable("S2HD_TEST_START_RINGS"), out int rings) ? rings : null;

	/// <summary>The emeralds that the player has at the start of a level, to test Super Sonic.</summary>
	public static readonly int? Emeralds =
		int.TryParse(Environment.GetEnvironmentVariable("S2HD_TEST_EMERALDS"), out int emeralds) ? emeralds : null;

	public static readonly int? SpecialStageAtTick =
		int.TryParse(Environment.GetEnvironmentVariable("S2HD_TEST_SPECIAL_STAGE_AT_TICK"), out int tick) ? tick : null;

	/// <summary>The results that the special stage shows at once, to test the results screen, or null.</summary>
	public static readonly (int Rings, bool Emerald, bool Perfect)? SpecialStageResults = ParseSpecialStageResults(Environment.GetEnvironmentVariable("S2HD_TEST_SPECIAL_STAGE_RESULTS"));

	/// <summary>Set when the special stage test hook was used, so that it runs once.</summary>
	public static bool SpecialStageTestDone { get; set; }

	private static (int, bool, bool)? ParseSpecialStageResults(string value)
	{
		string[] parts = value?.Split(',', StringSplitOptions.TrimEntries) ?? Array.Empty<string>();
		return parts.Length == 3 && int.TryParse(parts[0], out int rings) ? (rings, parts[1] == "1", parts[2] == "1") : null;
	}

	/// <summary>The scripted input of the level editor, by editor frame, or null.</summary>
	public static readonly IReadOnlyDictionary<int, string[]> EditorInput = ParseEditorInput(Environment.GetEnvironmentVariable("S2HD_TEST_EDITOR_INPUT"));

	/// <summary>The number of frames that the level editor updated, over all the times that it opened.</summary>
	public static int EditorFrame { get; set; }

	/// <summary>The capture number of an editor frame. It is larger than the level ticks of the tests.</summary>
	public const int EditorCaptureOffset = 10000;

	/// <summary>The capture number of a special stage tick, so that S2HD_CAPTURE_LEVEL_TICKS can also capture the special stage.</summary>
	public const int SpecialStageCaptureOffset = 20000;

	/// <summary>
	/// The capture number of a frame of a versus screen. It is larger than the level ticks of a long level (10 minutes
	/// are 36000 ticks). It is the offset, plus 1000 for each versus screen before it, plus
	/// the frame of the screen. The setup screen and the results screens count.
	/// </summary>
	public const int VersusScreenCaptureOffset = 100000;

	/// <summary>The number of versus screens that started, for <see cref="VersusScreenCaptureOffset"/>.</summary>
	public static int VersusScreensShown { get; set; }

	/// <summary>
	/// The frames after which a versus results screen goes on without input, for tests, or null to wait for Start.
	/// </summary>
	public static readonly int? VersusResultsFrames =
		int.TryParse(Environment.GetEnvironmentVariable("S2HD_TEST_VERSUS_RESULTS_FRAMES"), out int resultsFrames) ? resultsFrames : null;

	/// <summary>The zone that a versus match starts at once, without the setup screen, or null.</summary>
	public static readonly int? VersusZone =
		int.TryParse(Environment.GetEnvironmentVariable("S2HD_TEST_VERSUS_ZONE"), out int versusZone) ? versusZone : null;

	/// <summary>
	/// The rings of each segment of the versus special stages, one list for each stage in the order that they start, or
	/// null to play the stages.
	/// </summary>
	public static readonly IReadOnlyList<(int Player1, int Player2)[]> VersusSpecialStageSegments =
		ParseVersusSegments(Environment.GetEnvironmentVariable("S2HD_TEST_VERSUS_SPECIAL_STAGE_SEGMENTS"));

	/// <summary>The number of versus special stages that used <see cref="VersusSpecialStageSegments"/>.</summary>
	public static int VersusSpecialStagesPlayed { get; set; }

	private static IReadOnlyList<(int, int)[]> ParseVersusSegments(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		return value.Split('|').Select(stage => stage.Split(',').Select(segment =>
		{
			string[] rings = segment.Split(':');
			return (int.Parse(rings[0]), int.Parse(rings[1]));
		}).ToArray()).ToArray();
	}

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
		if (StoryTest.PendingCapture != null)
		{
			filename = StoryTest.PendingCapture;
			StoryTest.PendingCapture = null;
			return true;
		}
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
