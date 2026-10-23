// Decompiled with JetBrains decompiler
// Type: SONICORCA.LEVELS.ARZ.AREA.AquaticRuinZoneArea
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Resources;

#nullable disable
namespace SONICORCA.LEVELS.ARZ.AREA;

/// <summary>
/// The area for Aquatic Ruin Zone. It loads the tile set, the map and the binding, sets the level music,
/// and adds the water area in act 1.
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

	public static IReadOnlyList<string> AreaDependencies = new string[]
	{
		TileSetResourceKey,
		MapResourceKey,
		BindingResourceKey,
		LevelMusicResourceKey
	}.ToArray();

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
		_level.LevelMusic = LevelMusicResourceKey;
		if (!settings.Seamless)
		{
			_level.TileSet = _resourceTree.GetLoadedResource<TileSet>(this, TileSetResourceKey);
			_level.LoadMap(_resourceTree.GetLoadedResource<LevelMap>(this, MapResourceKey));
			_level.LoadBinding(_resourceTree.GetLoadedResource<LevelBinding>(this, BindingResourceKey));
		}
		if (settings.Act == 1)
		{
			PrepareAct1();
		}
		else if (settings.Act == 2)
		{
			if (settings.Seamless)
			{
				SeamlessPrepareAct2();
			}
			else
			{
				PrepareAct2();
			}
		}
	}

	private void PrepareAct1()
	{
		_level.CurrentAct = 1;
		_level.SetStartPosition("startpos_stk_1");
		_level.Bounds = _level.Map.Bounds;
		_level.WaterManager.WaterAreas.Add(new Rectanglei(0, Act1WaterTop, Act1WaterWidth, Act1WaterHeight));
	}

	private void PrepareAct2()
	{
		_level.CurrentAct = 2;
		_level.SetStartPosition("startpos_stk_2");
		_level.Bounds = _level.Map.Bounds;
	}

	private void SeamlessPrepareAct2()
	{
		_level.CurrentAct = 2;
		ExtendSeamlessLevelBounds(_level, _level.Map.Bounds);
	}
}
