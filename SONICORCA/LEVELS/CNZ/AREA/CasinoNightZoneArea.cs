using System.Collections.Generic;
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
		BossMusicResourceKey,
		BossObjectResourceKey
	}.Concat(AnimalResourceKeys).ToArray();

	/// <summary>The boss events of act 2 (LevEvents_CNZ2) and the start position of the boss (Obj51).</summary>
	private static readonly BossArenaSettings BossArenaSettings = new BossArenaSettings(0x27C0, 0x62E, 0x2890, 0x2860, 0x28E0, 0x4E0,
		new Vector2i(0x2A46, 0x654), 0x2B20);

	private SonicOrcaGameContext _gameContext;
	private ResourceTree _resourceTree;
	private Level _level;
	private BossArena _bossArena;

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
			_level.LoadMap(_resourceTree.GetLoadedResource<LevelMap>(this, MapResourceKey));
			_level.LoadBinding(_resourceTree.GetLoadedResource<LevelBinding>(this, BindingResourceKey));
		}
		_level.LevelMusic = LevelMusicResourceKey;
		int act = settings.Act == 2 ? 2 : 1;
		_level.CurrentAct = act;
		_bossArena = null;
		if (_level.TryGetMarker($"bounds_{act}_lt", out LevelMarker leftTop) && _level.TryGetMarker($"bounds_{act}_rb", out LevelMarker rightBottom))
		{
			_level.SetStartPosition("startpos_stk_" + act);
			_level.Bounds = Rectanglei.FromLTRB(leftTop.Position.X, leftTop.Position.Y, rightBottom.Position.X, rightBottom.Position.Y);
			if (act == 2 && _level.TryGetMarker("origin_2", out LevelMarker origin))
			{
				_bossArena = new BossArena(_level, origin.Position, BossArenaSettings, this.GetAbsolutePath(BossObjectResourceKey),
					this.GetAbsolutePath(BossMusicResourceKey), this.GetAbsolutePath(LevelMusicResourceKey));
			}
		}
		else
		{
			_level.Bounds = _level.Map.Bounds;
		}
		_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(_level.Bounds);
		settings.StartPath = 0;
	}

	public override void OnUpdate()
	{
		if (_level.StateFlags.HasFlag(LevelStateFlags.Updating))
		{
			_bossArena?.Update();
		}
	}
}
