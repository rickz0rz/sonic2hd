using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SonicOrca.Core.Debugging;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;
using SonicOrca.Input;

namespace SonicOrca.Core;

/// <summary>
/// The rules of versus mode in a level: the zoom of each camera, the end of an act at the signpost and the time
/// limit, the death and the respawn of each player, and the teleport monitor. The level makes the rules after it
/// made the two players and their cameras. See docs/two-player-design.md.
/// </summary>
public sealed class VersusRules
{
	/// <summary>The scale of a camera that is zoomed out: each half shows 2880 x 810 units of the level.</summary>
	private const double ZoomedOutScale = 2.0 / 3.0;

	/// <summary>The time that the other players get after the first player finished: 60 seconds (Loser_Time_Left).</summary>
	private const int TimeLimit = 60 * 60;

	/// <summary>The ticks that the teleport stops the level (Teleport_timer of the original).</summary>
	private const int TeleportTicks = 64;

	/// <summary>The last ticks of the teleport, in which the white flash goes away.</summary>
	private const int TeleportFadeTicks = 16;

	private readonly Level _level;

	/// <summary>True for each player whose camera is zoomed out. A match starts zoomed out.</summary>
	private readonly bool[] _zoomedOut = { true, true };

	/// <summary>The start of each player: the position, the path and the layer.</summary>
	private readonly (Vector2i Position, int Path, int LayerIndex)[] _starts;

	/// <summary>The ticks that the teleport still stops the level, or 0.</summary>
	private int _teleportTicks;

	/// <summary>
	/// The time that the players who did not finish have left, in ticks, after the first player finished the act, or
	/// null.
	/// </summary>
	public int? TimeLeft { get; private set; }

	/// <summary>The opacity of the white flash of the teleport, from 0 to 1.</summary>
	public double TeleportFlash => Math.Min(1.0, _teleportTicks / (double)TeleportFadeTicks);

	/// <summary>
	/// True when a teleport monitor can swap the players. As Obj2E_Init of the original, a monitor gives
	/// invincibility in place of the teleport when a player has finished the act.
	/// </summary>
	public bool CanSwapPlayers => _level.Players.All(player => player.StoppedTime == null);

	/// <param name="starts">The start of each player: the position, the path and the layer index.</param>
	public VersusRules(Level level, (Vector2i Position, int Path, int LayerIndex)[] starts)
	{
		_level = level;
		_starts = starts;
		if (TestHooks.VersusZoomedOut is bool[] zoom)
		{
			for (int i = 0; i < Math.Min(zoom.Length, _zoomedOut.Length); i++)
			{
				_zoomedOut[i] = zoom[i];
			}
		}
		for (int i = 0; i < _zoomedOut.Length; i++)
		{
			ApplyZoom(i);
		}
	}

	/// <summary>
	/// Updates the rules for one tick of the level: the deaths and the time limit. The zoom and the teleport have
	/// their own updates, because they run at other places of the tick.
	/// </summary>
	public void Update()
	{
		Player player2 = _level.Players[1];
		if (TestHooks.KillPlayer2AtTick == _level.Ticks && player2.Protagonist != null)
		{
			player2.Protagonist.Kill(PlayerDeathCause.Hurt);
		}
		UpdateDeaths();
		UpdateTimeLimit();
	}

	#region Zoom

	/// <summary>Gives the camera of a player the scale of their zoom, and centres it on their character again.</summary>
	private void ApplyZoom(int playerIndex)
	{
		Camera camera = _level.Cameras[playerIndex];
		camera.SetScale(_zoomedOut[playerIndex] ? ZoomedOutScale : 1.0);
		camera.CentreObjectToTrack();
	}

	/// <summary>
	/// Each player changes the zoom of their camera with their zoom key (Z for player 1, R for player 2) or the Select
	/// button of their game pad.
	/// </summary>
	public void UpdateZoom()
	{
		if (_level.GameContext.Console.IsOpen)
		{
			return;
		}
		InputState pressed = _level.GameContext.Input.GetInputState(InputStateEventType.Pressed);
		int[] keys = { KeyboardState.KEY_Z, KeyboardState.KEY_R };
		for (int i = 0; i < keys.Length; i++)
		{
			if (pressed.Keyboard.Keys[keys[i]] || pressed.GamePad[i].Select)
			{
				_zoomedOut[i] = !_zoomedOut[i];
				ApplyZoom(i);
			}
		}
	}

	#endregion

	#region End of the act

	/// <summary>
	/// A player finished the act, for example at a signpost: the time of the player stops. The first player who
	/// finishes starts the time limit of the other players. The act ends when all players finished.
	/// </summary>
	public void FinishPlayer(Player player)
	{
		if (player.StoppedTime != null)
		{
			return;
		}
		int time = _level.Time;
		player.StoppedTime = time;
		IReadOnlyList<Player> players = _level.Players;
		int index = players.ToList().IndexOf(player);
		Trace.WriteLine($"Versus: player {index + 1} finished at {time / 60}.{time % 60 * 100 / 60:00} s");
		if (players.All(p => p.StoppedTime != null))
		{
			EndAct();
			return;
		}
		TimeLeft ??= TimeLimit;
	}

