using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using S2HD.Menu;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.SpecialStage;
using SonicOrca.Menu;

namespace S2HD.Versus;

/// <summary>
/// A versus match, as the two-player mode of the original. The setup screen chooses a zone. The players play its two
/// acts, with the results after each act and after the zone. When the players played all the zones, the game results
/// show, and the match ends. A game over ends the match early, as in the original.
/// </summary>
internal sealed class VersusGameState : IGameState, IDisposable
{
	/// <summary>The zones of the versus match: the mnemonic of each level.</summary>
	private static readonly string[] ZoneMnemonics = { "ehz", "mcz", "cnz" };

	private const int ActsPerZone = 2;

	private readonly S2HDSonicOrcaGameContext _gameContext;

	private readonly VersusMatch _match;

	private readonly string[] _zoneAreaKeys;

	private IGameState _current;

	private LevelGameState _levelGameState;

	private OptionsMenu _optionsMenu;

	private PausableLevel _pausableLevel;

	private bool _quit;

	/// <summary>The settings of the first act from the command line, or null to start at the setup screen.</summary>
	private LevelPrepareSettings _start;

	public VersusGameState(S2HDSonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		List<LevelInfo> levels = Levels.GetStoryLevels(gameContext.ResourceTree).Concat(Levels.GetOtherLevels(gameContext.ResourceTree)).ToList();
		LevelInfo[] zones = ZoneMnemonics.Select(mnemonic => levels.FirstOrDefault(level => level.Mnemonic == mnemonic)).Where(level => level != null).ToArray();
		_zoneAreaKeys = zones.Select(level => Levels.GetAreaResourceKey(level.Mnemonic, gameContext.ResourceTree)).ToArray();
		_match = new VersusMatch(zones.Select(level => level.Name.ToUpperInvariant()).ToArray())
		{
			// Only the import of Casino Night has the two-player layouts
			HasTwoPlayerLayouts = zones.Any(level => string.Equals(level.Mnemonic, "CNZ", StringComparison.OrdinalIgnoreCase))
		};
	}

	/// <summary>Starts the match with the level of the command line, for example "--level ehz --versus --act 2".</summary>
	public void StartFrom(LevelPrepareSettings settings)
	{
		_start = settings;
	}

	public IEnumerable<UpdateResult> Update()
	{
		_optionsMenu = new OptionsMenu(_gameContext);
		_optionsMenu.OnResume += (s, e) => _levelGameState.Level.Unpause();
		_optionsMenu.OnQuit += (s, e) =>
		{
			Level level = _levelGameState.Level;
			level.Unpause();
			level.FadeOut(LevelState.Quit);
			_quit = true;
		};
		_pausableLevel = new PausableLevel(_optionsMenu);
		Task optionsLoadTask = _optionsMenu.LoadAsync();
		while (!optionsLoadTask.IsCompleted)
			yield return UpdateResult.Next();
		if (_start != null)
		{
			int zoneIndex = Array.IndexOf(_zoneAreaKeys, _start.AreaResourceKey);
			foreach (UpdateResult result in PlayZone(zoneIndex, _start))
				yield return result;
		}
		while (!_quit)
		{
			if (_match.Finished)
			{
				foreach (UpdateResult result in ShowScreen(VersusResultsScreen.ForGame(_gameContext, _match)))
					yield return result;
				Trace.WriteLine($"Versus: the game ends, winner {_match.Winner}");
				yield break;
			}
			VersusSetupScreen setup = new VersusSetupScreen(_gameContext, _match);
			foreach (UpdateResult result in ShowScreen(setup))
				yield return result;
			if (setup.Result is not int zone)
				yield break;
			foreach (UpdateResult result in PlayZone(zone, null))
				yield return result;
		}
	}

