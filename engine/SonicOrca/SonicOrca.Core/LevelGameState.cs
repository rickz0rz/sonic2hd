using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Network;
using SonicOrca.Core.SpecialStage;
using SonicOrca.Geometry;
using SonicOrca.Menu;

namespace SonicOrca.Core;

/// <summary>
/// The game state that loads and runs a level. It handles death, restart and act changes.
/// </summary>
public class LevelGameState : IGameState, IDisposable
{
	private const int NetworkNotificationInterval = 3000;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly LevelLoader _levelLoader;

	private Area _area;

	private Level _level;

	private Player _player;

	private bool _draw;

	private bool _finished;

	private bool _areaPaused;

	/// <summary>The special stage while the player plays it, or null.</summary>
	private ScreenGameState _specialStage;

	private int _lastNotificationTickCount;

	private Vector2 _inputDirection;

	private bool _inputAction;

	public LevelPrepareSettings PrepareSettings { get; set; }

	public bool Completed { get; set; }

	public Player Player => _player;

	public Level Level => _level;

	public LevelGameState(SonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		_levelLoader = new LevelLoader(gameContext);
	}

	public void Dispose()
	{
		if (_levelLoader.HasLoadedLevel)
		{
			_levelLoader.UnloadLevel();
		}
		if (_levelLoader.HasLoadedArea)
		{
			_levelLoader.UnloadArea();
		}
	}

	public IEnumerable<UpdateResult> Update()
	{
		if (PrepareSettings == null)
		{
			throw new InvalidOperationException("Prepare settings was not specified.");
		}
		_levelLoader.LoadArea(PrepareSettings);
		while (!_levelLoader.HasLoadedArea)
		{
			yield return UpdateResult.Next();
		}
		_area = _levelLoader.Area;
		_level = _levelLoader.Level;
		_player = _level.Player;
		_player.Lives = PrepareSettings.Lives;
		_player.Score = PrepareSettings.Score;
		_player.Emeralds = PrepareSettings.Emeralds;
		_levelLoader.LoadLevel();
		while (true)
		{
			if (!_levelLoader.HasLoadedLevel)
			{
				yield return UpdateResult.Next();
				continue;
			}
			switch (_level.State)
			{
				case LevelState.Null:
					if (!IsWaitingForNetworkPlayers())
					{
						StartLevel();
					}
					break;
				case LevelState.Playing:
					OnLevelUpdate();
					break;
				case LevelState.Dead:
					OnPlayerDeath();
					break;
				case LevelState.Restart:
				case LevelState.RestartCheckpoint:
					OnPlayerRestart();
					break;
				case LevelState.StageCompleted:
					OnStageComplete();
					break;
				case LevelState.Quit:
					_finished = true;
					break;
				case LevelState.SpecialStage:
					foreach (UpdateResult result in PlaySpecialStage())
					{
						yield return result;
					}
					break;
			}
			yield return UpdateResult.Next();
			if (_finished)
			{
				break;
			}
		}
	}

	public void HandleInput()
	{
		if (_level != null && _level.State == LevelState.Playing)
		{
			_level.HandleInput();
		}
	}

	public void Draw()
	{
		if (_specialStage != null)
		{
			_specialStage.Draw();
			return;
		}
		if (_draw)
		{
			_level.Draw(_gameContext.Renderer);
		}
	}

	/// <summary>
	/// Plays the special stage, then restarts the level at the starpost with no rings, as in Sonic 2. The stage gives
	/// its score. When the player wins the emerald, the next star circle opens the next stage.
	/// </summary>
	private IEnumerable<UpdateResult> PlaySpecialStage()
	{
		var screen = new HalfPipeSpecialStageScreen(_gameContext, PrepareSettings.SpecialStage);
		_specialStage = new ScreenGameState(_gameContext, screen);
		foreach (UpdateResult result in _specialStage.Update())
		{
			yield return result;
		}
		_specialStage.Dispose();
		_specialStage = null;
		if (screen.Results != null)
		{
			_player.GainScore(screen.Results.Score);
		}
		if (screen.Cleared)
		{
			_player.Emeralds = Math.Min(_player.Emeralds + 1, HalfPipeLayout.StageCount);
			PrepareSettings.SpecialStage = (PrepareSettings.SpecialStage + 1) % HalfPipeLayout.StageCount;
		}
		PrepareSettings.Emeralds = _player.Emeralds;
		_level.State = LevelState.RestartCheckpoint;
		ReloadLevel(restartFromBeginning: false);
	}