	/// <summary>The time limit: when it runs out, the act ends for all players.</summary>
	private void UpdateTimeLimit()
	{
		if (TimeLeft is not int left || !_level.StateFlags.HasFlag(LevelStateFlags.UpdateTime))
		{
			return;
		}
		TimeLeft = left - 1;
		if (TimeLeft <= 0)
		{
			Trace.WriteLine("Versus: the time limit ran out");
			EndAct();
		}
	}

	/// <summary>
	/// The act ends. The level fades out, and the versus match shows the results of the act. Act 2 then starts at its
	/// own start, as in the original, not where the players ended act 1.
	/// </summary>
	private void EndAct()
	{
		TimeLeft = null;
		_level.StateFlags &= ~LevelStateFlags.UpdateTime;
		Trace.WriteLine("Versus: the act ends");
		_level.NotifyActComplete();
		_level.FadeOut(LevelState.StageCompleted);
	}

	#endregion

	#region Teleport

	/// <summary>
	/// The teleport monitor, as swap_players of the original: the two players change places. Each character takes the
	/// position, the layer, the path, the speed and the spin of the other character. Nothing happens when a player is
	/// dead or comes back. The power-ups stay with each player. The level then stops for a short time, and the screen
	/// flashes white.
	/// </summary>
	public void SwapPlayers()
	{
		ICharacter first = _level.Players[0].Protagonist;
		ICharacter second = _level.Players[1].Protagonist;
		if (first == null || second == null || IsOutOfPlay(first) || IsOutOfPlay(second))
		{
			return;
		}
		first.LeaveGround();
		second.LeaveGround();
		// The spin moves the character up or down, so the position changes after the spin
		(first.IsSpinball, second.IsSpinball) = (second.IsSpinball, first.IsSpinball);
		(first.ForceSpinball, second.ForceSpinball) = (second.ForceSpinball, first.ForceSpinball);
		(first.PositionPrecise, second.PositionPrecise) = (second.PositionPrecise, first.PositionPrecise);
		(first.Layer, second.Layer) = (second.Layer, first.Layer);
		(first.Path, second.Path) = (second.Path, first.Path);
		(first.Velocity, second.Velocity) = (second.Velocity, first.Velocity);
		(first.GroundVelocity, second.GroundVelocity) = (second.GroundVelocity, first.GroundVelocity);
		(first.Facing, second.Facing) = (second.Facing, first.Facing);
		foreach (Camera camera in _level.Cameras)
		{
			camera.CentreObjectToTrack();
		}
		_teleportTicks = TeleportTicks;
		_level.SoundManager.PlaySound("SONICORCA/SOUND/SPECIALSTAGETRIGGER");
		Trace.WriteLine("Versus: the players change places");
	}

	private static bool IsOutOfPlay(ICharacter character) => character.IsDead || character.IsDying || character.Respawning;

	/// <summary>Counts down the teleport. Returns true while the teleport stops the level.</summary>
	public bool UpdateTeleport()
	{
		if (_teleportTicks == 0)
		{
			return false;
		}
		_teleportTicks--;
		return true;
	}

	#endregion

	#region Death and respawn

	/// <summary>
	/// As Obj01_ResetLevel_Part2 of the original: a dead player does not restart the level. The player loses a life
	/// and their rings, and comes back at their own star post, or at their start. A player with no lives left ends the
	/// match with the game over of the level.
	/// </summary>
	private void UpdateDeaths()
	{
		IReadOnlyList<Player> players = _level.Players;
		for (int i = 0; i < players.Count; i++)
		{
			Player player = players[i];
			ICharacter character = player.Protagonist;
			if (character == null || !character.IsDead)
			{
				continue;
			}
			if (player.Lives > 0)
			{
				player.Lives--;
			}
			if (player.Lives == 0)
			{
				_level.StartVersusGameOver();
				return;
			}
			// As the original: the rings of the player go, but the total of the act stays for the results
			player.LoseAllRings();
			bool atStarpost = player.StarpostIndex != Player.UndefinedStarpostIndex;
			Vector2i position = atStarpost ? player.StarpostPosition : _starts[i].Position;
			character.Path = atStarpost ? player.StarpostPath : _starts[i].Path;
			int layerIndex = atStarpost && player.StarpostLayerIndex >= 0 ? player.StarpostLayerIndex : _starts[i].LayerIndex;
			if (layerIndex >= 0 && layerIndex < _level.Map.Layers.Count)
			{
				character.Layer = _level.Map.Layers[layerIndex];
			}
			character.Respawn(position);
			Trace.WriteLine($"Versus: player {i + 1} comes back at {position.X}, {position.Y}" + (atStarpost ? $" (star post {player.StarpostIndex})" : " (start)"));
			// The camera of the player goes to the player at once. The original scrolls back, but the HD levels are
			// large, and the scroll would take many seconds.
			_level.Cameras[i].CentreObjectToTrack();
		}
	}

	#endregion
}
