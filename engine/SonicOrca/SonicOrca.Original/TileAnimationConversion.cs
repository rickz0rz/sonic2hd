using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SonicOrca.Resources;

namespace SonicOrca.Original;

/// <summary>
/// Moves the frames of the animated tiles of a tile set into an animation group (ANIGROUP), so that the animation
/// editor can change them. The tile sets of the HD zones (Emerald Hill, Chemical Plant and Hill Top) have their
/// frames in the tile set. The importer writes the frames of the other zones into an ANIGROUP already.
/// </summary>
public static class TileAnimationConversion
{
	/// <summary>The width and the height of a tile in the textures.</summary>
	private const int TileSize = 64;

	/// <summary>The key of the animation group, relative to the tile set.</summary>
	private const string AnimationGroupKey = "/ANIGROUP";

	/// <summary>
	/// Converts a tile set. Each tile with two or more frame elements gets an animation in the group, and the tile
	/// refers to it. The tile keeps its blend mode, and its texture and position are those of its first frame. The
	/// other tiles and the tile sequences do not change. Returns null when no tile has frames.
	/// </summary>
	/// <param name="tileSetXml">The XML of the tile set.</param>
	public static (string TileSet, string AnimationGroup)? Convert(string tileSetXml)
	{
		XDocument tileSet = XDocument.Parse(tileSetXml);
		XElement root = tileSet.Root;
		XElement[] animatedTiles = root.Elements("tiles").Elements("tile").Where(tile => tile.Elements("frame").Count() >= 2).ToArray();
		if (animatedTiles.Length == 0)
		{
			return null;
		}
		var animations = new StringBuilder();
		for (int i = 0; i < animatedTiles.Length; i++)
		{
			XElement tile = animatedTiles[i];
			animations.AppendLine($"    <!-- {i}: tile {(string)tile.Attribute("id")} -->");
			animations.AppendLine($"    <animation w=\"{TileSize}\" h=\"{TileSize}\">");
			foreach (XElement frame in tile.Elements("frame"))
			{
				animations.AppendLine("      " + GetAnimationFrame(tile, frame));
			}
			animations.AppendLine("    </animation>");
			XElement first = tile.Elements("frame").First();
			string texture = GetValue(tile, first, "texture", "0");
			tile.Elements("frame").Remove();
			foreach (string name in new[] { "texture", "x", "y", "delay", "opacity" })
			{
				tile.SetAttributeValue(name, null);
			}
			if (texture != "0")
			{
				tile.SetAttributeValue("texture", texture);
			}
			tile.SetAttributeValue("x", GetValue(tile, first, "x", "0"));
			tile.SetAttributeValue("y", GetValue(tile, first, "y", "0"));
			tile.SetAttributeValue("anigroup", AnimationGroupKey);
			tile.SetAttributeValue("animation", i);
		}
		// The textures of the group are the textures of the tile set, so that the tiles use the same texture objects.
		// The group is a child of the tile set, so a relative key gets one more "/".
		IEnumerable<string> textures = root.Elements("textures").Elements("texture").Select(texture => texture.Value.Trim())
			.Select(texture => texture.StartsWith('/') ? "/" + texture : texture);
		string animationGroup = "<?xml version=\"1.0\"?>\n<anigroup>\n  <textures>\n" +
			string.Concat(textures.Select(texture => $"    <texture>{texture}</texture>\n")) +
			"  </textures>\n  <animations>\n" + animations + "  </animations>\n</anigroup>\n";
		return (tileSet.Declaration + "\n" + tileSet.Root, animationGroup.Replace("\r\n", "\n"));
	}

	/// <summary>
	/// Converts every tile set of <paramref name="tree"/> that has animated tiles, and writes the new tile sets and
	/// their animation groups into <paramref name="folder"/>, in the layout of "s2hd-data unpack". Returns the number
	/// of tile sets.
	/// </summary>
	public static int ConvertAll(ResourceTree tree, string folder, Action<string> log)
	{
		int count = 0;
		foreach ((string key, Resource resource) in tree.GetResourceListing().OrderBy(r => r.Key, StringComparer.Ordinal))
		{
			if (resource.Identifier != ResourceTypeIdentifier.TileSet)
			{
				continue;
			}
			string xml;
			using (StreamReader reader = new StreamReader(resource.Source.ReadUncompressed()))
			{
				xml = reader.ReadToEnd();
			}
			if (Convert(xml) is not var (tileSet, animationGroup))
			{
				continue;
			}
			string tileSetFolder = Path.Combine(new[] { folder }.Concat(key.Split('/')).ToArray());
			Directory.CreateDirectory(tileSetFolder);
			File.WriteAllText(Path.Combine(tileSetFolder, "_.tileset.xml"), tileSet);
			File.WriteAllText(Path.Combine(tileSetFolder, "ANIGROUP.anigroup.xml"), animationGroup);
			log($"{key}: {animationGroup.Split("<animation ").Length - 1} animated tiles");
			count++;
		}
		return count;
	}

	/// <summary>A frame of the animation group: the frame of the tile, with the attributes of the tile for the attributes that the frame does not have.</summary>
	private static string GetAnimationFrame(XElement tile, XElement frame)
	{
		var xml = new StringBuilder("<frame");
		string texture = GetValue(tile, frame, "texture", "0");
		if (texture != "0")
		{
			xml.Append($" texture=\"{texture}\"");
		}
		xml.Append($" x=\"{GetValue(tile, frame, "x", "0")}\" y=\"{GetValue(tile, frame, "y", "0")}\"");
		xml.Append($" delay=\"{GetValue(tile, frame, "delay", "0")}\"");
		string opacity = GetValue(tile, frame, "opacity", "1");
		if (float.Parse(opacity, CultureInfo.InvariantCulture) != 1f)
		{
			xml.Append($" opacity=\"{opacity}\"");
		}
		return xml.Append(" />").ToString();
	}

	private static string GetValue(XElement tile, XElement frame, string name, string defaultValue)
	{
		return ((string)frame.Attribute(name) ?? (string)tile.Attribute(name) ?? defaultValue).Trim();
	}
}
