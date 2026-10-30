using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SONICORCA.MODAREA;

/// <summary>
/// The area of a level that a mod adds. The folder SONICORCA/LEVELS/NAME of the level has these resources:
/// <list type="bullet">
/// <item>AREA: the text "#use SONICORCA.MODAREA".</item>
/// <item>TILESET, MAP and BINDING, as in the levels of the game.</item>
/// <item>INFO: an XML file, for example
/// <c>&lt;level name="Sunny Meadow" music="SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT1" start="startpos" /&gt;</c>.</item>
/// </list>
/// With the attribute <c>tileset</c>, for example <c>tileset="SONICORCA/LEVELS/EHZ/TILESET"</c>, the level uses the
/// tile set of another level and needs no TILESET of its own.
/// The game starts the level with --level NAME. The bounds of the level are the bounds of the map, or the rectangle
/// between the markers "bounds_lt" and "bounds_rb" when the map has them.
/// </summary>
public class ModArea : Area
{
	private const string TileSetResourceKey = "//TILESET";
	private const string MapResourceKey = "//MAP";
	private const string BindingResourceKey = "//BINDING";
	private const string InfoResourceKey = "//INFO";
	private const string DefaultStartMarker = "startpos";

	public ModArea()
		: base(new[] { MapResourceKey, BindingResourceKey, InfoResourceKey })
	{
	}

	/// <summary>The tile set, the music and the animals of the info file, so that they load with the level.</summary>
	public override IEnumerable<string> GetMoreDependencies(ResourceTree resourceTree)
	{
		XmlElement info = ReadInfo(resourceTree);
		List<string> keys = new List<string> { GetTileSetKey(info) };
		if (info == null)
			return keys;
		string music = info.GetAttribute("music");
		if (!string.IsNullOrEmpty(music))
			keys.Add(music);
		keys.AddRange(info.GetAttribute("animals").Split(' ', System.StringSplitOptions.RemoveEmptyEntries));
		return keys;
	}

	/// <summary>The key of the tile set: the tile set of the info file, or the TILESET of the level.</summary>
	private string GetTileSetKey(XmlElement info)
	{
		return info?.GetAttribute("tileset") is { Length: > 0 } key ? key : this.GetAbsolutePath(TileSetResourceKey);
	}

	public override void Prepare(Level level, LevelPrepareSettings settings)
	{
		ResourceTree tree = level.GameContext.ResourceTree;
		XmlElement info = ((XmlLoadedResource)tree.GetLoadedResource<ILoadedResource>(this, InfoResourceKey)).XmlDocument.DocumentElement;
		level.Name = info.GetAttribute("name") is { Length: > 0 } name ? name : "Mod";
		level.ShowAsZone = true;
		level.ShowAsAct = true;
		level.Scheme = LevelScheme.S2;
		level.AnimalResourceKeys = info.GetAttribute("animals").Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
		if (!settings.Seamless)
		{
			level.TileSet = tree.GetLoadedResource<TileSet>(GetTileSetKey(info));
			level.LoadMap(tree.GetLoadedResource<LevelMap>(this, MapResourceKey));
			level.LoadBinding(tree.GetLoadedResource<LevelBinding>(this, BindingResourceKey));
		}
		string music = info.GetAttribute("music");
		if (!string.IsNullOrEmpty(music) && tree[music]?.Resource != null)
			level.LevelMusic = music;
		level.CurrentAct = settings.Act;
		level.SetStartPosition(info.GetAttribute("start") is { Length: > 0 } start ? start : DefaultStartMarker);
		level.Bounds = GetBounds(level);
		level.RingsPerfectTarget = level.ObjectManager.ObjectEntryTable.GetRingCountInRegion(level.Bounds);
		settings.StartPath = 0;
	}

	private static Rectanglei GetBounds(Level level)
	{
		LevelMarker topLeft = level.Map.Markers.FirstOrDefault(m => m.Name == "bounds_lt");
		LevelMarker bottomRight = level.Map.Markers.FirstOrDefault(m => m.Name == "bounds_rb");
		if (topLeft == null || bottomRight == null)
			return level.Map.Bounds;
		return new Rectanglei(topLeft.Position.X, topLeft.Position.Y, bottomRight.Position.X - topLeft.Position.X, bottomRight.Position.Y - topLeft.Position.Y);
	}

	/// <summary>Reads the info file of the level from the resources, before the level loads.</summary>
	private XmlElement ReadInfo(ResourceTree resourceTree)
	{
		Resource resource = resourceTree[this.GetAbsolutePath(InfoResourceKey)]?.Resource;
		if (resource == null)
			return null;
		XmlDocument document = new XmlDocument();
		using (Stream stream = resource.Source.ReadUncompressed())
			document.Load(stream);
		return document.DocumentElement;
	}
}
