using System;
using System.Collections.Generic;
using System.Globalization;
using S2HD.Menu;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.SpecialStage;
using SonicOrca.Geometry;
using SonicOrca.Input;
using SonicOrca.Menu;

namespace S2HD;

/// <summary>
/// The top game state. It runs the other game states in sequence: disclaimer, logos, title screen, and then the
/// item that the player selects on the title screen. Command line options can start a level, the credits, the
/// options or the demo directly.
/// </summary>
internal class RootGameState : IGameState, IDisposable
{
	/// <summary>SDL scancode of the Enter key.</summary>
	private const int EnterScancode = 40;
	private const string DemoResourceKey = "SONICORCA/RECORDINGS/EHZ";

	/// <summary>The character names for the command line. The index plus 1 is the <see cref="CharacterType"/> value.</summary>
	private static readonly string[] CharacterNames = new string[3]
	{
		"sonic",
		"tails",
		"knuckles"
	};
	private readonly S2HDSonicOrcaGameContext _gameContext;
	private readonly CommandLineArguments _cmdLineArgs;
	private IGameState _currentGameState;

	public RootGameState(S2HDSonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		_cmdLineArgs = gameContext.CommandLineArguments;
	}

	public IEnumerable<UpdateResult> Update()
	{
		Initialise();
		InputContext input = _gameContext.Input;
		if (_cmdLineArgs.HasOption("level"))
		{
			LevelPrepareSettings settingsFromCommandLine = GetLevelPrepareSettingsFromCommandLine();
			if (settingsFromCommandLine == null)
			{
				Program.ShowErrorMessageBox("Level does not exist.");
			}
			else
			{
				StoryPlaythroughGameState newGameState = new StoryPlaythroughGameState(_gameContext);
				newGameState.StartFrom(settingsFromCommandLine);
				ChangeGameState(newGameState);
				foreach (UpdateResult updateResult in _currentGameState.Update())
					yield return updateResult;
			}
		}
		else
		{
			if (_cmdLineArgs.HasOption("credits"))
			{
				ShowCredits();
				foreach (UpdateResult updateResult in _currentGameState.Update())
					yield return updateResult;
			}
			else if (_cmdLineArgs.HasOption("options"))
			{
				ShowOptions();
				foreach (UpdateResult updateResult in _currentGameState.Update())
					yield return updateResult;
			}
			if (_cmdLineArgs.HasOption("halfpipe"))
			{
				// Test mode for the 3D special stage, which the demo does not use yet. "--halfpipe 3" plays stage 3.
				string[] values = _cmdLineArgs.GetOptionValues("halfpipe");
				int stage = values.Length > 0 && int.TryParse(values[0], out int number) ? Math.Clamp(number, 1, HalfPipeLayout.StageCount) - 1 : 0;
				ChangeGameState(new ScreenGameState(_gameContext, new HalfPipeSpecialStageScreen(_gameContext, stage)));
				foreach (UpdateResult updateResult in _currentGameState.Update())
					yield return updateResult;
			}
			if (_cmdLineArgs.HasOption("demo"))
			{
				PlayDemo();
				foreach (UpdateResult updateResult in _currentGameState.Update())
					yield return updateResult;
			}
			if (!_cmdLineArgs.HasOption("nologos"))
			{
				ShowDisclaimer();
				foreach (UpdateResult updateResult in _currentGameState.Update())
					yield return updateResult;
			}
			bool shouldQuit = false;
			while (!shouldQuit)
			{
				// The player can skip the logos with Start or Enter.
				if (!_cmdLineArgs.HasOption("nologos"))
				{
					ShowLogos();
					foreach (UpdateResult updateResult in _currentGameState.Update())
					{
						if (input.Pressed.GamePad[0].Start || input.Pressed.Keyboard[EnterScancode])
							break;
						yield return updateResult;
					}
				}
				if (!_cmdLineArgs.HasOption("nologos"))
				{
					ShowTeamLogo();
					foreach (UpdateResult updateResult in _currentGameState.Update())
					{
						if (input.Pressed.GamePad[0].Start || input.Pressed.Keyboard[EnterScancode])
							break;
						yield return updateResult;
					}
				}
				ShowTitle();
				foreach (UpdateResult updateResult in _currentGameState.Update())
					yield return updateResult;
				TitleGameState titleState = _currentGameState as TitleGameState;
				switch (titleState.Result)
				{
					case TitleGameState.ResultType.NewGame:
					case TitleGameState.ResultType.LevelSelect:
						PlayClassic();
						StoryPlaythroughGameState storyState = _currentGameState as StoryPlaythroughGameState;
						if (titleState.LevelPrepareSettings != null)
							storyState.StartFrom(titleState.LevelPrepareSettings);
						foreach (UpdateResult updateResult in _currentGameState.Update())
							yield return updateResult;
						// Show the credits only when the player finished the game, not when the player quit from the pause menu.
						if (!storyState.QuitViaOptions)
						{
							ShowCredits();
							foreach (UpdateResult updateResult in _currentGameState.Update())
								yield return updateResult;
						}
						break;
					case TitleGameState.ResultType.ShowOptions:
						ShowOptions();
						foreach (UpdateResult updateResult in _currentGameState.Update())
							yield return updateResult;
						break;
					case TitleGameState.ResultType.StartDemo:
						PlayDemo();
						foreach (UpdateResult updateResult in _currentGameState.Update())
							yield return updateResult;
						break;
					case TitleGameState.ResultType.Quit:
						shouldQuit = true;
						break;
				}
			}
		}
	}

