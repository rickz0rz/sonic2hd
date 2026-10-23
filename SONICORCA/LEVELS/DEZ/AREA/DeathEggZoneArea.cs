// Decompiled with JetBrains decompiler
// Type: SONICORCA.LEVELS.DEZ.AREA.DeathEggZoneArea
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Resources;

#nullable disable
namespace SONICORCA.LEVELS.DEZ.AREA;

public class DeathEggZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string LevelMusicResourceKey = "SONICORCA/MUSIC/LEVELS/DEZ";
	public static IReadOnlyList<string> AreaDependencies = (new string[3]
	{
	"//TILESET",
	"//MAP",
	"//BINDING"
	}).ToArray<string>();
	private SonicOrcaGameContext _gameContext;
	private ResourceTree _resourceTree;
	private Level _level;

	public DeathEggZoneArea()
	  : base(AreaDependencies)
	{
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		_gameContext = level.GameContext;
		_resourceTree = _gameContext.ResourceTree;
		_level = level;
		_level.Name = "Death Egg";
		_level.ShowAsZone = true;
		_level.ShowAsAct = true;
		_level.Scheme = LevelScheme.S2;
		if (!settings.Seamless)
		{
			_level.TileSet = _resourceTree.GetLoadedResource<TileSet>(this, "//TILESET");
			_level.LoadMap(_resourceTree.GetLoadedResource<LevelMap>(this, "//MAP"));
			_level.LoadBinding(_resourceTree.GetLoadedResource<LevelBinding>(this, "//BINDING"));
		}
		_level.Bounds = _level.Map.Bounds;
	}
}
