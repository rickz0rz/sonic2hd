using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Extensions;
using SonicOrca.Resources;

namespace SonicOrca.Core.Tiles;

/// <summary>
/// Loads a tile set from an XML resource: the texture list, the tiles with their animation frames, and the tile sequences.
/// </summary>
internal class TileSetResourceType : ResourceType
{
	public override string Name => "tileset, xml";

	public override string DefaultExtension => ".tileset.xml";

	public override bool CompressByDefault => true;

	public TileSetResourceType()
		: base(ResourceTypeIdentifier.TileSet)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default)
	{
		XmlDocument xmlDocument = new XmlDocument();
		await Task.Run(() =>
		{
			xmlDocument.Load(e.InputStream);
		});
		XmlNode xmlNode = xmlDocument.SelectSingleNode("tileset");
		// This query is lazy. PushDependencies and the TileSet constructor both get the same query, not a list.
		IEnumerable<string> texturePaths = from x in xmlNode.SelectNodes("textures/texture").OfType<XmlNode>()
										   select e.GetAbsolutePath(x.InnerText);
		e.PushDependencies(texturePaths);
		TileSet tileSet = new TileSet(e.ResourceTree, texturePaths);
		tileSet.Resource = e.Resource;
		IEnumerable<Tile> tiles = from x in xmlNode.SelectNodes("tiles/tile").OfType<XmlNode>()
								  select ParseXmlTile(x, tileSet);
		IEnumerable<TileSequence> tileSequences = from x in xmlNode.SelectNodes("tiles/tileseq").OfType<XmlNode>()
												  select ParseXmlTileSequence(x, tileSet);
		foreach (ITile tile in tiles.Cast<ITile>().Concat(tileSequences.Cast<ITile>()))
		{
			tileSet[tile.Id] = tile;
		}
		return tileSet;
	}

	private static Tile ParseXmlTile(XmlNode node, TileSet tileSet)
	{
		int id = int.Parse(node.Attributes["id"].Value);
		int defaultTextureId = (node.TryGetAttributeValue("texture", out string value) ? int.Parse(value) : 0);
		int defaultX = (node.TryGetAttributeValue("x", out value) ? int.Parse(value) : 0);
		int defaultY = (node.TryGetAttributeValue("y", out value) ? int.Parse(value) : 0);
		int defaultDelay = (node.TryGetAttributeValue("delay", out value) ? int.Parse(value) : 0);
		float defaultOpacity = (node.TryGetAttributeValue("opacity", out value) ? float.Parse(value) : 1f);
		TileBlendMode blend = TileBlendMode.Alpha;
		if (node.TryGetAttributeValue("blend", out value) && Enum.TryParse(value, ignoreCase: true, out TileBlendMode parsedBlend))
		{
			blend = parsedBlend;
		}
		// A tile without frame elements has one frame that uses the attributes of the tile.
		// A frame element uses the attributes of the tile for the attributes that it does not have.
		XmlNode[] frameNodes = node.SelectNodes("frame").OfType<XmlNode>().ToArray();
		IEnumerable<Tile.Frame> frames;
		if (frameNodes.Length == 0)
		{
			frames = new Tile.Frame[1]
			{
				new Tile.Frame
				{
					TextureId = defaultTextureId,
					X = defaultX,
					Y = defaultY,
					Delay = defaultDelay,
					Opacity = defaultOpacity
				}
			};
		}
		else
		{
			frames = frameNodes.Select(x => ParseXmlTileFrame(x, defaultX, defaultY, defaultTextureId, defaultDelay, defaultOpacity));
		}
		return new Tile(tileSet, id, frames, blend);
	}

	private static Tile.Frame ParseXmlTileFrame(XmlNode node, int defaultX, int defaultY, int defaultTextureId, int defaultDelay, float defaultOpacity)
	{
		string value;
		return new Tile.Frame
		{
			TextureId = (node.TryGetAttributeValue("texture", out value) ? int.Parse(value) : defaultTextureId),
			X = (node.TryGetAttributeValue("x", out value) ? int.Parse(value) : defaultX),
			Y = (node.TryGetAttributeValue("y", out value) ? int.Parse(value) : defaultY),
			Delay = (node.TryGetAttributeValue("delay", out value) ? int.Parse(value) : defaultDelay),
			Opacity = (node.TryGetAttributeValue("opacity", out value) ? float.Parse(value) : defaultOpacity)
		};
	}

	private static TileSequence ParseXmlTileSequence(XmlNode node, TileSet tileSet)
	{
		int id = int.Parse(node.Attributes["id"].Value);
		List<int> tileIds = new List<int>();
		XmlNode[] tileNodes = node.SelectNodes("tile").OfType<XmlNode>().ToArray();
		for (int i = 0; i < tileNodes.Length; i++)
		{
			if (tileNodes[i].TryGetAttributeValue("id", out string value))
			{
				tileIds.Add(int.Parse(value));
			}
		}
		return new TileSequence(tileSet, id, tileIds);
	}
}