	public void Draw()
	{
		if (_currentGameState == null)
			return;
		_currentGameState.Draw();
	}

	private void Initialise() => SharedRenderers.Initialise(_gameContext.RenderFactory);

	public void Dispose()
	{
		if (_currentGameState != null)
			_currentGameState.Dispose();
		SharedRenderers.Dispose();
	}

	private void ShowLogos()
	{
		ChangeGameState(new LogosGameState(_gameContext));
	}

	private void ShowTeamLogo()
	{
		ChangeGameState(new TeamLogoGameState(_gameContext));
	}

	private void ShowDisclaimer()
	{
		ChangeGameState(new DisclaimerGameState(_gameContext));
	}

	private void ShowTitle()
	{
		ChangeGameState(new TitleGameState(_gameContext));
	}

	private void PlayClassic()
	{
		ChangeGameState(new StoryPlaythroughGameState(_gameContext));
	}

	private void ShowOptions()
	{
		ChangeGameState(new OptionsGameState(_gameContext));
	}

	private void ShowCredits()
	{
		ChangeGameState(new CreditsGameState(_gameContext));
	}

	private void PlayDemo()
	{
		ChangeGameState(new DemoGameState(_gameContext, DemoResourceKey));
	}

	private void ChangeGameState(IGameState newGameState)
	{
		if (_currentGameState != null)
			_currentGameState.Dispose();
		_currentGameState = newGameState;
	}

	/// <summary>
	/// Makes the level settings from the "--level" option and the related options.
	/// Returns null when the level option has no value or names a level that does not exist.
	/// </summary>
	private LevelPrepareSettings GetLevelPrepareSettingsFromCommandLine()
	{
		string[] levelValues = _cmdLineArgs.GetOptionValues("level");
		if (levelValues.Length == 0)
			return null;
		string mnemonic = levelValues[0];
		string areaResourceKey = Levels.GetAreaResourceKey(mnemonic);
		if (areaResourceKey == null)
			return null;
		int levelNumber = Levels.GetLevelIndex(mnemonic);
		if (levelNumber == -1)
			levelNumber = 1;
		LevelPrepareSettings settingsFromCommandLine = new LevelPrepareSettings()
		{
			Act = 1,
			AreaResourceKey = areaResourceKey,
			Debugging = _cmdLineArgs.HasOption("debug"),
			LevelNumber = levelNumber,
			Lives = 3,
			TimeTrial = _cmdLineArgs.HasOption("timetrial")
		};
		string[] actValues = _cmdLineArgs.GetOptionValues("act");
		if (actValues.Length != 0 && int.TryParse(actValues[0], out int act))
			settingsFromCommandLine.Act = act;
		string[] livesValues = _cmdLineArgs.GetOptionValues("lives");
		if (livesValues.Length != 0 && int.TryParse(livesValues[0], out int lives))
			settingsFromCommandLine.Lives = lives;
		string[] protagonistValues = _cmdLineArgs.GetOptionValues("protagonist");
		if (protagonistValues.Length != 0)
			settingsFromCommandLine.ProtagonistCharacter = GetCharacterType(protagonistValues[0]);
		string[] sidekickValues = _cmdLineArgs.GetOptionValues("sidekick");
		if (sidekickValues.Length != 0)
			settingsFromCommandLine.SidekickCharacter = GetCharacterType(sidekickValues[0]);
		string[] startPositionValues = _cmdLineArgs.GetOptionValues("startpos");
		if (startPositionValues.Length >= 2 && int.TryParse(startPositionValues[0], out int startX) && int.TryParse(startPositionValues[1], out int startY))
			settingsFromCommandLine.StartPosition = new Vector2i(startX, startY);
		string[] nightValues = _cmdLineArgs.GetOptionValues("night");
		if (nightValues.Length != 0 && float.TryParse(nightValues[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float nightMode))
			settingsFromCommandLine.NightMode = nightMode;
		string[] recordValues = _cmdLineArgs.GetOptionValues("record");
		if (recordValues.Length != 0)
			settingsFromCommandLine.RecordedPlayWritePath = recordValues[0];
		string[] playbackValues = _cmdLineArgs.GetOptionValues("playback");
		if (playbackValues.Length != 0)
			settingsFromCommandLine.RecordedPlayReadPath = playbackValues[0];
		string[] ghostValues = _cmdLineArgs.GetOptionValues("ghost");
		if (ghostValues.Length != 0)
			settingsFromCommandLine.RecordedPlayGhostReadPath = ghostValues[0];
		return settingsFromCommandLine;
	}

	private static CharacterType GetCharacterType(string characterType)
	{
		for (int index = 0; index < CharacterNames.Length; ++index)
		{
			if (CharacterNames[index].Equals(characterType, StringComparison.OrdinalIgnoreCase))
				return (CharacterType)(index + 1);
		}
		return CharacterType.Null;
	}
}
