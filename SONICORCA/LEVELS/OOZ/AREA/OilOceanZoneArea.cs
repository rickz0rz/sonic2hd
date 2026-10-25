using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;
using SONICORCA.OBJECTS.OOZOIL;

namespace SONICORCA.LEVELS.OOZ.AREA;

/// <summary>
/// The area for Oil Ocean Zone. It loads the tile set, the map and the binding, and sets the level music. The game
/// data has the tiles of both acts, but no collision, no objects and no music. A map from "s2hd-data import-level
/// ooz" adds the collision, the objects, and the markers for the start positions, the act bounds and the act origins.
/// With these markers, act 2 has the boss arena of the original. Without them, the level bounds are the bounds of
/// the map.
/// </summary>
public class OilOceanZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string LevelMusicResourceKey = "SONICORCA/MUSIC/LEVELS/OOZ";
	private const string BossMusicResourceKey = "SONICORCA/MUSIC/BOSS/S2";
	/// <summary>Only a mod from "s2hd-data import-level ooz" has the boss.</summary>
	private const string BossObjectResourceKey = "SONICORCA/OBJECTS/OOZBOSS";
	/// <summary>Only a mod from "s2hd-data import-level ooz" has the oil.</summary>
	private const string OilObjectResourceKey = "SONICORCA/OBJECTS/OOZOIL";

	/// <summary>The oil line of the original (Oil+y_pos), and the oil line of the boss arena (LevEvents_OOZ2).</summary>
	private const int OilY = 0x758;
	private const int BossOilY = 0x2D8;

	/// <summary>
	/// The animals that destroyed badniks set free. The original has a penguin and a seal. The game data has
	/// neither, so Locky and Wocky take their place.
	/// </summary>
	private static IReadOnlyList<string> AnimalResourceKeys = new string[]
	{
		"SONICORCA/OBJECTS/LOCKY",
		"SONICORCA/OBJECTS/WOCKY"
	};

	// AnimalResourceKeys must stay above AreaDependencies, because static fields are set in the order of the source.
	public static IReadOnlyList<string> AreaDependencies = new string[]
	{
		TileSetResourceKey,
		MapResourceKey,
		BindingResourceKey,
		LevelMusicResourceKey,
		BossMusicResourceKey,
		BossObjectResourceKey,
		OilObjectResourceKey
	}.Concat(AnimalResourceKeys).ToArray();

	/// <summary>The boss events of act 2 (LevEvents_OOZ2) and the start position of the boss (Obj55).</summary>
	private static readonly BossArenaSettings BossArenaSettings = new BossArenaSettings(0x2668, 0x1E0, 0x2880, 0x2880, 0x28C0, 0x1D8,
		new Vector2i(0x2940, 0x2D0), 0x2A20);

	private SonicOrcaGameContext _gameContext;
	private ResourceTree _resourceTree;
	private Level _level;
	private BossArena _bossArena;
	private Vector2i _origin;
	private OOZOilInstance _oil;

	public OilOceanZoneArea()
		: base(AreaDependencies)
	{
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_resourceTree = _gameContext.ResourceTree;
		_level = level;
		_level.Name = "Oil Ocean";
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
		_oil = null;
		_origin = new Vector2i(-1, -1);
		Rectanglei bounds = _level.Map.Bounds;
		if (_level.TryGetMarker($"bounds_{act}_lt", out LevelMarker leftTop) && _level.TryGetMarker($"bounds_{act}_rb", out LevelMarker rightBottom))
		{
			// A map from "s2hd-data import-level ooz": the start and the bounds of each act
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
			// The orange sky: the backdrop colour of the original palette (line 2, colour 0)
			_level.BackdropColour = new Colour(255, 255, 109, 0);
			// The background moves at 1/8 of the camera speed, 80 pixels lower (InitCam_OOZ)
			LevelLayer background = _level.Map.Layers[0];
			background.ParallaxY = 1.0 / 8.0;
			background.OffsetY = -80 * 4;
			if (_level.TryGetMarker("origin_" + act, out LevelMarker actOrigin))
			{
				_origin = actOrigin.Position;
			}
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
		if (!_level.StateFlags.HasFlag(LevelStateFlags.Updating))
		{
			return;
		}
		UpdateOil();
		_bossArena?.Update();
	}

	/// <summary>
	/// Adds the oil (Obj07) on the first update, and raises the oil line when the boss events start.
	/// </summary>
	private void UpdateOil()
	{
		if (_origin.X < 0 || _resourceTree[this.GetAbsolutePath(OilObjectResourceKey)]?.Resource == null)
		{
			return;
		}
		if (_oil == null)
		{
			Vector2i position = new Vector2i(_level.Player.Protagonist.Position.X, _origin.Y + OilY * 4);
			int layer = _level.Map.Layers.IndexOf(_level.Player.Protagonist.Layer);
			_oil = _level.ObjectManager.AddObject(new ObjectPlacement(this.GetAbsolutePath(OilObjectResourceKey), layer, position, new { })) as OOZOilInstance;
		}
		if (_oil != null && _bossArena != null && _bossArena.Triggered)
		{
			_oil.OilY = _origin.Y + BossOilY * 4;
		}
	}
}
