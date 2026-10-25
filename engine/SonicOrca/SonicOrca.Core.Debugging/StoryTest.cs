using System;
using System.Globalization;
using System.IO;
using System.Linq;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Debugging;

/// <summary>
/// A test mode that plays the story from start to end without a player (S2HD_TEST_STORY=1). It tests the joins of
/// the levels: the start of each act, the change from act 1 to act 2, and the change to the next zone.
/// <list type="bullet">
/// <item>At the start of each act, the player does nothing for <see cref="WatchTicks"/> ticks, so the log shows
/// where the player lands. The test takes a screenshot (story-ZONE-ACT.png).</item>
/// <item>Then, when the act has a signpost, the player moves to a point before it and holds right until the act
/// ends. The change to act 2 is then the real change of the game.</item>
/// <item>An act without a signpost ahead of its start (a boss, Sky Chase) ends after <see cref="WatchTicks"/> more
/// ticks. A run that does not reach the signpost in <see cref="RunTicks"/> ticks is logged as stuck, and the act
/// ends too.</item>
/// </list>
/// With S2HD_TEST_LOG, the test writes a log: the start of each act, the state of the player each second, each
/// death, and the end of the story.
/// </summary>
public static class StoryTest
{
	/// <summary>The ticks that the player does nothing at the start of an act, after the title card.</summary>
	public const int WatchTicks = 300;

	/// <summary>The level starts to update at this tick, after the title card.</summary>
	private const int TitleCardTicks = 120;

	/// <summary>The ticks that a run to the signpost can take.</summary>
	public const int RunTicks = 1800;

	/// <summary>The player starts this far before the signpost.</summary>
	private const int SignpostRunDistance = 640;

	/// <summary>The distance below the start point where the test looks for the floor.</summary>
	private const int FloorSearchDistance = 2048;

	/// <summary>The player jumps when it moves less than this in <see cref="StuckTicks"/> ticks.</summary>
	private const int StuckDistance = 32;

	private const int StuckTicks = 60;

	private const string SignpostKey = "SONICORCA/OBJECTS/SIGNPOST";

	public static readonly bool Enabled = Environment.GetEnvironmentVariable("S2HD_TEST_STORY") == "1";

	private static readonly string LogPath = Environment.GetEnvironmentVariable("S2HD_TEST_LOG");

	private static bool _running;

	private static bool _dead;

	private static int _stuckCheckTick;

	private static double _stuckCheckX;

	private static int _startX;

	private static int _runStartTick;

	/// <summary>The name of the next screenshot, or null.</summary>
	public static string PendingCapture { get; set; }

	/// <summary>Writes a line to the log, when S2HD_TEST_LOG is set. The line starts with the time in milliseconds.</summary>
	public static void Log(string message)
	{
		if (string.IsNullOrEmpty(LogPath))
		{
			return;
		}
		File.AppendAllText(LogPath, FormattableString.Invariant($"{Environment.TickCount64} {message}{Environment.NewLine}"));
	}

	private static string GetZone(Level level)
	{
		string key = level.PrepareSettings?.AreaResourceKey ?? "";
		string[] parts = key.Split('/');
		return parts.Length >= 2 ? parts[parts.Length - 2] : key;
	}

	/// <summary>Called when an act starts, also an act that starts seamlessly.</summary>
	public static void OnStart(Level level)
	{
		if (!Enabled)
		{
			return;
		}
		_running = false;
		_dead = false;
		ICharacter protagonist = level.Player.Protagonist;
		_startX = protagonist?.Position.X ?? 0;
		Log(FormattableString.Invariant($"start {GetZone(level)} {level.CurrentAct} x={protagonist?.Position.X} y={protagonist?.Position.Y} seamless={level.PrepareSettings.Seamless}"));
	}

