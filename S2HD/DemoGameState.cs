// Decompiled with JetBrains decompiler
// Type: S2HD.DemoGameState
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Input;
using SonicOrca.Resources;

#nullable disable
namespace S2HD;

internal class DemoGameState : IGameState, IDisposable
{
	private readonly S2HDSonicOrcaGameContext _gameContext;
	private readonly string _demoResourceKey;
	private LevelPrepareSettings _prepareSettings;
	private LevelGameState _levelGameState;
	private int _demoLength = 1800;

	public DemoGameState(S2HDSonicOrcaGameContext gameContext, string demoResourceKey)
	{
		_gameContext = gameContext;
		_demoResourceKey = demoResourceKey;
	}

	private void StartLevel(LevelPrepareSettings prepareSettings)
	{
		_prepareSettings = prepareSettings;
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
	label_1:
		foreach (UpdateResult updateResult in _levelGameState.Update())
		{
			if (!finishingDemo && ShouldFinishDemo())
			{
				finishingDemo = true;
				_levelGameState.Level?.FadeOut(LevelState.Quit);
			}
			yield return updateResult;
		}
		if (_levelGameState.Completed)
			goto label_1;
	}

	private bool ShouldFinishDemo()
	{
		Level level = _levelGameState.Level;
		if (level != null && level.Ticks >= _demoLength)
			return true;
		InputState pressed = _gameContext.Input.Pressed;
		return pressed.Keyboard[40] || pressed.GamePad[0].Start;
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
