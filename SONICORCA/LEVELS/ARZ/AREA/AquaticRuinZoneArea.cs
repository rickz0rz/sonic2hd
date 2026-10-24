using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SONICORCA.LEVELS.ARZ.AREA;

/// <summary>
/// The area for Aquatic Ruin Zone. It loads the tile set, the map and the binding, and sets the level music. The game
/// data has the tiles of both acts, but no objects and no music. A map from "s2hd-data import-level arz" adds the
/// collision, the objects, and the markers for the start positions, the act bounds and the water. Without the
/// markers, the area keeps its old behaviour: the map bounds, and water in act 1 only.
/// </summary>
public class AquaticRuinZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string LevelMusicResourceKey = "SONICORCA/MUSIC/LEVELS/ARZ";

	/// <summary>
	/// The top of the act 1 water area, in level units.
	/// </summary>
	private const int Act1WaterTop = 4096;

	/// <summary>
	/// The width and the height of the act 1 water area, in level units.
	/// </summary>
	private const int Act1WaterWidth = 65536;
	private const int Act1WaterHeight = 4096;

	/// <summary>
	/// The animals that destroyed badniks set free. The original has a penguin and Flicky. The game data has no
	/// penguin, so Locky takes its place.
	/// </summary>
	private static IReadOnlyList<string> AnimalResourceKeys = new string[]
	{
		"SONICORCA/OBJECTS/FLICKY",
		"SONICORCA/OBJECTS/LOCKY"
	};

	// AnimalResourceKeys must stay above AreaDependencies, because static fields are set in the order of the source.
	public static IReadOnlyList<string> AreaDependencies = new string[]
	{
		TileSetResourceKey,
		MapResourceKey,
		BindingResourceKey,
		LevelMusicResourceKey
	}.Concat(AnimalResourceKeys).ToArray();

	private SonicOrcaGameContext _gameContext;
	private ResourceTree _resourceTree;
	private Level _level;

	public AquaticRuinZoneArea()
		: base(AreaDependencies)
	{
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_resourceTree = _gameContext.ResourceTree;
		_level = level;
		_level.Name = "Aquatic Ruin";
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
		if (_level.TryGetMarker($"bounds_{act}_lt", out LevelMarker leftTop) && _level.TryGetMarker($"bounds_{act}_rb", out LevelMarker rightBottom))
		{
			// A map from "s2hd-data import-level arz": the start, the bounds and the water of each act
			_level.SetStartPosition("startpos_stk_" + act);
			_level.Bounds = Rectanglei.FromLTRB(leftTop.Position.X, leftTop.Position.Y, rightBottom.Position.X, rightBottom.Position.Y);
			if (_level.TryGetMarker("water_" + act, out LevelMarker water))
			{
				_level.WaterManager.Enabled = true;
				// Blue-green water, like the underwater palette of the original
				_level.WaterManager.DarkColour = new Colour(255, 0, 16, 48);
				_level.WaterManager.MiddleColour = new Colour(255, 0, 136, 160);
				_level.WaterManager.LightColour = new Colour(255, 160, 232, 224);
				_level.WaterManager.WaterAreas.Clear();
				_level.WaterManager.WaterAreas.Add(Rectanglei.FromLTRB(_level.Map.Bounds.Left, water.Position.Y, _level.Map.Bounds.Right, _level.Map.Bounds.Bottom));
			}
			_level.RingsPerfectTarget = _level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(_level.Bounds);
			settings.StartPath = 0;
		}
		else if (act == 1)
		{
			PrepareAct1();
		}
		else if (settings.Seamless)
		{
			SeamlessPrepareAct2();
		}
		else
		{
			PrepareAct2();
		}
	}

	private void PrepareAct1()
	{
		_level.SetStartPosition("startpos_stk_1");
		_level.Bounds = _level.Map.Bounds;
		_level.WaterManager.WaterAreas.Add(new Rectanglei(0, Act1WaterTop, Act1WaterWidth, Act1WaterHeight));
	}

	private void PrepareAct2()
	{
		_level.SetStartPosition("startpos_stk_2");
		_level.Bounds = _level.Map.Bounds;
	}

	private void SeamlessPrepareAct2()
	{
		ExtendSeamlessLevelBounds(_level, _level.Map.Bounds);
	}
}
