using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SONICORCA.LEVELS.MCZ.AREA;

/// <summary>
/// The area for Mystic Cave Zone. It loads the tile set, the map and the binding, and sets the level music. The game
/// data has the tiles of both acts, but no collision, no objects and no music. A map from "s2hd-data import-level
/// mcz" adds the collision, the objects, and the markers for the start positions, the act bounds and the act origins.
/// With these markers, act 2 has the boss arena of the original. Without them, the level bounds are the bounds of
/// the map.
/// </summary>
public class MysticCaveZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string LevelMusicResourceKey = "SONICORCA/MUSIC/LEVELS/MCZ/COOPERATIVE";
	private const string BossMusicResourceKey = "SONICORCA/MUSIC/BOSS/S2";
	/// <summary>Only a mod from "s2hd-data import-level mcz" has the boss.</summary>
	private const string BossObjectResourceKey = "SONICORCA/OBJECTS/MCZBOSS";

	/// <summary>
	/// The animals that destroyed badniks set free. The original has a mouse and a chicken. The game data has
	/// neither, so Ricky and Flicky take their place.
	/// </summary>
	private static IReadOnlyList<string> AnimalResourceKeys = new string[]
	{
		"SONICORCA/OBJECTS/RICKY",
		"SONICORCA/OBJECTS/FLICKY"
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

	/// <summary>The boss events of act 2 (LevEvents_MCZ2) and the start position of the boss (Obj57).</summary>
	private static readonly BossArenaSettings BossArenaSettings = new BossArenaSettings(0x2080, 0x5D0, 0x20F0, 0x20F0, 0x20F0, 0x5C8,
		new Vector2i(0x21A0, 0x560), 0x2240);

	private SonicOrcaGameContext _gameContext;
	private ResourceTree _resourceTree;
	private Level _level;
	private BossArena _bossArena;

	public MysticCaveZoneArea()
		: base(AreaDependencies)
	{
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_resourceTree = _gameContext.ResourceTree;
		_level = level;
		_level.Name = "Mystic Cave";
		_level.ShowAsZone = true;
		_level.ShowAsAct = true;
		_level.Scheme = LevelScheme.S2;
		_level.AnimalResourceKeys = AnimalResourceKeys.Select(x => this.GetAbsolutePath(x)).ToArray();
		_level.LevelMusic = LevelMusicResourceKey;
		if (!settings.Seamless)
		{
			_level.TileSet = _resourceTree.GetLoadedResource<TileSet>(this, TileSetResourceKey);
			_level.LoadMap(_resourceTree.GetLoadedResource<LevelMap>(this, MapResourceKey));
			_level.LoadBinding(_resourceTree.GetLoadedResource<LevelBinding>(this, BindingResourceKey));
		}
		int act = settings.Act == 2 ? 2 : 1;
		_level.CurrentAct = act;
		_bossArena = null;
		Rectanglei bounds = _level.Map.Bounds;
		if (_level.TryGetMarker($"bounds_{act}_lt", out LevelMarker leftTop) && _level.TryGetMarker($"bounds_{act}_rb", out LevelMarker rightBottom))
		{
			// A map from "s2hd-data import-level mcz": the start and the bounds of each act
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
			// The background moves at 1/3 of the camera speed in act 1 and at 1/6 in act 2 (InitCam_MCZ). Act 1 is
			// 64 pixels lower in the map than in the original.
			LevelLayer background = _level.Map.Layers[0];
			background.ParallaxY = act == 1 ? 1.0 / 3.0 : 1.0 / 6.0;
			background.OffsetY = act == 1 ? (320 + 64 / 3) * 4 : 16 * 4;
			if (act == 2 && _level.TryGetMarker("origin_2", out LevelMarker origin))
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
		if (_level.StateFlags.HasFlag(LevelStateFlags.Updating))
		{
			_bossArena?.Update();
		}
	}
}
