using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using S2HD.Menu;
using SonicOrca;
using SonicOrca.Core;

namespace S2HD;

/// <summary>
/// Plays the levels of the game one after the other, and keeps the lives and the score between levels.
/// It also shows the pause menu when the player pauses a level.
/// </summary>
internal class StoryPlaythroughGameState : IGameState, IDisposable
{
	private readonly S2HDSonicOrcaGameContext _gameContext;
	private LevelPrepareSettings _prepareSettings;
	private LevelGameState _levelGameState;
	private int _levelNumber;
	private OptionsMenu _optionsMenu;
	private bool _optionsMenuShowing;

	public bool QuitViaOptions { get; private set; }

	public StoryPlaythroughGameState(S2HDSonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		_prepareSettings = new LevelPrepareSettings();
		_levelNumber = 1;
	}

	public void StartFrom(LevelPrepareSettings prepareSettings)
	{
		_prepareSettings = prepareSettings;
		_levelGameState = new LevelGameState(_gameContext);
		_levelGameState.PrepareSettings = prepareSettings;
		_levelNumber = prepareSettings.LevelNumber;
		_gameContext.Console.LevelGameState = _levelGameState;
	}

	public IEnumerable<UpdateResult> Update()
	{
		QuitViaOptions = false;
		_optionsMenu = new OptionsMenu(_gameContext);
		_optionsMenu.OnResume += (s, e) => _levelGameState.Level.Unpause();
		_optionsMenu.OnRestart += (s, e) =>
		{
			Level level = _levelGameState.Level;
			if (level.Player.Lives <= 0)
				return;
			level.Unpause();
			level.FadeOut(LevelState.RestartCheckpoint);
			--level.Player.Lives;
		};
		_optionsMenu.OnQuit += (s, e) =>
		{
			Level level = _levelGameState.Level;
			level.Unpause();
			level.FadeOut(LevelState.Quit);
			QuitViaOptions = true;
		};
		Task optionsLoadTask = _optionsMenu.LoadAsync();
		while (!optionsLoadTask.IsCompleted)
			yield return UpdateResult.Next();
		if (_levelGameState == null)
			SetLevel(_levelNumber);
		bool playNextLevel;
		do
		{
			foreach (UpdateResult updateResult in _levelGameState.Update())
			{
				Level level = _levelGameState.Level;
				if (level != null && level.StateFlags.HasFlag(LevelStateFlags.Paused))
				{
					if (!_optionsMenuShowing)
					{
						_optionsMenuShowing = true;
						_optionsMenu.CanRestart = _levelGameState.Player.Lives > 1;
						_optionsMenu.Show();
					}
				}
				else if (_optionsMenuShowing)
				{
					_optionsMenuShowing = false;
					_optionsMenu.Hide();
				}
				else
				{
					_levelGameState.HandleInput();
				}
				_optionsMenu.Update();
				yield return updateResult;
			}
			playNextLevel = _levelGameState.Completed;
			if (playNextLevel)
			{
				_prepareSettings.Lives = _levelGameState.Player.Lives;
				_prepareSettings.Score = _levelGameState.Player.Score;
				_prepareSettings.Continues = _levelGameState.Player.Continues;
				_prepareSettings.Emeralds = _levelGameState.Player.Emeralds;
				_prepareSettings.SpecialStage = _levelGameState.PrepareSettings.SpecialStage;
				if (!LoadNextLevel())
					playNextLevel = false;
			}
		}
		while (playNextLevel);
	}

	public void Draw()
	{
		if (_levelGameState == null)
			return;
		_levelGameState.Draw();
		_optionsMenu.Draw(_gameContext.Renderer);
	}

	private bool LoadNextLevel()
	{
		// A zone with more than two acts (Metropolis) goes on with its next act. The level game state plays act 1
		// and act 2 together.
		LevelInfo levelInfo = Levels.GetAvailableLevels(_gameContext.ResourceTree).Skip(_levelNumber - 1).FirstOrDefault();
		int act = _levelGameState.PrepareSettings.Act;
		if (levelInfo != null && act >= 2 && act < levelInfo.Acts)
		{
			return SetLevel(_levelNumber, act + 1);
		}
		++_levelNumber;
		return SetLevel(_levelNumber);
	}

	/// <summary>
	/// Prepares the level with the number in <see cref="_levelNumber"/>, counted from 1 over the released Sonic 2 levels.
	/// Returns false when there is no such level.
	/// </summary>
	/// <remarks>The <paramref name="index"/> parameter is not used. The callers give the same value as _levelNumber.</remarks>
	private bool SetLevel(int index, int act = 1)
	{
		if (_levelGameState != null)
			_levelGameState.Dispose();
		LevelInfo levelInfo = Levels.GetAvailableLevels(_gameContext.ResourceTree).Skip(_levelNumber - 1).FirstOrDefault();
		if (levelInfo == null)
			return false;
		_levelGameState = new LevelGameState(_gameContext);
		_levelGameState.PrepareSettings = new LevelPrepareSettings()
		{
			AreaResourceKey = Levels.GetAreaResourceKey(levelInfo.Mnemonic),
			Act = act,
			Lives = _prepareSettings.Lives,
			Score = _prepareSettings.Score,
			Continues = _prepareSettings.Continues,
			Emeralds = _prepareSettings.Emeralds,
			SpecialStage = _prepareSettings.SpecialStage,
			Debugging = _prepareSettings.Debugging,
			ProtagonistCharacter = _prepareSettings.ProtagonistCharacter,
			SidekickCharacter = _prepareSettings.SidekickCharacter
		};
		_gameContext.Console.LevelGameState = _levelGameState;
		return true;
	}

	public void Dispose()
	{
		if (_levelGameState == null)
			return;
		_levelGameState.Dispose();
	}
}
