using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Input;
using SonicOrca.Resources;

namespace S2HD;

/// <summary>
/// Plays a recorded demo when the title screen waits. Each zone can have a demo: an input recording at
/// SONICORCA/RECORDINGS/&lt;ZONE&gt; of act 1 with Sonic and Tails (record it with "--level &lt;zone&gt; --record file.rec",
/// or convert the demos of the original with "s2hd-data import-demos"). The demos of the available zones play in turn,
/// as the original plays its demos (DemoLevels). A demo stops after its time is over, at the end
/// of its recording, or when the player presses Enter or Start.
/// </summary>
internal class DemoGameState : IGameState, IDisposable
{
	/// <summary>SDL scancode of the Enter key.</summary>
	private const int EnterScancode = 40;
	/// <summary>
	/// The longest demo in level ticks: the title card (120 ticks), then 1640 ticks of play as in the original
	/// (Demo_Time_left).
	/// </summary>
	private const int MaxDemoLength = 120 + 1640;

	/// <summary>The size of one frame in an input recording (see PlayRecorder).</summary>
	private const int RecordingEntrySize = 40;

	/// <summary>The demo of Emerald Hill Zone, which the game data has.</summary>
	private const string DefaultDemoZone = "ehz";

	/// <summary>The number of demos played since the game started, to choose the next demo.</summary>
	private static int _demosPlayed;

	private readonly S2HDSonicOrcaGameContext _gameContext;
	private LevelGameState _levelGameState;
	private int _demoLength = MaxDemoLength;

	public DemoGameState(S2HDSonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
	}

	/// <summary>The resource key of the demo recording of a zone.</summary>
	public static string GetDemoResourceKey(string zone) => $"SONICORCA/RECORDINGS/{zone.ToUpperInvariant()}";

	/// <summary>The zone of the next demo: the next available zone with a recording, in the order of the game.</summary>
	private string ChooseDemoZone()
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		string[] zones = Levels.GetAvailableLevels(resourceTree)
			.Select(level => level.Mnemonic)
			.Where(zone => resourceTree[GetDemoResourceKey(zone)]?.Resource != null)
			.ToArray();
		if (zones.Length == 0)
		{
			return DefaultDemoZone;
		}
		return zones[_demosPlayed++ % zones.Length];
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
		string zone = ChooseDemoZone();
		string demoResourceKey = GetDemoResourceKey(zone);
		using (ResourceSession resourceSession = new ResourceSession(resourceTree))
		{
			resourceSession.PushDependency(demoResourceKey);
			// Poll across frames instead of blocking with Wait(), which single-threaded runtimes (WebAssembly) forbid
			Task loading = resourceSession.LoadAsync(serial: true);
			while (!loading.IsCompleted)
				yield return UpdateResult.Next();
			loading.GetAwaiter().GetResult();
			demoData = resourceTree.GetLoadedResource<InputRecordingResource>(demoResourceKey).Data;
		}
		_demoLength = Math.Min(MaxDemoLength, demoData.Length / RecordingEntrySize);
		StartLevel(new LevelPrepareSettings()
		{
			Act = 1,
			AreaResourceKey = Levels.GetAreaResourceKey(zone),
			LevelNumber = 1,
			Lives = 3,
			ProtagonistCharacter = CharacterType.Sonic,
			// The demos of the original play with Tails, who follows Sonic
			SidekickCharacter = CharacterType.Tails,
			// The recordings take their routes only with the physics of the original
			Physics = PhysicsMode.Original,
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
		if (level != null && level.Ticks >= _demoLength)
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