	private void StartLevel()
	{
		_level.SeamlessAct = _level.CurrentAct == 1 && !PrepareSettings.TimeTrial;
		if (PrepareSettings.TimeTrial)
		{
			_level.Player.Lives = -1;
		}
		_level.Start();
		_draw = true;
	}

	private void OnLevelUpdate()
	{
		NetworkPlayUpdate();
		LevelStateFlags stateFlags = _level.StateFlags;
		if (stateFlags.HasFlag(LevelStateFlags.Paused))
		{
			if (!_areaPaused)
			{
				_areaPaused = true;
				_area.OnPause();
			}
		}
		else
		{
			if (_areaPaused)
			{
				_areaPaused = false;
				_area.OnUnpause();
			}
			_area.OnUpdate();
		}
		_level.Update();
		_level.Animate();
	}

	private void OnPlayerDeath()
	{
		ReloadLevel(false);
	}

	private void OnPlayerRestart()
	{
		bool fromCheckpoint = _level.State == LevelState.RestartCheckpoint;
		ReloadLevel(!fromCheckpoint);
	}

	/// <summary>
	/// Unloads the level and loads it again. In a time trial, or when <paramref name="restartFromBeginning"/> is true,
	/// the time, the rings and the starpost are reset. Otherwise the game ends when the player has no lives.
	/// </summary>
	private void ReloadLevel(bool restartFromBeginning)
	{
		if (_level.PlayRecorder != null)
		{
			if (_level.PlayRecorder.Recording)
			{
				PrepareSettings.RecordedPlayWritePath = null;
			}
			if (_level.PlayRecorder.Playing)
			{
				if (!string.IsNullOrEmpty(PrepareSettings.RecordedPlayReadPath))
				{
					_finished = true;
					return;
				}
				if (!PrepareSettings.TimeTrial && _level.Player.StarpostIndex != -1)
				{
					PrepareSettings.RecordedPlayGhostReadPath = null;
				}
			}
		}
		_draw = false;
		_level.State = LevelState.Null;
		_levelLoader.UnloadLevel();
		PrepareSettings.Seamless = false;
		if (PrepareSettings.TimeTrial || restartFromBeginning)
		{
			_level.Time = 0;
			_level.RingsCollected = 0;
			_level.Player.StarpostIndex = -1;
		}
		else if (_level.Player.Lives == 0)
		{
			_finished = true;
			return;
		}
		_levelLoader.LoadLevel();
	}

	private void OnStageComplete()
	{
		_level.State = LevelState.Null;
		if (PrepareSettings.TimeTrial)
		{
			_draw = false;
			Completed = true;
			_finished = true;
		}
		else if (_level.CurrentAct == 1)
		{
			_level.Time = 0;
			_level.RingsCollected = 0;
			_level.Player.StarpostIndex = -1;
			PrepareSettings.Act++;
			PrepareSettings.Seamless = true;
			_levelLoader.LoadLevel();
		}
		else
		{
			_draw = false;
			_finished = true;
			Completed = true;
		}
	}

	private bool IsWaitingForNetworkPlayers()
	{
		bool result = true;
		NetworkManager networkManager = _gameContext.NetworkManager;
		if (networkManager.NetworkPlay)
		{
			if (networkManager.Hosting)
			{
				networkManager.Server.Level = _level;
				if (networkManager.Server.NetworkPlayers.All(x => x.Ready))
				{
					networkManager.Server.SendPacketToAllClients(new NotifyPacket(PacketType.ReadyToStartLevel));
					result = false;
				}
			}
			else
			{
				networkManager.Client.Level = _level;
				if (networkManager.Client.Ready)
				{
					result = false;
				}
				else if (Environment.TickCount > _lastNotificationTickCount + NetworkNotificationInterval)
				{
					networkManager.Client.SendPacket(new NotifyPacket(PacketType.ReadyToStartLevel));
					_lastNotificationTickCount = Environment.TickCount;
				}
			}
		}
		else
		{
			result = false;
		}
		return result;
	}

	private void NetworkPlayUpdate()
	{
		NetworkManager networkManager = _gameContext.NetworkManager;
		if (networkManager.Hosting)
		{
			int characterId = 1;
			if (!(_inputDirection == _gameContext.Current[0].DirectionLeft) || _inputAction != _gameContext.Current[0].Action1)
			{
				_inputDirection = _gameContext.Current[0].DirectionLeft;
				_inputAction = _gameContext.Current[0].Action1;
				PlayInputPacket packet = new PlayInputPacket(characterId, _inputDirection, _inputAction);
				networkManager.Server.SendPacketToAllClients(packet);
			}
		}
	}
}
