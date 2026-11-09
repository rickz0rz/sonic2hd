using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SONICORCA.LEVELS.CNZ.AREA;

/// <summary>
/// The area for Casino Night Zone. It loads the tile set, the map and the binding. The game data has the tiles of
/// both acts, but no collision, no objects and no music. A map from "s2hd-data import-level cnz" adds the collision,
/// the objects and the markers for the start positions and the act bounds. Without the markers, the level bounds are
/// the bounds of the map.
/// </summary>
public class CasinoNightZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	/// <summary>The game data has no music for this zone. A mod can add it.</summary>
	/// <summary>The song of versus mode (MusID_CNZ_2P). It comes from the import of the music.</summary>
	private const string VersusMusicResourceKey = "SONICORCA/MUSIC/LEVELS/CNZ/COMPETITIVE";
	/// <summary>The binding with the two-player object layouts of the original. Only the import of the level makes it.</summary>
	private const string VersusBindingResourceKey = "//BINDING2P";
	private const string LevelMusicResourceKey = "SONICORCA/MUSIC/LEVELS/CNZ";
	private const string BossMusicResourceKey = "SONICORCA/MUSIC/BOSS/S2";
	/// <summary>Only a mod from "s2hd-data import-level cnz" has the boss.</summary>
	private const string BossObjectResourceKey = "SONICORCA/OBJECTS/CNZBOSS";

	/// <summary>
	/// The animals that destroyed badniks set free. The original has a bear and Flicky. The game data has no bear, so
	/// Pocky takes its place.
	/// </summary>
	private static IReadOnlyList<string> AnimalResourceKeys = new string[]
	{
		"SONICORCA/OBJECTS/FLICKY",
		"SONICORCA/OBJECTS/POCKY"
	};

	// AnimalResourceKeys must stay above AreaDependencies, because static fields are set in the order of the source.
	public static IReadOnlyList<string> AreaDependencies = new string[]
	{
		TileSetResourceKey,
		MapResourceKey,
		BindingResourceKey,
		LevelMusicResourceKey,
		VersusMusicResourceKey,
		BossMusicResourceKey,
		BossObjectResourceKey
	}.Concat(AnimalResourceKeys).ToArray();

	/// <summary>The boss events of act 2 (LevEvents_CNZ2) and the start position of the boss (Obj51).</summary>
	private static readonly BossArenaSettings BossArenaSettings = Bosses.CasinoNight.Arena;

	/// <summary>Loads the versus binding with the area, when the import made it.</summary>
	public override IEnumerable<string> GetMoreDependencies(ResourceTree resourceTree)
	{
		string key = this.GetAbsolutePath(VersusBindingResourceKey);
		return resourceTree[key]?.Resource != null ? new[] { key } : Enumerable.Empty<string>();
	}

	private SonicOrcaGameContext _gameContext;
	private ResourceTree _resourceTree;
	private Level _level;
	private BossArena _bossArena;
	private bool _rightWallDone;

	public CasinoNightZoneArea()
		: base(AreaDependencies)
	{
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_resourceTree = _gameContext.ResourceTree;
		_level = level;
		_level.Name = "Casino Night";
		_level.ShowAsZone = true;
		_level.ShowAsAct = true;
		_level.Scheme = LevelScheme.S2;
		_level.AnimalResourceKeys = AnimalResourceKeys.Select(x => this.GetAbsolutePath(x)).ToArray();
		if (!settings.Seamless)
		{
			_level.TileSet = _resourceTree.GetLoadedResource<TileSet>(this, TileSetResourceKey);
			LevelMap map = _resourceTree.GetLoadedResource<LevelMap>(this, MapResourceKey);
			// The map is shared: put back the layout changes of the last play
			LayoutChanges.Reset(map);
			_level.LoadMap(map);
			// Versus mode uses the two-player object layouts of the original (ObjectsManager_Init), when the players chose them
			bool versusLayouts = settings.Versus && settings.VersusTwoPlayerLayouts && _resourceTree[this.GetAbsolutePath(VersusBindingResourceKey)]?.Resource != null;
			_level.LoadBinding(versusLayouts ? _resourceTree.GetLoadedResource<LevelBinding>(this, VersusBindingResourceKey) : GetLevelBinding(_level, BindingResourceKey));
			if (versusLayouts)
			{
				Trace.WriteLine("Casino Night: the two-player object layouts");
			}
		}
		_level.LevelMusic = settings.Versus && _resourceTree[VersusMusicResourceKey]?.Resource != null ? VersusMusicResourceKey : LevelMusicResourceKey;
		int act = settings.Act == 2 ? 2 : 1;
		_level.CurrentAct = act;
		_bossArena = null;
		_rightWallDone = false;
		Rectanglei bounds = _level.Map.Bounds;
		if (_level.TryGetMarker($"bounds_{act}_lt", out LevelMarker leftTop) && _level.TryGetMarker($"bounds_{act}_rb", out LevelMarker rightBottom))
		{
			_level.SetStartPosition("startpos_stk_" + act);
			bounds = Rectanglei.FromLTRB(leftTop.Position.X, leftTop.Position.Y, rightBottom.Position.X, rightBottom.Position.Y);
			// In a seamless change from act 1, the bounds grow to include act 2 when the title card ends, as in the HD zones.
			if (settings.Seamless)
			{
				ExtendSeamlessLevelBounds(_level, bounds);
			}
			else
			{
				_level.Bounds = bounds;
			}
			// Versus mode has no boss: act 2 ends at a signpost, as in the original (LevEvents_xxx2_Routine1)
			if (act == 2 && !settings.Versus && _level.TryGetMarker("origin_2", out LevelMarker origin))
			{
				_bossArena = new BossArena(_level, origin.Position, BossArenaSettings, this.GetAbsolutePath(BossObjectResourceKey),
					this.GetAbsolutePath(BossMusicResourceKey), this.GetAbsolutePath(LevelMusicResourceKey));
			}
		}
		else
		{
			_level.Bounds = bounds;
		}
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(bounds);
		settings.StartPath = 0;
	}

	public override void OnUpdate()
	{
		if (!_level.StateFlags.HasFlag(LevelStateFlags.Updating) || _bossArena == null)
		{
			return;
		}
		_bossArena.Update();
		// The walls of the boss arena (LevEvents_CNZ2): the right wall at the trigger, the left wall at the lock.
		// The boss opens the right wall again when it flees.
		if (_bossArena.Triggered && !_rightWallDone)
		{
			_rightWallDone = true;
			LayoutChanges.Apply(_level, "boss_right_wall");
		}
		if (_bossArena.Locked)
		{
			LayoutChanges.Apply(_level, "boss_left_wall");
		}
		if (_bossArena.Boss?.Fleeing == true)
		{
			LayoutChanges.Undo(_level, "boss_right_wall");
		}
	}
}
