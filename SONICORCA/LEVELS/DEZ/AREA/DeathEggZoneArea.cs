using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Resources;

namespace SONICORCA.LEVELS.DEZ.AREA;

/// <summary>
/// The area for Death Egg Zone. It loads the tile set, the map and the binding.
/// The level bounds are the bounds of the map.
/// </summary>
public class DeathEggZoneArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	/// <summary>
	/// Not used. This area does not set the level music and does not load this resource.
	/// </summary>
	private const string LevelMusicResourceKey = "SONICORCA/MUSIC/LEVELS/DEZ";

	public static IReadOnlyList<string> AreaDependencies = new string[]
	{
		TileSetResourceKey,
		MapResourceKey,
		BindingResourceKey
	}.ToArray();

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
			_level.TileSet = _resourceTree.GetLoadedResource<TileSet>(this, TileSetResourceKey);
			_level.LoadMap(_resourceTree.GetLoadedResource<LevelMap>(this, MapResourceKey));
			_level.LoadBinding(_resourceTree.GetLoadedResource<LevelBinding>(this, BindingResourceKey));
		}
		_level.Bounds = _level.Map.Bounds;
	}
}
