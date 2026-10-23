// Decompiled with JetBrains decompiler
// Type: S2HD.RootGameState
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using System.Globalization;
using S2HD.Menu;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Input;

#nullable disable
namespace S2HD;

internal class RootGameState : IGameState, IDisposable
{
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
		InputState controller = _gameContext.Input.Pressed;
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
				GamePadInputState gamePadInputState;
				if (!_cmdLineArgs.HasOption("nologos"))
				{
					ShowLogos();
					foreach (UpdateResult updateResult in _currentGameState.Update())
					{
						gamePadInputState = controller.GamePad[0];
						if (!gamePadInputState.Start && !input.Pressed.Keyboard[40])
							yield return updateResult;
						else
							break;
					}
				}
				if (!_cmdLineArgs.HasOption("nologos"))
				{
					ShowTeamLogo();
					foreach (UpdateResult updateResult in _currentGameState.Update())
					{
						gamePadInputState = controller.GamePad[0];
						if (!gamePadInputState.Start && !input.Pressed.Keyboard[40])
							yield return updateResult;
						else
							break;
					}
				}
				ShowTitle();
				foreach (UpdateResult updateResult in _currentGameState.Update())
					yield return updateResult;
				TitleGameState currentGameState = _currentGameState as TitleGameState;
				StoryPlaythroughGameState storyState;
				switch (currentGameState.Result)
				{
					case TitleGameState.ResultType.NewGame:
					case TitleGameState.ResultType.LevelSelect:
						PlayClassic();
						storyState = _currentGameState as StoryPlaythroughGameState;
						if (currentGameState.LevelPrepareSettings != null)
							storyState.StartFrom(currentGameState.LevelPrepareSettings);
						foreach (UpdateResult updateResult in _currentGameState.Update())
							yield return updateResult;
						if (!storyState.QuitViaOptions)
						{
							ShowCredits();
							foreach (UpdateResult updateResult in _currentGameState.Update())
								yield return updateResult;
							break;
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
				storyState = null;
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
		ChangeGameState(new DemoGameState(_gameContext, "SONICORCA/RECORDINGS/EHZ"));
	}

	private void ChangeGameState(IGameState newGameState)
	{
		if (_currentGameState != null)
			_currentGameState.Dispose();
		_currentGameState = newGameState;
	}

	private LevelPrepareSettings GetLevelPrepareSettingsFromCommandLine()
	{
		string[] optionValues1 = _cmdLineArgs.GetOptionValues("level");
		if (optionValues1.Length == 0)
			return null;
		string mnemonic = optionValues1[0];
		string areaResourceKey = Levels.GetAreaResourceKey(mnemonic);
		if (areaResourceKey == null)
			return null;
		int num = Levels.GetLevelIndex(mnemonic);
		if (num == -1)
			num = 1;
		LevelPrepareSettings settingsFromCommandLine = new LevelPrepareSettings()
		{
			Act = 1,
			AreaResourceKey = areaResourceKey,
			Debugging = _cmdLineArgs.HasOption("debug"),
			LevelNumber = num,
			Lives = 3,
			TimeTrial = _cmdLineArgs.HasOption("timetrial")
		};
		string[] optionValues2 = _cmdLineArgs.GetOptionValues("act");
		int result1;
		if (optionValues2.Length != 0 && int.TryParse(optionValues2[0], out result1))
			settingsFromCommandLine.Act = result1;
		string[] optionValues3 = _cmdLineArgs.GetOptionValues("lives");
		if (optionValues3.Length != 0 && int.TryParse(optionValues3[0], out result1))
			settingsFromCommandLine.Lives = result1;
		string[] optionValues4 = _cmdLineArgs.GetOptionValues("protagonist");
		if (optionValues4.Length != 0)
			settingsFromCommandLine.ProtagonistCharacter = RootGameState.GetCharacterType(optionValues4[0]);
		string[] optionValues5 = _cmdLineArgs.GetOptionValues("sidekick");
		if (optionValues5.Length != 0)
			settingsFromCommandLine.SidekickCharacter = RootGameState.GetCharacterType(optionValues5[0]);
		string[] optionValues6 = _cmdLineArgs.GetOptionValues("startpos");
		int result2;
		int result3;
		if (optionValues6.Length >= 2 && int.TryParse(optionValues6[0], out result2) && int.TryParse(optionValues6[1], out result3))
			settingsFromCommandLine.StartPosition = new Vector2i?(new Vector2i(result2, result3));
		string[] optionValues7 = _cmdLineArgs.GetOptionValues("night");
		float result4;
		if (optionValues7.Length != 0 && float.TryParse(optionValues7[0], NumberStyles.Float, CultureInfo.InvariantCulture, out result4))
			settingsFromCommandLine.NightMode = (double)result4;
		string[] optionValues8 = _cmdLineArgs.GetOptionValues("record");
		if (optionValues8.Length != 0)
			settingsFromCommandLine.RecordedPlayWritePath = optionValues8[0];
		string[] optionValues9 = _cmdLineArgs.GetOptionValues("playback");
		if (optionValues9.Length != 0)
			settingsFromCommandLine.RecordedPlayReadPath = optionValues9[0];
		string[] optionValues10 = _cmdLineArgs.GetOptionValues("ghost");
		if (optionValues10.Length != 0)
			settingsFromCommandLine.RecordedPlayGhostReadPath = optionValues10[0];
		return settingsFromCommandLine;
	}

	private static CharacterType GetCharacterType(string characterType)
	{
		for (int index = 0; index < RootGameState.CharacterNames.Length; ++index)
		{
			if (RootGameState.CharacterNames[index].Equals(characterType, StringComparison.OrdinalIgnoreCase))
				return (CharacterType)(index + 1);
		}
		return CharacterType.Null;
	}
}
