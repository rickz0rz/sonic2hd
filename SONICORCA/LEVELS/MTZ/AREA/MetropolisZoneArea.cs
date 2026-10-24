using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SONICORCA.LEVELS.MTZ.AREA;

/// <summary>
/// The area for Metropolis Zone. It loads the tile set, the map and the binding, and sets the level music. The game
/// data has the tiles of the three acts, but no collision, no objects and no music. A map from "s2hd-data import-level
/// mtz" adds the collision, the objects, and the markers for the start positions, the act bounds and the act origins.
/// With these markers, act 2 has the boss arena of the original. Without them, the level bounds are the bounds of
/// the map.
/// </summary>
public class MetropolisZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string LevelMusicResourceKey = "SONICORCA/MUSIC/LEVELS/MTZ";
	private const string BossMusicResourceKey = "SONICORCA/MUSIC/BOSS/S2";
	/// <summary>Only a mod from "s2hd-data import-level mtz" has the boss.</summary>
	private const string BossObjectResourceKey = "SONICORCA/OBJECTS/MTZBOSS";

	/// <summary>
	/// The animals that destroyed badniks set free. The original has a monkey and an eagle. The game data has
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

	/// <summary>The boss events of act 2 of act 3 (LevEvents_MTZ3) and the start position of the boss (Obj54).</summary>
	private static readonly BossArenaSettings BossArenaSettings = new BossArenaSettings(0x2980, 0x400, 0x2A80, 0x2AB0, 0x2AB0, 0x400,
		new Vector2i(0x2B50, 0x380), 0x2BF0);

	private SonicOrcaGameContext _gameContext;
	private ResourceTree _resourceTree;
	private Level _level;
	private BossArena _bossArena;

	public MetropolisZoneArea()
		: base(AreaDependencies)
	{
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_resourceTree = _gameContext.ResourceTree;
		_level = level;
		_level.Name = "Metropolis";
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
		int act = Math.Clamp(settings.Act, 1, 3);
		_level.CurrentAct = act;
		_bossArena = null;
		if (_level.TryGetMarker($"bounds_{act}_lt", out LevelMarker leftTop) && _level.TryGetMarker($"bounds_{act}_rb", out LevelMarker rightBottom))
		{
			// A map from "s2hd-data import-level mtz": the start and the bounds of each act
			_level.SetStartPosition("startpos_stk_" + act);
			_level.Bounds = Rectanglei.FromLTRB(leftTop.Position.X, leftTop.Position.Y, rightBottom.Position.X, rightBottom.Position.Y);
			if (act == 3 && _level.TryGetMarker("origin_3", out LevelMarker origin))
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
