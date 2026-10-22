using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Extensions;
using SonicOrca.Resources;

namespace SonicOrca.Core.Tiles;

internal class TileSetResourceType : ResourceType
{
	public override string Name => "tileset, xml";

	public override string DefaultExtension => ".tileset.xml";

	public override bool CompressByDefault => true;

	public TileSetResourceType()
		: base(ResourceTypeIdentifier.TileSet)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		XmlDocument xmlDocument = new XmlDocument();
		await Task.Run(() =>
		{
			xmlDocument.Load(e.InputStream);
		});
		XmlNode xmlNode = xmlDocument.SelectSingleNode("tileset");
		IEnumerable<string> enumerable = from x in xmlNode.SelectNodes("textures/texture").OfType<XmlNode>()
			select e.GetAbsolutePath(x.InnerText);
		e.PushDependencies(enumerable);
		TileSet tileSet = new TileSet(e.ResourceTree, enumerable);
		tileSet.Resource = e.Resource;
		IEnumerable<Tile> source = from x in xmlNode.SelectNodes("tiles/tile").OfType<XmlNode>()
			select ParseXmlTile(x, tileSet);
		IEnumerable<TileSequence> source2 = from x in xmlNode.SelectNodes("tiles/tileseq").OfType<XmlNode>()
			select ParseXmlTileSequence(x, tileSet);
		foreach (ITile item in source.Cast<ITile>().Concat(source2.Cast<ITile>()))
		{
			tileSet[item.Id] = item;
		}
		return tileSet;
	}

	private static Tile ParseXmlTile(XmlNode node, TileSet tileSet)
	{
		int id = int.Parse(node.Attributes["id"].Value);
		int defaultTextureId = (node.TryGetAttributeValue("texture", out var value) ? int.Parse(value) : 0);
		int defaultX = (node.TryGetAttributeValue("x", out value) ? int.Parse(value) : 0);
		int defaultY = (node.TryGetAttributeValue("y", out value) ? int.Parse(value) : 0);
		int defaultDelay = (node.TryGetAttributeValue("delay", out value) ? int.Parse(value) : 0);
		float defaultOpacity = (node.TryGetAttributeValue("opacity", out value) ? float.Parse(value) : 1f);
		TileBlendMode blend = TileBlendMode.Alpha;
		if (node.TryGetAttributeValue("blend", out value) && Enum.TryParse<TileBlendMode>(value, ignoreCase: true, out var result))
		{
			blend = result;
		}
		XmlNode[] array = node.SelectNodes("frame").OfType<XmlNode>().ToArray();
		IEnumerable<Tile.Frame> enumerable2;
		if (array.Length == 0)
		{
			IEnumerable<Tile.Frame> enumerable = new Tile.Frame[1]
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
			enumerable2 = enumerable;
		}
		else
		{
			enumerable2 = array.Select((XmlNode x) => ParseXmlTileFrame(x, defaultX, defaultY, defaultTextureId, defaultDelay, defaultOpacity));
		}
		IEnumerable<Tile.Frame> frames = enumerable2;
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
		List<int> list = new List<int>();
		XmlNode[] array = node.SelectNodes("tile").OfType<XmlNode>().ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].TryGetAttributeValue("id", out var value))
			{
				list.Add(int.Parse(value));
			}
		}
		return new TileSequence(tileSet, id, list);
	}
}
