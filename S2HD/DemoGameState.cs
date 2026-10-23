using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Input;
using SonicOrca.Resources;

namespace S2HD;

/// <summary>
/// Plays the recorded Emerald Hill Zone demo that the title screen shows when the player waits.
/// The demo stops after its time is over, or when the player presses Enter or Start.
/// </summary>
internal class DemoGameState : IGameState, IDisposable
{
	/// <summary>SDL scancode of the Enter key.</summary>
	private const int EnterScancode = 40;
	/// <summary>The length of the demo in level ticks (30 seconds at 60 ticks each second).</summary>
	private const int DemoLength = 1800;

	private readonly S2HDSonicOrcaGameContext _gameContext;
	private readonly string _demoResourceKey;
	private LevelGameState _levelGameState;

	public DemoGameState(S2HDSonicOrcaGameContext gameContext, string demoResourceKey)
	{
		_gameContext = gameContext;
		_demoResourceKey = demoResourceKey;
	}

	private void StartLevel(LevelPrepareSettings prepareSettings)
	{
		_levelGameState = new LevelGameState(_gameContext);
		_levelGameState.PrepareSettings = prepareSettings;
		_gameContext.Console.LevelGameState = _levelGameState;
	}

	public IEnumerable<UpdateResult> Update()
	{
		byte[] demoData;
		ResourceTree resourceTree = _gameContext.ResourceTree;
		using (ResourceSession resourceSession = new ResourceSession(resourceTree))
		{
			resourceSession.PushDependency(_demoResourceKey);
			// Poll across frames instead of blocking with Wait(), which single-threaded runtimes (WebAssembly) forbid
			Task loading = resourceSession.LoadAsync(serial: true);
			while (!loading.IsCompleted)
				yield return UpdateResult.Next();
			loading.GetAwaiter().GetResult();
			demoData = resourceTree.GetLoadedResource<InputRecordingResource>(_demoResourceKey).Data;
		}
		StartLevel(new LevelPrepareSettings()
		{
			Act = 1,
			AreaResourceKey = Levels.GetAreaResourceKey("ehz"),
			LevelNumber = 1,
			Lives = 3,
			ProtagonistCharacter = CharacterType.Sonic,
			SidekickCharacter = CharacterType.Null,
			RecordedPlayReadData = demoData
		});
		bool finishingDemo = false;
		do
		{
			foreach (UpdateResult updateResult in _levelGameState.Update())
			{
				if (!finishingDemo && ShouldFinishDemo())
				{
					finishingDemo = true;
					_levelGameState.Level?.FadeOut(LevelState.Quit);
				}
				yield return updateResult;
			}
		}
		while (_levelGameState.Completed);
	}

	private bool ShouldFinishDemo()
	{
		Level level = _levelGameState.Level;
		if (level != null && level.Ticks >= DemoLength)
			return true;
		InputState pressed = _gameContext.Input.Pressed;
		return pressed.Keyboard[EnterScancode] || pressed.GamePad[0].Start;
	}

	public void Draw()
	{
		if (_levelGameState == null)
			return;
		_levelGameState.Draw();
	}

	public void Dispose()
	{
		if (_levelGameState == null)
			return;
		_levelGameState.Dispose();
	}
}
