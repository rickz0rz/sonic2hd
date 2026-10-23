// Decompiled with JetBrains decompiler
// Type: SONICORCA.LEVELS.SCZ.AREA.SkyChaseZoneArea
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Resources;

#nullable disable
namespace SONICORCA.LEVELS.SCZ.AREA;

/// <summary>
/// The area for Sky Chase Zone. It loads the tile set, the map and the binding, and sets the level music.
/// The level bounds are the bounds of the map.
/// </summary>
public class SkyChaseZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string LevelMusicResourceKey = "SONICORCA/MUSIC/LEVELS/SCZ";

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

	public SkyChaseZoneArea()
		: base(AreaDependencies)
	{
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_resourceTree = _gameContext.ResourceTree;
		_level = level;
		_level.Name = "Sky Chase";
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
		_level.Bounds = _level.Map.Bounds;
	}
}
