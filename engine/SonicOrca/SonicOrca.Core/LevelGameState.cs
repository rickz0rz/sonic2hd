using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Editor;
using SonicOrca.Core.Network;
using SonicOrca.Core.SpecialStage;
using SonicOrca.Geometry;
using SonicOrca.Input;
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

	/// <summary>The start position of the prepare settings before the editor opened the first time.</summary>
	private Vector2i? _startPositionBeforeEditor;

	private bool _editorOpened;

	/// <summary>The level that the editor asked to open, until the area of the old level unloads.</summary>
	private LevelCatalogEntry _levelToOpen;

	public LevelPrepareSettings PrepareSettings { get; set; }

	public bool Completed { get; set; }

	/// <summary>
	/// The error when the area or the level could not load, for example because a resource of a mod is broken, or null.
	/// The update then ends, and the game does not stop.
	/// </summary>
	public string LoadError { get; private set; }

	public Player Player => _player;

	public Level Level => _level;

	public LevelGameState(SonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		_levelLoader = new LevelLoader(gameContext);
	}

	public void Dispose()
	{
		// Keep the changes of the level editor that the user did not save
		_level?.Editor.SaveBackup();
		if (_levelLoader.HasLoadedLevel)
		{
			_levelLoader.UnloadLevel();
		}
		if (_levelLoader.HasLoadedArea)
		{
			_levelLoader.UnloadArea();
		}
	}

	/// <summary>Keeps the error of the loader, and writes it to the log with the area that did not load.</summary>
	private void FailLoad()
	{
		Exception error = _levelLoader.Error;
		LoadError = $"The level {PrepareSettings.AreaResourceKey} could not load: {error.Message}";
		System.Diagnostics.Trace.WriteLine(LoadError);
		System.Diagnostics.Trace.WriteLine(error.ToString());
	}

	public IEnumerable<UpdateResult> Update()
	{
		if (PrepareSettings == null)
		{
			throw new InvalidOperationException("Prepare settings was not specified.");
		}
		// The editor can open another level. The loop then loads the area of that level.
		while (true)
		{
			_levelLoader.LoadArea(PrepareSettings);
			while (!_levelLoader.HasLoadedArea)
			{
				if (_levelLoader.Error != null)
				{
					FailLoad();
					yield break;
				}
				yield return UpdateResult.Next();
			}
			_area = _levelLoader.Area;
			_level = _levelLoader.Level;
			_player = _level.Player;
			_player.Lives = PrepareSettings.Lives;
			_player.SetStartScore(PrepareSettings.Score);
			_player.Continues = PrepareSettings.Continues;
			_player.Emeralds = PrepareSettings.Emeralds;
			_levelLoader.LoadLevel();
			while (true)
			{
				if (!_levelLoader.HasLoadedLevel)
				{
					if (_levelLoader.Error != null)
					{
						FailLoad();
						yield break;
					}
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
					case LevelState.Editing:
						OnEditorUpdate();
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
				if (_finished || _levelToOpen != null)
				{
					break;
				}
			}
			if (_levelToOpen == null)
			{
				break;
			}
			SwitchArea();
		}
	}

	/// <summary>
	/// Unloads the level and the area, and prepares the level that the editor asked for. The editor opens again on the
	/// new level with the same tool. The changes that are not saved go to the backup of the editor.
	/// </summary>
	private void SwitchArea()
	{
		LevelEditor editor = _level.Editor;
		int toolIndex = editor.ToolIndex;
		editor.SaveBackup();
		editor.ResetForNewLevel();
		_draw = false;
		_level.State = LevelState.Null;
		_levelLoader.UnloadLevel();
		_levelLoader.UnloadArea();
		PrepareSettings.AreaResourceKey = _levelToOpen.AreaResourceKey;
		PrepareSettings.LevelNumber = _levelToOpen.StoryNumber;
		PrepareSettings.Act = 1;
		PrepareSettings.StartPosition = null;
		PrepareSettings.Seamless = false;
		PrepareSettings.OpenEditor = true;
		PrepareSettings.EditorToolIndex = toolIndex;
		_startPositionBeforeEditor = null;
		_editorOpened = false;
		_levelToOpen = null;
		_level.Time = 0;
		_level.RingsCollected = 0;
		_level.Player.StarpostIndex = -1;
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
		// Tails plays in the special stage when Tails follows Sonic in the level
		bool withTails = _player.ProtagonistCharacterType == CharacterType.Sonic && _player.SidekickCharacterType == CharacterType.Tails;
		var screen = new HalfPipeSpecialStageScreen(_gameContext, PrepareSettings.SpecialStage, withTails, _player.Emeralds, _player.Score);
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
		if (PrepareSettings.OpenEditor && _gameContext.EditorEnabled)
		{
			PrepareSettings.OpenEditor = false;
			OpenEditor();
			if (PrepareSettings.EditorToolIndex is int toolIndex)
			{
				PrepareSettings.EditorToolIndex = null;
				_level.Editor.ToolIndex = toolIndex;
			}
		}
	}

	/// <summary>Opens the level editor. A recorded play or a network play cannot open it.</summary>
	private void OpenEditor()
	{
		if (_level.PlayRecorder.Playing || _level.PlayRecorder.Recording || _gameContext.NetworkManager.NetworkPlay)
		{
			return;
		}
		if (!_editorOpened)
		{
			_editorOpened = true;
			_startPositionBeforeEditor = PrepareSettings.StartPosition;
		}
		_level.EnterEditor();
	}

	/// <summary>
	/// Updates the level editor. To play, the level starts again from the beginning with the changed objects, at the
	/// start position or at the position that the editor gives.
	/// </summary>
	private void OnEditorUpdate()
	{
		LevelEditor editor = _level.Editor;
		editor.Update();
		if (editor.LevelToOpen != null)
		{
			_levelToOpen = editor.LevelToOpen;
			return;
		}
		if (editor.PlayRequest == EditorPlayRequest.None)
		{
			return;
		}
		PrepareSettings.StartPosition = editor.PlayRequest == EditorPlayRequest.FromCursor ? editor.PlayPosition : _startPositionBeforeEditor;
		_level.State = LevelState.Restart;
	}

	private void OnLevelUpdate()
	{
		if (_gameContext.EditorEnabled && !_gameContext.Console.IsOpen && _gameContext.Input.Pressed.Keyboard[KeyboardState.KEY_F2])
		{
			OpenEditor();
			if (_level.State == LevelState.Editing)
			{
				return;
			}
		}
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
			if (!_level.OriginalPhysics || _level.Player.Continues <= 0)
			{
				_finished = true;
				return;
			}
			// The original goes to the continue screen when the player has continues. This game has no continue
			// screen, so it uses a continue at once and restarts the act from its start.
			_level.Player.UseContinue();
			_level.Time = 0;
			_level.RingsCollected = 0;
			_level.Player.StarpostIndex = -1;
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