	/// <summary>
	/// Plays the acts of a zone, and shows the results after each act and after the zone. A zone that is not one of
	/// the versus zones (for example from the command line) plays its acts but does not count in the match.
	/// </summary>
	private IEnumerable<UpdateResult> PlayZone(int zoneIndex, LevelPrepareSettings start)
	{
		string name = zoneIndex >= 0 ? _match.ZoneNames[zoneIndex] : "VERSUS";
		string areaKey = zoneIndex >= 0 ? _zoneAreaKeys[zoneIndex] : start.AreaResourceKey;
		int firstAct = start?.Act ?? 1;
		VersusZoneResult zone = new VersusZoneResult(name);
		for (int act = firstAct; act <= ActsPerZone; act++)
		{
			LevelPrepareSettings settings = new LevelPrepareSettings
			{
				AreaResourceKey = areaKey,
				Act = act,
				Versus = true,
				ProtagonistCharacter = CharacterType.Sonic,
				SidekickCharacter = CharacterType.Null,
				Lives = _match.Player1Lives,
				Player2Lives = _match.Player2Lives,
				VersusTeleportOnly = _match.TeleportOnly,
				VersusTwoPlayerLayouts = _match.TwoPlayerLayouts,
				Physics = start?.Physics,
				Debugging = start?.Debugging ?? false
			};
			// The test options of the command line are only for the first act
			if (start != null && act == firstAct)
			{
				settings.StartPosition = start.StartPosition;
				settings.RecordedPlayReadPath = start.RecordedPlayReadPath;
				settings.RecordedPlayWritePath = start.RecordedPlayWritePath;
			}
			_levelGameState?.Dispose();
			_levelGameState = new LevelGameState(_gameContext) { PrepareSettings = settings };
			_gameContext.Console.LevelGameState = _levelGameState;
			_current = _levelGameState;
			// A versus act cannot restart: each player comes back at their own star post
			foreach (UpdateResult result in _pausableLevel.Run(_levelGameState, () => false))
				yield return result;
			if (_levelGameState.LoadError != null)
			{
				Program.ShowErrorMessageBox(_levelGameState.LoadError);
				_quit = true;
				yield break;
			}
			if (_quit)
				yield break;
			if (!_levelGameState.Completed)
			{
				// A game over ends the match. The results of all the zones go, and both players have three lives again.
				Trace.WriteLine("Versus: game over, the match starts again");
				_match.Reset();
				yield break;
			}
			Level level = _levelGameState.Level;
			IReadOnlyList<Player> players = level.Players;
			_match.Player1Lives = players[0].Lives;
			_match.Player2Lives = players.Count > 1 ? players[1].Lives : _match.Player2Lives;
			VersusActResult actResult = new VersusActResult(
				VersusPlayerStats.FromPlayer(players[0], level.Time),
				VersusPlayerStats.FromPlayer(players[players.Count - 1], level.Time));
			zone.Acts.Add(actResult);
			Trace.WriteLine($"Versus: act {act} points {actResult.Player1Points} to {actResult.Player2Points}");
			_levelGameState.Dispose();
			_levelGameState = null;
			foreach (UpdateResult result in ShowScreen(VersusResultsScreen.ForAct(_gameContext, name, act, actResult)))
				yield return result;
		}
		// A tie of the acts goes to the special stage of the zone, as in the original: Emerald Hill plays stage 2,
		// Mystic Cave stage 3 and Casino Night stage 4
		if (zoneIndex >= 0 && zone.NeedsTieBreak && zone.Acts.Count == ActsPerZone)
		{
			foreach (UpdateResult result in PlaySpecialStage(zone, zoneIndex + 1))
				yield return result;
		}
		if (zoneIndex >= 0)
		{
			_match.Zones[zoneIndex] = zone;
		}
		Trace.WriteLine($"Versus: zone {name} winner {zone.Winner}");
		foreach (UpdateResult result in ShowScreen(VersusResultsScreen.ForZone(_gameContext, zone, firstAct)))
			yield return result;
	}

	/// <summary>
	/// Plays the special stage that breaks a tie of the acts. Each player plays their own character, and the player
	/// who collects more rings in more segments wins the stage, which counts as one more act of the zone.
	/// </summary>
	private IEnumerable<UpdateResult> PlaySpecialStage(VersusZoneResult zone, int stage)
	{
		Trace.WriteLine($"Versus: the acts of {zone.Name} end in a tie, special stage {stage + 1}");
		HalfPipeSpecialStageScreen specialStage = new HalfPipeSpecialStageScreen(_gameContext, Math.Min(stage, HalfPipeLayout.StageCount - 1), versus: true);
		foreach (UpdateResult result in ShowScreen(specialStage))
			yield return result;
		zone.SpecialStage = new VersusSpecialStageResult(specialStage.VersusSegments.Select(s => (s.Sonic, s.Tails)).ToArray());
		Trace.WriteLine($"Versus: special stage points {zone.SpecialStage.Player1Points} to {zone.SpecialStage.Player2Points}");
		foreach (UpdateResult result in ShowScreen(VersusResultsScreen.ForSpecialStage(_gameContext, zone.Name, zone.SpecialStage)))
			yield return result;
	}

	private IEnumerable<UpdateResult> ShowScreen(Screen screen)
	{
		ScreenGameState state = new ScreenGameState(_gameContext, screen);
		_current = state;
		foreach (UpdateResult result in state.Update())
			yield return result;
		_current = null;
	}

	public void Draw()
	{
		_current?.Draw();
		if (_current == _levelGameState && _levelGameState != null)
		{
			_optionsMenu?.Draw(_gameContext.Renderer);
		}
	}

	public void Dispose()
	{
		_optionsMenu?.Dispose();
		_levelGameState?.Dispose();
	}
}
