using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Extensions;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// Loads a level map from a map XML file: the layer tree, the collision paths and vectors, and the markers.
/// </summary>
internal class LevelMapResourceType : ResourceType
{
	// Tile value flags. A tile value in the file can start with "H" or "V" to flip the tile.
	private const int FlipHorizontalFlag = 0x4000;

	private const int FlipVerticalFlag = 0x8000;

	// StringReader.Peek returns -1 at the end of the text. As a char, this is '\uffff'.
	private const char EndOfText = '\uffff';

	public override string Name => "map, xml";

	public override string DefaultExtension => ".map.xml";

	public override bool CompressByDefault => true;

	public LevelMapResourceType()
		: base(ResourceTypeIdentifier.Map)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		XmlDocument xmlDocument = new XmlDocument();
		await Task.Run(() =>
		{
			xmlDocument.Load(e.InputStream);
		});
		LevelMap map = new LevelMap();
		map.Resource = e.Resource;
		XmlNode tilesNode = xmlDocument.SelectSingleNode("map/tiles");
		ILevelLayerTreeNode[] layerTreeNodes = GetLevelLayersFromXmlNode(tilesNode, map).ToArray();
		map.LayerTree.Children.Clear();
		map.LayerTree.Children.AddRange(layerTreeNodes);
		// The flat layer list holds the layers of the tree in order, without the groups.
		LevelLayer[] layers = map.LayerTree.GetDescendantsOrdered().OfType<LevelLayer>().ToArray();
		map.Layers.Clear();
		map.Layers.AddRange(layers);
		XmlNode collisionNode = xmlDocument.SelectSingleNode("map/collision");
		if (collisionNode != null)
		{
			map.CollisionPathLayers.AddRange(GetCollisionPathsFromXmlNode(collisionNode.SelectSingleNode("paths")));
			map.CollisionVectors.AddRange(GetCollisionVectorsFromXmlNode(collisionNode.SelectSingleNode("vectors")));
		}
		XmlNode markersNode = xmlDocument.SelectSingleNode("map/markers");
		if (markersNode != null)
		{
			map.Markers.AddRange(from x in markersNode.SelectNodes("marker").OfType<XmlNode>()
								 select GetMarkerFromXmlNode(map, x));
		}
		return map;
	}

	private IEnumerable<ILevelLayerTreeNode> GetLevelLayersFromXmlNode(XmlNode parent, LevelMap map)
	{
		foreach (XmlNode node in parent.ChildNodes)
		{
			if (node.Name == "layergroup")
			{
				if (!node.TryGetAttributeValue("name", out string groupName))
				{
					groupName = string.Empty;
				}
				LevelLayerGroup levelLayerGroup = new LevelLayerGroup(groupName);
				foreach (ILevelLayerTreeNode childNode in GetLevelLayersFromXmlNode(node, map))
				{
					levelLayerGroup.Children.Add(childNode);
				}
				yield return levelLayerGroup;
			}
			else if (node.Name == "layer")
			{
				yield return GetLevelLayerFromXmlNode(node, map);
			}
		}
	}

	private LevelLayer GetLevelLayerFromXmlNode(XmlNode node, LevelMap map)
	{
		LevelLayer levelLayer = new LevelLayer(map);
		if (node.TryGetAttributeValue("name", out string value))
		{
			levelLayer.Name = value;
		}
		if (node.TryGetAttributeValue("minimap_colour", out value) && Colour.TryParseHex(value, out Colour miniMapColour))
		{
			levelLayer.MiniMapColour = miniMapColour;
		}
		levelLayer.LayerRowDefinitions.AddRange(from x in node.SelectNodes("rowdefinitions/rowdefinition").OfType<XmlNode>()
												select GetRowDefinitionFromXmlNode(x));
		XmlNode lightingNode = node.SelectSingleNode("lighting");
		if (lightingNode != null)
		{
			LevelLayerLighting levelLayerLighting = new LevelLayerLighting();
			if (lightingNode.TryGetAttributeValue("type", out value) && Enum.TryParse(value, ignoreCase: true, out LevelLayerLightingType lightingType))
			{
				levelLayerLighting.Type = lightingType;
			}
			if (lightingNode.TryGetAttributeValue("light", out value))
			{
				levelLayerLighting.Light = double.Parse(value, CultureInfo.InvariantCulture);
			}
			levelLayer.Lighting = levelLayerLighting;
		}
		levelLayer.Shadows.AddRange(from x in node.SelectNodes("shadows/shadow").OfType<XmlNode>()
									select GetShadowFromXmlNode(x));
		XmlNode tilesNode = node.SelectSingleNode("tiles");
		if (tilesNode != null)
		{
			if (tilesNode.TryGetAttributeValue("offset_y", out value))
			{
				levelLayer.OffsetY = int.Parse(value);
			}
			if (tilesNode.TryGetAttributeValue("parallax_y", out value))
			{
				if (value.Equals("auto", StringComparison.OrdinalIgnoreCase))
				{
					levelLayer.AutomaticYParallax = true;
				}
				else
				{
					levelLayer.ParallaxY = double.Parse(value, CultureInfo.InvariantCulture);
				}
			}
			if (tilesNode.TryGetAttributeValue("wrap_x", out value))
			{
				levelLayer.WrapX = value.Equals("true", StringComparison.OrdinalIgnoreCase);
			}
			if (tilesNode.TryGetAttributeValue("wrap_y", out value))
			{
				levelLayer.WrapY = value.Equals("true", StringComparison.OrdinalIgnoreCase);
			}
		}
		int[][] tileRows = (from x in node.SelectNodes("tiles/row").OfType<XmlNode>()
							select ReadTileValues(x.InnerText)).ToArray();
		if (tileRows.Length != 0)
		{
			levelLayer.Resize(tileRows.Max(x => x.Length), tileRows.Length);
			for (int y = 0; y < tileRows.Length; y++)
			{
				for (int x = 0; x < Math.Min(levelLayer.Columns, tileRows[y].Length); x++)
				{
					levelLayer.Tiles[x, y] = tileRows[y][x];
				}
			}
		}
		return levelLayer;
	}

	private LayerRowDefinition GetRowDefinitionFromXmlNode(XmlNode node)
	{
		LayerRowDefinition layerRowDefinition = new LayerRowDefinition();
		if (node.TryGetAttributeValue("width", out string value))
		{
			layerRowDefinition.Width = int.Parse(value);
		}
		if (node.TryGetAttributeValue("height", out value))
		{
			layerRowDefinition.Height = int.Parse(value);
		}
		if (node.TryGetAttributeValue("parallax_x", out value))
		{
			layerRowDefinition.Parallax = double.Parse(value, CultureInfo.InvariantCulture);
		}
		if (node.TryGetAttributeValue("velocity_x", out value))
		{
			layerRowDefinition.Velocity = double.Parse(value, CultureInfo.InvariantCulture);
		}
		if (node.TryGetAttributeValue("offset_x", out value))
		{
			layerRowDefinition.InitialOffset = int.Parse(value);
		}
		return layerRowDefinition;
	}

	private LevelLayerShadow GetShadowFromXmlNode(XmlNode node)
	{
		LevelLayerShadow levelLayerShadow = new LevelLayerShadow();
		if (node.TryGetAttributeValue("tiles", out string value))
		{
			levelLayerShadow.Tiles = bool.Parse(value);
		}
		if (node.TryGetAttributeValue("objects", out value))
		{
			levelLayerShadow.Objects = bool.Parse(value);
		}
		if (node.TryGetAttributeValue("layerIndexOffset", out value))
		{
			levelLayerShadow.LayerIndexOffset = int.Parse(value);
		}
		if (node.TryGetAttributeValue("dx", out value))
		{
			Vector2i displacement = levelLayerShadow.Displacement;
			displacement.X = int.Parse(value);
			levelLayerShadow.Displacement = displacement;
		}
		if (node.TryGetAttributeValue("dy", out value))
		{
			Vector2i displacement = levelLayerShadow.Displacement;
			displacement.Y = int.Parse(value);
			levelLayerShadow.Displacement = displacement;
		}
		if (node.TryGetAttributeValue("softness", out value))
		{
			levelLayerShadow.Softness = int.Parse(value);
		}
		if (node.TryGetAttributeValue("colour", out value))
		{
			levelLayerShadow.Colour = Colour.ParseHex(value);
		}
		return levelLayerShadow;
	}

	/// <summary>
	/// Reads a row of tile values in the form "12,h13,v14,hv15". The flip prefixes become flag bits.
	/// </summary>
	private int[] ReadTileValues(string text)
	{
		StringBuilder digits = new StringBuilder();
		List<int> tileValues = new List<int>();
		StringReader stringReader = new StringReader(text);
		while (stringReader.CanRead())
		{
			int tileValue = 0;
			char c = (char)stringReader.Peek();
			if (char.ToUpper(c) == 'H')
			{
				tileValue |= FlipHorizontalFlag;
				stringReader.Read();
				c = (char)stringReader.Peek();
			}
			if (char.ToUpper(c) == 'V')
			{
				tileValue |= FlipVerticalFlag;
				stringReader.Read();
				c = (char)stringReader.Peek();
			}
			digits.Clear();
			while (char.IsNumber(c))
			{
				stringReader.Read();
				digits.Append(c);
				c = (char)stringReader.Peek();
			}
			tileValue |= int.Parse(digits.ToString());
			tileValues.Add(tileValue);
			if (c != ',' && c != EndOfText)
			{
				throw new Exception();
			}
			stringReader.Read();
		}
		return tileValues.ToArray();
	}

	private LevelMarker GetMarkerFromXmlNode(LevelMap map, XmlNode node)
	{
		Rectanglei bounds = new Rectanglei
		{
			X = int.Parse(node.Attributes["x"].Value),
			Y = int.Parse(node.Attributes["y"].Value)
		};
		if (node.TryGetAttributeValue("width", out string value))
		{
			bounds.Width = int.Parse(value);
		}
		if (node.TryGetAttributeValue("height", out value))
		{
			bounds.Height = int.Parse(value);
		}
		string name = null;
		if (node.TryGetAttributeValue("name", out value))
		{
			name = value;
		}
		string tag = null;
		if (node.TryGetAttributeValue("tag", out value))
		{
			tag = value;
		}
		LevelLayer layer = null;
		if (node.TryGetAttributeValue("layer", out value))
		{
			layer = map.Layers[int.Parse(value)];
		}
		return new LevelMarker(name, tag, bounds, layer);
	}

	private IEnumerable<CollisionVector> GetCollisionVectorsFromXmlNode(XmlNode parent)
	{
		foreach (XmlNode childNode in parent.ChildNodes)
		{
			// Skip a vector that has a missing or bad attribute.
			if (childNode.Name == "vector" && TryGetAttributeXY(childNode, "a", out Vector2i a) && TryGetAttributeXY(childNode, "b", out Vector2i b) && childNode.TryGetAttributeValue("paths", out string value) && uint.TryParse(value, out uint paths) && childNode.TryGetAttributeValue("flags", out value) && int.TryParse(value, out int flags))
			{
				yield return new CollisionVector(a, b, paths, (CollisionFlags)flags);
			}
		}
	}

	private IEnumerable<int> GetCollisionPathsFromXmlNode(XmlNode parent)
	{
		foreach (XmlNode childNode in parent.ChildNodes)
		{
			if (childNode.Name == "path")
			{
				if (!childNode.TryGetAttributeValue("layer", out string value))
				{
					throw new Exception();
				}
				if (!int.TryParse(value, out int layerIndex))
				{
					throw new Exception();
				}
				yield return layerIndex;
			}
		}
	}

	private bool TryGetAttributeXY(XmlNode node, string name, out Vector2i xy)
	{
		xy = default;
		if (!node.TryGetAttributeValue(name, out string value))
		{
			return false;
		}
		string[] parts = value.Split(',');
		if (parts.Length != 2)
		{
			return false;
		}
		if (!int.TryParse(parts[0], out int x))
		{
			return false;
		}
		if (!int.TryParse(parts[1], out int y))
		{
			return false;
		}
		xy = new Vector2i(x, y);
		return true;
	}
}