	/// <summary>Called on each update of the level, before the characters move.</summary>
	public static void Update(Level level)
	{
		if (!Enabled)
		{
			return;
		}
		ICharacter protagonist = level.Player.Protagonist;
		if (protagonist == null)
		{
			return;
		}
		int ticks = level.Ticks;
		if (ticks % 60 == 0)
		{
			Log(FormattableString.Invariant($"state {GetZone(level)} {level.CurrentAct} t={ticks} x={protagonist.Position.X} y={protagonist.Position.Y} air={protagonist.IsAirborne} dead={protagonist.IsDead} lives={level.Player.Lives}"));
		}
		if (protagonist.IsDead && !_dead)
		{
			_dead = true;
			Log(FormattableString.Invariant($"death {GetZone(level)} {level.CurrentAct} t={ticks} x={protagonist.Position.X} y={protagonist.Position.Y}"));
		}
		int watchEnd = TitleCardTicks + WatchTicks;
		if (ticks == watchEnd - 1)
		{
			// The screenshot shows where the player landed, before the run moves the player.
			PendingCapture = $"story-{GetZone(level)}-{level.CurrentAct}.png";
		}
		else if (ticks == watchEnd)
		{
			StartRun(level, protagonist);
		}
		else if (ticks == watchEnd + WatchTicks && !_running)
		{
			Log($"complete {GetZone(level)} {level.CurrentAct} (no signpost)");
			EndAct(level);
		}
		else if (_running && ticks == _runStartTick + RunTicks && !level.StateFlags.HasFlag(LevelStateFlags.CompletingStage))
		{
			Log(FormattableString.Invariant($"stuck {GetZone(level)} {level.CurrentAct} x={protagonist.Position.X} y={protagonist.Position.Y}"));
			_running = false;
			EndAct(level);
		}
		if (_running && level.StateFlags.HasFlag(LevelStateFlags.AllowCharacterControl))
		{
			HoldRight(level, protagonist);
		}
	}

	/// <summary>
	/// Ends the act as a level ends after its tally. A boss act of the HD zones ends with the sequence of its area,
	/// which the test does not play, so the test does not use the tally.
	/// </summary>
	private static void EndAct(Level level)
	{
		level.FadeOut(LevelState.StageCompleted);
	}

	/// <summary>Moves the player before the signpost of the act, when the act has one ahead of the start.</summary>
	private static void StartRun(Level level, ICharacter protagonist)
	{
		Rectanglei bounds = level.Bounds;
		ObjectPlacement signpost = level.Binding.ObjectPlacements
			.Where(p => p.Key == SignpostKey && p.Position.X > _startX && p.Position.X <= bounds.Right)
			.OrderBy(p => p.Position.X)
			.FirstOrDefault();
		if (signpost == null)
		{
			return;
		}
		int x = Math.Max(_startX, signpost.Position.X - SignpostRunDistance);
		MoveTo(protagonist, new Vector2i(x, signpost.Position.Y - 256));
		ICharacter sidekick = level.Player.Sidekick;
		if (sidekick != null)
		{
			MoveTo(sidekick, protagonist.Position + new Vector2i(-64, -64));
		}
		level.Camera.CentreObjectToTrack();
		_running = true;
		_runStartTick = level.Ticks;
		_stuckCheckTick = level.Ticks;
		_stuckCheckX = protagonist.Position.X;
		Log(FormattableString.Invariant($"run {GetZone(level)} {level.CurrentAct} from x={protagonist.Position.X} y={protagonist.Position.Y} to signpost x={signpost.Position.X}"));
	}

	/// <summary>
	/// Moves a character to a point above the floor and puts it on the floor. The character leaves the ground
	/// first, so that it does not follow its old ground vector.
	/// </summary>
	private static void MoveTo(ICharacter character, Vector2i position)
	{
		if (character is not Character body)
		{
			character.Position = position;
			return;
		}
		body.LeaveGround();
		body.GroundVelocity = 0.0;
		body.Velocity = new Vector2(0.0, 0.0);
		body.Position = position;
		body.PlaceOnFloor(FloorSearchDistance);
	}

	/// <summary>Holds right. The player jumps when it does not move forward.</summary>
	private static void HoldRight(Level level, ICharacter protagonist)
	{
		CharacterInputState input = protagonist.Input;
		input.Throttle = 1.0;
		input.A = CharacterInputButtonState.Up;
		if (level.Ticks - _stuckCheckTick >= StuckTicks)
		{
			if (protagonist.Position.X - _stuckCheckX < StuckDistance)
			{
				input.A = CharacterInputButtonState.Pressed;
			}
			_stuckCheckTick = level.Ticks;
			_stuckCheckX = protagonist.Position.X;
		}
	}
}
